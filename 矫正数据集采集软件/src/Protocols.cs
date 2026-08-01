using System.Buffers.Binary;
using DigitalKeyLab;

namespace CalibrationCollector;

public enum AnchorFrameKind
{
    Unknown,
    Position,
    Heartbeat
}

public sealed record AnchorFrame(
    AnchorFrameKind Kind,
    ushort PacketLength,
    ushort Sequence,
    ushort Command,
    ushort Version,
    uint AnchorId,
    uint TagId,
    uint DistanceCm,
    short AzimuthDeg,
    short ElevationDeg,
    ushort TagStatus,
    ushort BatchSequence,
    bool ChecksumValid,
    DateTimeOffset ReceivedAt,
    byte[] Raw)
{
    public double RawDistanceM => DistanceCm / 100.0;
    public double CorrectedDistanceM => Math.Max(0, RawDistanceM - CalibrationModel.DistanceCorrectionM);
    public int FourBitId => (int)(TagId & 0x0F);
}

public sealed class AnchorProtocolParser
{
    private readonly List<byte> _buffer = new(4096);

    public IReadOnlyList<AnchorFrame> Append(ReadOnlySpan<byte> incoming, DateTimeOffset receivedAt)
    {
        foreach (var value in incoming)
            _buffer.Add(value);

        var parsed = new List<AnchorFrame>();
        while (true)
        {
            var headerIndex = FindHeader();
            if (headerIndex < 0)
            {
                var keep = 0;
                for (var i = _buffer.Count - 1; i >= 0 && _buffer[i] == 0xFF && keep < 3; i--)
                    keep++;
                if (_buffer.Count > keep)
                    _buffer.RemoveRange(0, _buffer.Count - keep);
                break;
            }

            if (headerIndex > 0)
                _buffer.RemoveRange(0, headerIndex);
            if (_buffer.Count < 6)
                break;

            var packetLength = (ushort)((_buffer[4] << 8) | _buffer[5]);
            if (packetLength < 10 || packetLength > 1024)
            {
                _buffer.RemoveAt(0);
                continue;
            }
            if (_buffer.Count < packetLength)
                break;

            var raw = _buffer.GetRange(0, packetLength).ToArray();
            _buffer.RemoveRange(0, packetLength);
            parsed.Add(Parse(raw, receivedAt));
        }

        return parsed;
    }

    public void Reset() => _buffer.Clear();

    private int FindHeader()
    {
        for (var i = 0; i <= _buffer.Count - 4; i++)
        {
            if (_buffer[i] == 0xFF && _buffer[i + 1] == 0xFF &&
                _buffer[i + 2] == 0xFF && _buffer[i + 3] == 0xFF)
                return i;
        }
        return -1;
    }

    private static AnchorFrame Parse(byte[] raw, DateTimeOffset receivedAt)
    {
        var data = raw.AsSpan();
        var length = ReadU16(data, 4);
        var sequence = ReadU16(data, 6);
        var command = ReadU16(data, 8);
        var version = data.Length >= 12 ? ReadU16(data, 10) : (ushort)0;
        var anchorId = data.Length >= 16 ? ReadU32(data, 12) : 0u;

        if (command == 0x2001 && data.Length >= 37)
        {
            byte checksum = 0;
            for (var i = 0; i < data.Length - 1; i++)
                checksum ^= data[i];
            return new AnchorFrame(
                AnchorFrameKind.Position, length, sequence, command, version, anchorId,
                ReadU32(data, 16), ReadU32(data, 20), ReadI16(data, 24), ReadI16(data, 26),
                ReadU16(data, 28), ReadU16(data, 30), checksum == data[^1], receivedAt, raw);
        }

        return new AnchorFrame(
            command == 0x2002 ? AnchorFrameKind.Heartbeat : AnchorFrameKind.Unknown,
            length, sequence, command, version, anchorId, 0, 0, 0, 0, 0, 0, true, receivedAt, raw);
    }

    private static ushort ReadU16(ReadOnlySpan<byte> data, int offset) =>
        BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset, 2));

    private static short ReadI16(ReadOnlySpan<byte> data, int offset) =>
        BinaryPrimitives.ReadInt16BigEndian(data.Slice(offset, 2));

    private static uint ReadU32(ReadOnlySpan<byte> data, int offset) =>
        BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4));
}

public sealed record BluetoothFrame(
    byte Type,
    byte Sequence,
    byte[] Payload,
    bool CrcValid,
    DateTimeOffset ReceivedAt,
    byte[] Raw)
{
    public int? AllowedKeyId => Type == 0x10 && Payload.Length == 1 && Payload[0] <= 0x0F
        ? Payload[0]
        : null;
}

public sealed class BluetoothProtocolParser
{
    private const int MaximumPayloadLength = 16;
    private readonly List<byte> _buffer = new(512);

    public IReadOnlyList<BluetoothFrame> Append(ReadOnlySpan<byte> incoming, DateTimeOffset receivedAt)
    {
        foreach (var value in incoming)
            _buffer.Add(value);

        var parsed = new List<BluetoothFrame>();
        while (true)
        {
            var headerIndex = FindHeader();
            if (headerIndex < 0)
            {
                if (_buffer.Count > 0 && _buffer[^1] == 0xAA)
                {
                    _buffer.Clear();
                    _buffer.Add(0xAA);
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

            if (_buffer[2] != 0x01 || _buffer[5] > MaximumPayloadLength)
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
            var expected = ComputeCrc(raw.AsSpan(2, 4 + payloadLength));
            var received = BinaryPrimitives.ReadUInt16LittleEndian(raw.AsSpan(6 + payloadLength, 2));
            parsed.Add(new BluetoothFrame(
                raw[3], raw[4], raw.AsSpan(6, payloadLength).ToArray(),
                expected == received, receivedAt, raw));
        }

        return parsed;
    }

    public void Reset() => _buffer.Clear();

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

    private int FindHeader()
    {
        for (var i = 0; i < _buffer.Count - 1; i++)
        {
            if (_buffer[i] == 0xAA && _buffer[i + 1] == 0x55)
                return i;
        }
        return -1;
    }
}
