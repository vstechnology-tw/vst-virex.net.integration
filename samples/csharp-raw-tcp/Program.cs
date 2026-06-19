using System.Net.Sockets;
using System.Text;
using Virex.NET.Contracts;

var host = args.Length > 0 ? args[0] : "127.0.0.1";
var port = args.Length > 1 ? int.Parse(args[1]) : 5089;

using var client = new TcpClient();
await client.ConnectAsync(host, port);
using var stream = client.GetStream();
using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);

var initialStatus = await reader.ReadLineAsync();
var initialWaferInfo = await reader.ReadLineAsync();
Console.WriteLine(initialStatus);
Console.WriteLine(initialWaferInfo);

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

Console.WriteLine("Sent waferInfo frame. Waiting for echo/update event...");
Console.WriteLine(await reader.ReadLineAsync());
