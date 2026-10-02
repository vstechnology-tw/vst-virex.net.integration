using System.Text.Json;
using Virex.NET.Contracts;

namespace Virex.NET.Simulator.Core;

public sealed partial class SimulatorSession
{
    private readonly object _recipeGate = new object();
    private readonly HashSet<string> _recipes = new HashSet<string>(StringComparer.Ordinal) { "RCP-A", "RCP-DEMO" };
    private RecipeParameters? _loadedRecipe;
    private const string SimulatorRecipeRevision = "simulator-v1";

    public RecipeList GetRecipes()
    {
        lock (_recipeGate)
        {
            var items = _recipes.OrderBy(x => x, StringComparer.Ordinal)
                .Select(x => new RecipeInfo { Recipe = x, Revision = SimulatorRecipeRevision }).ToArray();
            return new RecipeList { Items = items, Count = items.Length };
        }
    }

    public RecipeInfo GetCurrentRecipe()
    {
        lock (_recipeGate)
        {
            var loaded = RequireLoadedRecipe();
            return new RecipeInfo { Recipe = loaded.Recipe, Revision = loaded.Revision };
        }
    }

    public RecipeParameters GetCurrentRecipeParameters()
    {
        lock (_recipeGate)
            return ProtocolJson.Deserialize<RecipeParameters>(ProtocolJson.Serialize(RequireLoadedRecipe()))!;
    }

    private RecipeParameters RequireLoadedRecipe()
    {
        if (State is SimulatorState.Initializing or SimulatorState.UpdatingProductInfo or SimulatorState.Deinitializing)
            throw new SimulatorQueryException(409, QueryErrorCodes.QueryNotReady, "The loaded recipe snapshot is not ready.");
        return _loadedRecipe ?? throw new SimulatorQueryException(409, QueryErrorCodes.NoCurrentRecipe, "No recipe is currently loaded.");
    }

    private void LoadRecipe(string recipe)
    {
        using var value = JsonDocument.Parse("1000");
        lock (_recipeGate)
        {
            _recipes.Add(recipe);
            _loadedRecipe = new RecipeParameters
            {
                Recipe = recipe,
                Revision = SimulatorRecipeRevision,
                Groups = [new RecipeParameterGroup
                {
                    Key = "simulator",
                    Parameters = [new RecipeParameter { Key = "cycleDelayMs", Type = "integer", Value = value.RootElement.Clone() }],
                }],
            };
        }
    }

    private void ClearLoadedRecipe()
    {
        lock (_recipeGate)
            _loadedRecipe = null;
    }

    public async Task<ResultDetail> GetResultDetailAsync(string resultId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(resultId))
            throw new SimulatorQueryException(400, QueryErrorCodes.InvalidQuery, "A result identifier is required.");

        string path;
        lock (_results)
        {
            var result = _results.FirstOrDefault(x => string.Equals(x.ResultId, resultId, StringComparison.Ordinal));
            if (result is null)
                throw new SimulatorQueryException(404, QueryErrorCodes.ResultNotFound, "The result is unknown, uncommitted, or outside retained history.");
            // Only an indexed result may select a file. The request never becomes a filesystem path.
            path = result.ResultPath;
        }

        try
        {
            using var stream = File.OpenRead(path);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (!document.RootElement.TryGetProperty("detail", out var element) ||
                !element.TryGetProperty("findings", out var findings) || findings.ValueKind != JsonValueKind.Array)
                throw new JsonException("Missing result detail.");
            var detail = ProtocolJson.Deserialize<ResultDetail>(element.GetRawText());
            if (detail is null || detail.SchemaVersion != ResultDetail.CurrentSchemaVersion ||
                detail.ResultId != resultId || detail.Summary is null || detail.Summary.ResultId != resultId || detail.Findings is null)
                throw new JsonException("Invalid result detail identity or schema.");
            return detail;
        }
        catch (FileNotFoundException)
        {
            throw new SimulatorQueryException(410, QueryErrorCodes.ResultDeleted, "The committed result artifact has been deleted.");
        }
        catch (DirectoryNotFoundException)
        {
            throw new SimulatorQueryException(410, QueryErrorCodes.ResultDeleted, "The committed result artifact has been deleted.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            throw new SimulatorQueryException(503, QueryErrorCodes.QueryFailed, "The committed result detail could not be read.");
        }
    }
}
