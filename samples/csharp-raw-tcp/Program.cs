using System.Net.Sockets;
using System.Text;
using Virex.NET.Contracts;

var host = args.Length > 0 ? args[0] : "127.0.0.1";
var port = args.Length > 1 ? int.Parse(args[1]) : 5089;
var product = new ProductInfo
{
    WaferID = "WCS-TCP-210-001",
    LotID = "LOT-CS-TCP-210",
    Recipe = "RCP-DEMO",
    Slot = "1",
    FoupID = "FOUP-DEMO",
    ChamberID = "CH-1",
};

PrintStep("Virex.NET C# Raw TCP 13-Step Demo");
Console.WriteLine($"TCP endpoint: {host}:{port}");
Prompt("In Simulator, press Start Servers, then press Enter here.");

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

Console.WriteLine("Initial simulator frames:");
Console.WriteLine(await ReadFrameAsync(reader));
Console.WriteLine(await ReadFrameAsync(reader));

PrintStep("Step 1 - Query status");
await WriteFrameAsync(stream, TcpSocketEventFormatter.FormatCommand("status"));
Console.WriteLine(await ReadFrameAsync(reader));

PrintStep("Step 2 - Query error");
await WriteFrameAsync(stream, TcpSocketEventFormatter.FormatCommand("error"));
Console.WriteLine(await ReadFrameAsync(reader));

PrintStep("Step 3 - Query ProductInfo");
await WriteFrameAsync(stream, TcpSocketEventFormatter.FormatCommand("getProductInfo"));
Console.WriteLine(await ReadFrameAsync(reader));

PrintStep("Step 4 - Initialize");
await WriteFrameAsync(stream, TcpSocketEventFormatter.FormatInitializeCommand());
Console.WriteLine(await ReadFrameAsync(reader));

PrintStep("Step 5 - Confirm Ready");
Console.WriteLine(await ReadFrameAsync(reader));

PrintStep("Step 6 - Set ProductInfo");
await WriteFrameAsync(stream, TcpSocketEventFormatter.FormatProductInfo(product));
Console.WriteLine(await ReadFrameAsync(reader));

PrintStep("Step 7 - Confirm ProductInfo");
await WriteFrameAsync(stream, TcpSocketEventFormatter.FormatCommand("getProductInfo"));
Console.WriteLine(await ReadFrameAsync(reader));

PrintStep("Step 8 - Start run");
await WriteFrameAsync(stream, TcpSocketEventFormatter.FormatStartCommand("golden-sample", ControlRunModes.Continue));
Console.WriteLine(await ReadFrameAsync(reader));

PrintStep("Step 9 - Observe run events");
Console.WriteLine(await ReadFrameAsync(reader));

PrintStep("Step 10 - Stop run");
await WriteFrameAsync(stream, TcpSocketEventFormatter.FormatStopCommand("operator-request"));
Console.WriteLine(await ReadFrameAsync(reader));

PrintStep("Step 11 - Query results");
await WriteFrameAsync(stream, ProtocolJson.Serialize(new { type = "results", product.LotID, product.WaferID }) + "\n");
Console.WriteLine(await ReadFrameAsync(reader));

PrintStep("Step 12 - Deinitialize");
await WriteFrameAsync(stream, TcpSocketEventFormatter.FormatDeinitializeCommand());
Console.WriteLine(await ReadFrameAsync(reader));

PrintStep("Step 13 - Confirm Uninitialized");
Console.WriteLine(await ReadFrameAsync(reader));

return 0;

static async Task<string> ReadFrameAsync(StreamReader reader)
{
    var line = await reader.ReadLineAsync();
    if (line is null)
        throw new EndOfStreamException("TCP stream closed before a full NDJSON frame was received.");
    return line;
}

static async Task WriteFrameAsync(NetworkStream stream, string frame)
{
    var bytes = Encoding.UTF8.GetBytes(frame);
    await stream.WriteAsync(bytes);
    await stream.FlushAsync();
}

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
