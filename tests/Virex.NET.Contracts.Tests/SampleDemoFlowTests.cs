namespace Virex.NET.Contracts.Tests;

public sealed class SampleDemoFlowTests
{
    private static readonly string[] RequiredSteps =
    [
        "Step 1 - Query status",
        "Step 2 - Query error",
        "Step 3 - Query ProductInfo",
        "Step 4 - Initialize",
        "Step 5 - Confirm Ready",
        "Step 6 - Set ProductInfo",
        "Step 7 - Confirm ProductInfo",
        "Step 8 - Start run",
        "Step 9 - Observe run events",
        "Step 10 - Stop run",
        "Step 11 - Query results",
        "Step 12 - Deinitialize",
        "Step 13 - Confirm Uninitialized",
    ];

    private static readonly string[] SampleSources =
    [
        "samples/csharp-sdk/Program.cs",
        "samples/csharp-raw-rest/Program.cs",
        "samples/csharp-raw-tcp/Program.cs",
        "samples/csharp-raw-mqtt/Program.cs",
        "samples/python-raw-rest/main.py",
        "samples/python-raw-tcp/main.py",
        "samples/python-raw-mqtt/main.py",
        "samples/cpp-raw-rest/main.cpp",
        "samples/cpp-raw-tcp/main.cpp",
        "samples/cpp-raw-mqtt/main.cpp",
    ];

    [Fact]
    public void AllExecutableSamplesUseTheSameThirteenStepDemoFlow()
    {
        var root = FindRepositoryRoot();

        foreach (var source in SampleSources)
        {
            var path = Path.Combine(root, source.Replace('/', Path.DirectorySeparatorChar));
            var text = File.ReadAllText(path);
            foreach (var step in RequiredSteps)
            {
                Assert.Contains(step, text);
            }
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Directory.Build.props")) &&
                Directory.Exists(Path.Combine(directory.FullName, "samples")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root.");
    }
}
