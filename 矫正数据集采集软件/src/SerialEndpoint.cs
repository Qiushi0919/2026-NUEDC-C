using System.IO.Ports;

namespace CalibrationCollector;

public sealed record SerialRead(
    string PortName,
    string Role,
    DateTimeOffset ReceivedAt,
    byte[] Bytes);

public sealed class SerialEndpoint : IDisposable
{
    private readonly object _sync = new();
    private readonly string _portName;
    private readonly string _role;
    private SerialPort? _port;
    private int _connecting;
    private long _bytesReceived;
    private long _connectAttempts;
    private string _lastError = "等待首次连接";

    public SerialEndpoint(string portName, string role)
    {
        _portName = portName;
        _role = role;
    }

    public event Action<SerialRead>? BytesReceived;
    public event Action<SerialEndpoint>? StateChanged;

    public string PortName => _portName;
    public string Role => _role;
    public long BytesReceivedCount => Interlocked.Read(ref _bytesReceived);
    public long ConnectAttempts => Interlocked.Read(ref _connectAttempts);
    public string LastError { get { lock (_sync) return _lastError; } }

    public bool IsConnected
    {
        get
        {
            lock (_sync)
                return _port?.IsOpen == true;
        }
    }

    public Task EnsureConnectedAsync()
    {
        if (Interlocked.Exchange(ref _connecting, 1) != 0)
            return Task.CompletedTask;

        return Task.Run(() =>
        {
            try
            {
                if (IsConnected)
                {
                    var present = SerialPort.GetPortNames()
                        .Any(name => string.Equals(name, _portName, StringComparison.OrdinalIgnoreCase));
                    if (!present)
                        Disconnect("端口已从系统移除");
                    return;
                }

                Interlocked.Increment(ref _connectAttempts);
                var port = new SerialPort(_portName, 115200, Parity.None, 8, StopBits.One)
                {
                    Handshake = Handshake.None,
                    DtrEnable = false,
                    RtsEnable = false,
                    ReadBufferSize = 16384,
                    WriteBufferSize = 4096,
                    ReadTimeout = 500,
                    WriteTimeout = 500,
                    ReceivedBytesThreshold = 1
                };
                port.DataReceived += OnDataReceived;
                port.ErrorReceived += OnErrorReceived;

                try
                {
                    port.Open();
                    lock (_sync)
                    {
                        _port?.Dispose();
                        _port = port;
                        _lastError = string.Empty;
                    }
                    StateChanged?.Invoke(this);
                }
                catch (Exception ex)
                {
                    port.DataReceived -= OnDataReceived;
                    port.ErrorReceived -= OnErrorReceived;
                    port.Dispose();
                    SetError(FriendlyError(ex));
                }
            }
            finally
            {
                Interlocked.Exchange(ref _connecting, 0);
            }
        });
    }

    public void Disconnect(string reason = "已断开")
    {
        SerialPort? port;
        lock (_sync)
        {
            port = _port;
            _port = null;
            _lastError = reason;
        }

        if (port is not null)
        {
            try
            {
                port.DataReceived -= OnDataReceived;
                port.ErrorReceived -= OnErrorReceived;
                if (port.IsOpen)
                    port.Close();
            }
            catch
            {
                // The reconnect loop will recover after a device removal.
            }
            finally
            {
                port.Dispose();
            }
        }
        StateChanged?.Invoke(this);
    }

    private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        try
        {
            if (sender is not SerialPort port || !port.IsOpen)
                return;

            byte[] bytes;
            lock (_sync)
            {
                if (!ReferenceEquals(_port, port) || !port.IsOpen)
                    return;
                var count = port.BytesToRead;
                if (count <= 0)
                    return;
                bytes = new byte[count];
                var read = port.Read(bytes, 0, bytes.Length);
                if (read != bytes.Length)
                    Array.Resize(ref bytes, read);
            }

            if (bytes.Length == 0)
                return;
            Interlocked.Add(ref _bytesReceived, bytes.Length);
            BytesReceived?.Invoke(new SerialRead(_portName, _role, DateTimeOffset.Now, bytes));
        }
        catch (Exception ex)
        {
            Disconnect(FriendlyError(ex));
        }
    }

    private void OnErrorReceived(object sender, SerialErrorReceivedEventArgs e) =>
        SetError($"串口错误：{e.EventType}");

    private void SetError(string error)
    {
        lock (_sync)
            _lastError = error;
        StateChanged?.Invoke(this);
    }

    private static string FriendlyError(Exception ex) => ex switch
    {
        UnauthorizedAccessException => "端口被其他程序占用",
        IOException => "设备已断开或不可用",
        ArgumentException => "系统中没有这个串口",
        _ => ex.Message
    };

    public void Dispose() => Disconnect("程序已关闭");
}
