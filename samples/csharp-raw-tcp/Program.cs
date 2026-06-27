using System.Net.Sockets;
using System.Text;
using Virex.NET.Contracts;

var host = args.Length > 0 ? args[0] : "127.0.0.1";
var port = args.Length > 1 ? int.Parse(args[1]) : 5089;

PrintStep("Virex.NET C# Raw TCP ProductInfo Demo");
Console.WriteLine($"TCP endpoint: {host}:{port}");
Prompt("In Simulator, press Start Servers and Initialize, then press Enter here.");

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

Console.WriteLine(await reader.ReadLineAsync());
Console.WriteLine(await reader.ReadLineAsync());

var frame = TcpSocketEventFormatter.FormatProductInfo(new ProductInfo
{
    WaferID = "W01",
    LotID = "LOT-RAW-TCP-001",
    Recipe = "RCP-A",
    Slot = "1",
    FoupID = "FOUP-A",
    ChamberID = "CH-1",
});

await WriteFrameAsync(stream, frame);
Console.WriteLine(await reader.ReadLineAsync());

await WriteFrameAsync(stream, TcpSocketEventFormatter.FormatStartCommand("golden-sample", ControlRunModes.Continue));
Console.WriteLine(await reader.ReadLineAsync());

await Task.Delay(300);
await WriteFrameAsync(stream, TcpSocketEventFormatter.FormatStopCommand("operator-request"));
Console.WriteLine(await reader.ReadLineAsync());

return 0;

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
