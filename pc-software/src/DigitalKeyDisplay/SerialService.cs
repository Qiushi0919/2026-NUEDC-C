using System.IO.Ports;
using Microsoft.Win32;

namespace DigitalKeyLab;

public sealed record SerialPortDevice(string PortName, string DevicePath)
{
    public bool IsBluetooth => DevicePath.Contains("BTHMODEM", StringComparison.OrdinalIgnoreCase);
    public bool IsUsb => DevicePath.Contains("USB", StringComparison.OrdinalIgnoreCase);
}

public sealed class SerialService : IDisposable
{
    private readonly ProtocolParser _parser = new();
    private readonly object _readLock = new();
    private SerialPort? _port;

    public event Action<AnchorFrame>? FrameReceived;
    public event Action<string>? ErrorOccurred;

    public bool IsConnected => _port?.IsOpen == true;
    public string PortName => _port?.PortName ?? string.Empty;

    public static string[] GetPortNames() => SerialPort.GetPortNames()
        .OrderBy(PortNumber)
        .ToArray();

    public static SerialPortDevice[] GetPortDevices()
    {
        var paths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DEVICEMAP\SERIALCOMM");
            if (key is not null)
            {
                foreach (var devicePath in key.GetValueNames())
                {
                    if (key.GetValue(devicePath) is string portName)
                        paths[portName] = devicePath;
                }
            }
        }
        catch
        {
            // Port enumeration still works even if registry metadata is unavailable.
        }

        return GetPortNames()
            .Select(port => new SerialPortDevice(port, paths.GetValueOrDefault(port, string.Empty)))
            .ToArray();
    }

    public void Connect(string portName, int baudRate = 115200)
    {
        Disconnect();
        lock (_readLock)
            _parser.Reset();
        var port = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One)
        {
            Handshake = Handshake.None,
            DtrEnable = false,
            RtsEnable = false,
            ReadBufferSize = 8192,
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

    public void Disconnect()
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

    private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        try
        {
            if (sender is not SerialPort port || !port.IsOpen)
                return;
            AnchorFrame[] frames;
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
                FrameReceived?.Invoke(frame);
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(ex.Message);
        }
    }

    private void OnErrorReceived(object sender, SerialErrorReceivedEventArgs e) =>
        ErrorOccurred?.Invoke($"串口错误：{e.EventType}");

    private static int PortNumber(string name) =>
        name.StartsWith("COM", StringComparison.OrdinalIgnoreCase) && int.TryParse(name[3..], out var number)
            ? number
            : int.MaxValue;

    public void Dispose() => Disconnect();
}
