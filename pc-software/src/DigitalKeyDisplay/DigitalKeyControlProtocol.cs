using System.Buffers.Binary;

namespace DigitalKeyLab;

public enum ControlMessageType : byte
{
    IdReport = 0x10,
    LockStatus = 0x20
}

public enum ControlAuth : byte
{
    Unknown = 0,
    Failed = 1,
    Passed = 2
}

public enum ControlZone : byte
{
    None = 0,
    Sensing = 1,
    Welcome = 2,
    Unlock = 3
}

public enum ControlEvent : byte
{
    Steady = 0,
    Discovered = 1,
    EnterWelcome = 2,
    EnterUnlock = 3,
    LeaveUnlock = 4,
    LeaveWelcome = 5,
    Lost = 6
}

[Flags]
public enum ControlStateFlags : byte
{
    None = 0,
    KeyPresent = 0x01,
    IdMatched = 0x02,
    WelcomeActive = 0x04,
    Unlocked = 0x08,
    Locked = 0x10
}

public sealed record ControlFrame(
    ControlMessageType Type,
    byte Sequence,
    byte[] Payload,
    DateTime ReceivedAt);

public sealed record ControlLockStatus(
    byte KeyId,
    ControlAuth Auth,
    ControlZone Zone,
    ControlEvent Event,
    ushort DistanceMm,
    sbyte AngleDeg,
    ControlStateFlags State,
    byte Quality);

public static class DigitalKeyControlProtocol
{
    public const byte Header0 = 0xAA;
    public const byte Header1 = 0x55;
    public const byte Version = 0x01;
    public const int MaximumPayloadLength = 16;

    public static byte[] BuildIdReport(byte sequence, int allowedKeyId) =>
        Encode(ControlMessageType.IdReport, sequence, new[] { (byte)(allowedKeyId & 0x0F) });

    public static byte[] BuildLockStatus(byte sequence, ControlLockStatus status)
    {
        var payload = new byte[9];
        payload[0] = (byte)(status.KeyId & 0x0F);
        payload[1] = (byte)status.Auth;
        payload[2] = (byte)status.Zone;
        payload[3] = (byte)status.Event;
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(4, 2), status.DistanceMm);
        payload[6] = unchecked((byte)status.AngleDeg);
        payload[7] = (byte)status.State;
        payload[8] = status.Quality;
        return Encode(ControlMessageType.LockStatus, sequence, payload);
    }

    public static byte[] Encode(ControlMessageType type, byte sequence, ReadOnlySpan<byte> payload)
    {
        if (payload.Length > MaximumPayloadLength)
            throw new ArgumentOutOfRangeException(nameof(payload));

        var frame = new byte[8 + payload.Length];
        frame[0] = Header0;
        frame[1] = Header1;
        frame[2] = Version;
        frame[3] = (byte)type;
        frame[4] = sequence;
        frame[5] = (byte)payload.Length;
        payload.CopyTo(frame.AsSpan(6));
        var crc = ComputeCrc(frame.AsSpan(2, 4 + payload.Length));
        BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(6 + payload.Length, 2), crc);
        return frame;
    }

    public static ushort ComputeCrc(ReadOnlySpan<byte> data)
    {
        ushort crc = 0xFFFF;
        foreach (var value in data)
        {
            crc ^= (ushort)(value << 8);
            for (var bit = 0; bit < 8; bit++)
                crc = (ushort)((crc & 0x8000) != 0 ? (crc << 1) ^ 0x1021 : crc << 1);
        }
        return crc;
    }
}

public sealed class ControlFrameParser
{
    private readonly List<byte> _buffer = new(512);

    public int CrcErrorCount { get; private set; }

    public IEnumerable<ControlFrame> Append(ReadOnlySpan<byte> incoming)
    {
        foreach (var value in incoming)
            _buffer.Add(value);

        var frames = new List<ControlFrame>();
        while (true)
        {
            var headerIndex = FindHeader();
            if (headerIndex < 0)
            {
                if (_buffer.Count > 0 && _buffer[^1] == DigitalKeyControlProtocol.Header0)
                {
                    var last = _buffer[^1];
                    _buffer.Clear();
                    _buffer.Add(last);
                }
                else
                {
                    _buffer.Clear();
                }
                break;
            }

            if (headerIndex > 0)
                _buffer.RemoveRange(0, headerIndex);
            if (_buffer.Count < 6)
                break;

            if (_buffer[2] != DigitalKeyControlProtocol.Version ||
                _buffer[5] > DigitalKeyControlProtocol.MaximumPayloadLength)
            {
                _buffer.RemoveAt(0);
                continue;
            }

            var payloadLength = _buffer[5];
            var frameLength = 8 + payloadLength;
            if (_buffer.Count < frameLength)
                break;

            var raw = _buffer.GetRange(0, frameLength).ToArray();
            _buffer.RemoveRange(0, frameLength);
            var expectedCrc = DigitalKeyControlProtocol.ComputeCrc(raw.AsSpan(2, 4 + payloadLength));
            var receivedCrc = BinaryPrimitives.ReadUInt16LittleEndian(raw.AsSpan(6 + payloadLength, 2));
            if (expectedCrc != receivedCrc)
            {
                CrcErrorCount++;
                continue;
            }

            frames.Add(new ControlFrame(
                (ControlMessageType)raw[3],
                raw[4],
                raw.AsSpan(6, payloadLength).ToArray(),
                DateTime.Now));
        }
        return frames;
    }

    public void Reset()
    {
        _buffer.Clear();
        CrcErrorCount = 0;
    }

    private int FindHeader()
    {
        for (var i = 0; i < _buffer.Count - 1; i++)
        {
            if (_buffer[i] == DigitalKeyControlProtocol.Header0 &&
                _buffer[i + 1] == DigitalKeyControlProtocol.Header1)
                return i;
        }
        return -1;
    }
}
