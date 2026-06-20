using System.Net.Sockets;
using System.Text;
using Virex.NET.Contracts;

var host = args.Length > 0 ? args[0] : "127.0.0.1";
var port = args.Length > 1 ? int.Parse(args[1]) : 5089;

PrintStep("Virex.NET C# Raw TCP Guided Demo");
Console.WriteLine("This sample connects to the simulator TCP socket, reads the initial frames, sends WaferInfo, and waits for the update event.");
Console.WriteLine($"TCP endpoint: {host}:{port}");
Prompt("In Simulator, press Start Servers, then press Enter here. Initialize is not required for the WaferInfo TCP demo.");

using var client = new TcpClient();
try
{
    await client.ConnectAsync(host, port);
}
catch (SocketException ex)
{
    Console.WriteLine("Connection failed. In Simulator, press Start Servers and verify the TCP port matches this sample.");
    Console.WriteLine(ex.Message);
    return 1;
}

using var stream = client.GetStream();
using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);

PrintStep("Step 1 - Read initial TCP frames");
var initialStatus = await reader.ReadLineAsync();
var initialWaferInfo = await reader.ReadLineAsync();
Console.WriteLine("Initial status frame:");
Console.WriteLine(initialStatus);
Console.WriteLine("Initial WaferInfo frame:");
Console.WriteLine(initialWaferInfo);

PrintStep("Step 2 - Send waferInfo frame");
var frame = TcpSocketEventFormatter.FormatWaferInfo(new WaferInfo
{
    LotId = "LOT-RAW-TCP-001",
    WaferId = "W01",
    RecipeId = "RCP-A",
    Slot = "1",
    FoupId = "FOUP-A",
    ChamberId = "CH-1",
});

var bytes = Encoding.UTF8.GetBytes(frame);
await stream.WriteAsync(bytes);
await stream.FlushAsync();

Console.WriteLine("Sent waferInfo frame.");
Console.WriteLine("Expected Simulator Event Log:");
Console.WriteLine("WaferInfo updated from TCP: lotId=LOT-RAW-TCP-001, waferId=W01, recipeId=RCP-A, slot=1, foupId=FOUP-A, chamberId=CH-1");
Console.WriteLine("Waiting for echoed waferInfo update event...");
Console.WriteLine(await reader.ReadLineAsync());

return 0;

static void PrintStep(string title)
{
    Console.WriteLine();
    Console.WriteLine("== " + title + " ==");
}

static void Prompt(string message)
{
    Console.WriteLine();
    Console.WriteLine("Action required in Simulator:");
    Console.WriteLine(message);
    Console.ReadLine();
}
