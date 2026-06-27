using System.Net.Sockets;
using System.Text;
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

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await ReadFrameAsync(stream, cancellationToken).ConfigureAwait(false);
            if (line is null)
                return;

            if (VirexEventParser.TryParse(line, out var value, out _))
                EventReceived?.Invoke(this, value);
        }
    }

    public async Task SendProductInfoAsync(ProductInfo info, CancellationToken cancellationToken = default) =>
        await SendFrameAsync(TcpSocketEventFormatter.FormatProductInfo(info), cancellationToken).ConfigureAwait(false);

    public async Task SendStartAsync(CancellationToken cancellationToken = default) =>
        await SendFrameAsync(TcpSocketEventFormatter.FormatStartCommand(), cancellationToken).ConfigureAwait(false);

    public async Task SendStartAsync(string? condition, CancellationToken cancellationToken = default) =>
        await SendFrameAsync(TcpSocketEventFormatter.FormatStartCommand(condition), cancellationToken).ConfigureAwait(false);

    public async Task SendStartAsync(string? condition, string? runMode, CancellationToken cancellationToken = default) =>
        await SendFrameAsync(TcpSocketEventFormatter.FormatStartCommand(condition, runMode), cancellationToken).ConfigureAwait(false);

    public async Task SendStopAsync(CancellationToken cancellationToken = default) =>
        await SendFrameAsync(TcpSocketEventFormatter.FormatStopCommand(), cancellationToken).ConfigureAwait(false);

    public async Task SendStopAsync(string? reason, CancellationToken cancellationToken = default) =>
        await SendFrameAsync(TcpSocketEventFormatter.FormatStopCommand(reason), cancellationToken).ConfigureAwait(false);

    private async Task SendFrameAsync(string frame, CancellationToken cancellationToken)
    {
        using var client = new TcpClient();
        await client.ConnectAsync(_options.TcpHost, _options.TcpPort).ConfigureAwait(false);
        using var stream = client.GetStream();
        var bytes = Encoding.UTF8.GetBytes(frame);
        await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<string?> ReadFrameAsync(Stream stream, CancellationToken cancellationToken)
    {
        var bytes = new List<byte>();
        var buffer = new byte[1];
        var frameStarted = false;
        var frameTimeoutMs = _options.TcpFrameTimeoutMs <= 0 ? 5000 : _options.TcpFrameTimeoutMs;

        while (true)
        {
            var read = await ReadByteWithTimeoutAsync(
                stream,
                buffer,
                frameStarted,
                frameTimeoutMs,
                cancellationToken).ConfigureAwait(false);

            if (read == 0)
            {
                if (frameStarted)
                    throw new EndOfStreamException("TCP stream closed before the current NDJSON frame was completed.");

                return null;
            }

            frameStarted = true;

            if (buffer[0] == (byte)'\n')
                return Encoding.UTF8.GetString(bytes.ToArray()).TrimEnd('\r');

            bytes.Add(buffer[0]);
        }
    }

    private static async Task<int> ReadByteWithTimeoutAsync(
        Stream stream,
        byte[] buffer,
        bool frameStarted,
        int frameTimeoutMs,
        CancellationToken cancellationToken)
    {
        if (!frameStarted)
            return await stream.ReadAsync(buffer, 0, 1, cancellationToken).ConfigureAwait(false);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(frameTimeoutMs);

        try
        {
            return await stream.ReadAsync(buffer, 0, 1, timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"TCP NDJSON frame was not completed within {frameTimeoutMs} ms after partial data was received.");
        }
    }
}
