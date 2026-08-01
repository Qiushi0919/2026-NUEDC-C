using System.IO.Ports;

namespace DigitalKeyLab;

public sealed class ControlLinkService : IDisposable
{
    private readonly ControlFrameParser _parser = new();
    private readonly object _readLock = new();
    private readonly object _writeLock = new();
    private SerialPort? _port;
    private byte _transmitSequence;

    public event Action<int>? DipIdReceived;
    public event Action<string>? ErrorOccurred;

    public bool IsConnected => _port?.IsOpen == true;
    public string PortName => _port?.PortName ?? string.Empty;
    public long ValidIdReportCount { get; private set; }
    public int CrcErrorCount => _parser.CrcErrorCount;

    public void Connect(string portName)
    {
        Disconnect();
        lock (_readLock)
            _parser.Reset();
        _transmitSequence = 0;
        ValidIdReportCount = 0;

        var port = new SerialPort(portName, 115200, Parity.None, 8, StopBits.One)
        {
            Handshake = Handshake.None,
            DtrEnable = false,
            RtsEnable = false,
            ReadBufferSize = 4096,
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
            _port = port;
        }
        catch
        {
            port.DataReceived -= OnDataReceived;
            port.ErrorReceived -= OnErrorReceived;
            port.Dispose();
            throw;
        }
    }

    public void SendLockStatus(ControlLockStatus status)
    {
        lock (_writeLock)
        {
            var port = _port;
            if (port?.IsOpen != true)
                return;
            var frame = DigitalKeyControlProtocol.BuildLockStatus(_transmitSequence++, status);
            port.Write(frame, 0, frame.Length);
        }
    }

    public void Disconnect()
    {
        lock (_writeLock)
        {
            var port = _port;
            _port = null;
            if (port is null)
                return;
            try
            {
                port.DataReceived -= OnDataReceived;
                port.ErrorReceived -= OnErrorReceived;
                if (port.IsOpen)
                    port.Close();
            }
            finally
            {
                port.Dispose();
            }
        }
    }

    private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        try
        {
            if (sender is not SerialPort port || !port.IsOpen)
                return;
            ControlFrame[] frames;
            lock (_readLock)
            {
                var count = port.BytesToRead;
                if (count <= 0)
                    return;
                var bytes = new byte[count];
                var read = port.Read(bytes, 0, bytes.Length);
                frames = _parser.Append(bytes.AsSpan(0, read)).ToArray();
            }

            foreach (var frame in frames)
            {
                if (frame.Type != ControlMessageType.IdReport ||
                    frame.Payload.Length != 1 || frame.Payload[0] > 0x0F)
                    continue;
                ValidIdReportCount++;
                DipIdReceived?.Invoke(frame.Payload[0]);
            }
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(ex.Message);
        }
    }

    private void OnErrorReceived(object sender, SerialErrorReceivedEventArgs e) =>
        ErrorOccurred?.Invoke($"蓝牙串口错误：{e.EventType}");

    public void Dispose() => Disconnect();
}
