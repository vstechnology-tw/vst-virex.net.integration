using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Virex.NET.Contracts;

namespace Virex.NET.Client;

public sealed class VirexTcpEventClient
{
    private readonly VirexClientOptions _options;

    public VirexTcpEventClient(VirexClientOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public event EventHandler<VirexEvent>? EventReceived;

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        using var client = new TcpClient();
        await client.ConnectAsync(_options.TcpHost, _options.TcpPort).ConfigureAwait(false);
        using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 4096, leaveOpen: true);

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync().ConfigureAwait(false);
            if (line is null)
                return;

            if (VirexEventParser.TryParse(line, out var value, out _))
                EventReceived?.Invoke(this, value);
        }
    }

    public async Task SendWaferInfoAsync(WaferInfo info, CancellationToken cancellationToken = default) =>
        await SendFrameAsync(TcpSocketEventFormatter.FormatWaferInfo(info), cancellationToken).ConfigureAwait(false);

    public async Task SendStartAsync(CancellationToken cancellationToken = default) =>
        await SendFrameAsync(TcpSocketEventFormatter.FormatCommand("start"), cancellationToken).ConfigureAwait(false);

    public async Task SendStopAsync(CancellationToken cancellationToken = default) =>
        await SendFrameAsync(TcpSocketEventFormatter.FormatCommand("stop"), cancellationToken).ConfigureAwait(false);

    private async Task SendFrameAsync(string frame, CancellationToken cancellationToken)
    {
        using var client = new TcpClient();
        await client.ConnectAsync(_options.TcpHost, _options.TcpPort).ConfigureAwait(false);
        using var stream = client.GetStream();
        var bytes = Encoding.UTF8.GetBytes(frame);
        await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
}
