using Virex.NET.Client;
using Virex.NET.Samples;
using Xunit.Abstractions;

namespace Virex.NET.Contracts.Tests;

public sealed class CandidateSampleTests(ITestOutputHelper output)
{
    [Fact]
    public async Task CandidateOpenApiIdentifiesTheNewPublicContractVersion()
    {
        output.WriteLine(System.Text.Json.JsonSerializer.Serialize(new
        {
            target = ".NETCoreApp,Version=v8.0 / Windows",
            actualFramework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            environmentVersion = Environment.Version.ToString(),
            loaded = new[] { typeof(CommandResponse).Assembly, typeof(VirexClient).Assembly,
                typeof(Virex.NET.Simulator.Core.SimulatorSession).Assembly,
                typeof(Virex.NET.Simulator.WPF.Services.RestSimulatorServer).Assembly, typeof(CandidateSampleTests).Assembly }
                .Select(assembly => new { name = assembly.GetName().Name, path = assembly.Location,
                    version = assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
                        .Cast<System.Reflection.AssemblyInformationalVersionAttribute>().Single().InformationalVersion,
                    sha256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(assembly.Location))) }),
        }));
        await using var fixture = await PublicTransportFixture.CreateAsync(false);
        using var document = System.Text.Json.JsonDocument.Parse(await fixture.Http.GetStringAsync("openapi/v1.json"));
        Assert.Equal("2.4.0", document.RootElement.GetProperty("info").GetProperty("version").GetString());
        Assert.True(document.RootElement.GetProperty("paths").TryGetProperty("/api/events", out _));
        Assert.True(document.RootElement.GetProperty("paths").TryGetProperty("/api/data-sources", out _));
        Assert.True(document.RootElement.GetProperty("paths").TryGetProperty("/api/alarms", out _));
        Assert.True(document.RootElement.GetProperty("paths").TryGetProperty("/api/system/reset", out _));
    }

    [Theory]
    [InlineData(" source / 零 value ")]
    [InlineData("Δ/measurement")]
    [InlineData(" outer + inner & ? ")]
    [InlineData("literal %E9%9B%B6")]
    public async Task ExactUtf8SourceNameDoesNotUseRequestBodyCharset(string name)
    {
        await using var fixture = await PublicTransportFixture.CreateAsync(false);
        using (var value = System.Text.Json.JsonDocument.Parse("0"))
            await fixture.Session.ConfigureLiveDataSourceAsync(name, "integer", value.RootElement);
        using var request = new HttpRequestMessage(HttpMethod.Get, RestRoutes.DataSourceValue(name))
        {
            Content = new StringContent(string.Empty),
        };
        request.Content.Headers.ContentType!.CharSet = "iso-8859-1";
        using var response = await fixture.Http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var actual = ProtocolJson.Deserialize<DataSourceValue>(await response.Content.ReadAsStringAsync())!;
        Assert.Equal(name, actual.Name);
        Assert.Equal(0, actual.Value.GetInt32());
        Assert.Equal(name, (await fixture.Client.ReadDataSourceAsync(name)).Name);
    }

    [Fact]
    public async Task SampleRejectsAnotherWorksResultEvenWhenThatResultIsCommitted()
    {
        await using var fixture = await PublicTransportFixture.CreateAsync(false);
        await fixture.Client.InitializeAsync();
        var arrived = new TaskCompletionSource<EventEnvelope>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = await fixture.Client.TcpEvents.SubscribeAsync(["resultCreated"], e => arrived.TrySetResult(e));
        await fixture.Client.StartAsync(new SystemStartRequest { RequestId = "other-work", RunMode = ControlRunModes.Continue });
        await fixture.Session.RunCompletedAsync();
        var unrelated = await arrived.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var current = await fixture.Client.StartAsync(new SystemStartRequest { RequestId = "expected-work", RunMode = ControlRunModes.Continue });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CommittedResultReader.ReadAsync(fixture.Client, current, unrelated, "result.findings"));

        await subscription.CloseAsync();
    }

    [Fact]
    public async Task DelayedResultEventReadsItsCommittedValueAfterNextAcceptanceInvalidatesCurrentResult()
    {
        await using var fixture = await PublicTransportFixture.CreateAsync(false);
        await fixture.Client.InitializeAsync();
        var arrived = new TaskCompletionSource<EventEnvelope>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = await fixture.Client.TcpEvents.SubscribeAsync(["resultCreated"], e => arrived.TrySetResult(e));
        var first = await fixture.Client.StartAsync(new SystemStartRequest { RequestId = "sample-first", RunMode = ControlRunModes.Continue });
        await fixture.Session.RunCompletedAsync();
        var completed = await arrived.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var next = await fixture.Client.StartAsync(new SystemStartRequest { RequestId = "sample-next", RunMode = ControlRunModes.Continue });
        var missing = await Assert.ThrowsAsync<VirexQueryException>(() => fixture.Client.GetCurrentResultAsync());
        Assert.Equal(QueryErrorCodes.NoCurrentResult, missing.ErrorCode);

        var value = await CommittedResultReader.ReadAsync(fixture.Client, first, completed, "result.findings");

        Assert.Equal(completed.ResultId, value.ResultId);
        Assert.Equal(first.JobId, value.JobId);
        Assert.NotEqual(next.JobId, value.JobId);
        Assert.Empty(value.Value.EnumerateArray());
        await subscription.CloseAsync();
    }
}
