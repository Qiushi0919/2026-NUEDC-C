using System.Buffers.Binary;

namespace DigitalKeyLab;

public enum FrameKind
{
    Unknown,
    Position,
    Heartbeat
}

public sealed record AnchorFrame(
    FrameKind Kind,
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
    DateTime ReceivedAt,
    byte[] Raw)
{
    public double DistanceM => DistanceCm / 100.0;
    public int FourBitId => (int)(TagId & 0x0F);
    public string TagIdHex => $"0x{TagId:X8}";
    public string AnchorIdHex => $"0x{AnchorId:X8}";
}

public sealed class ProtocolParser
{
    private readonly List<byte> _buffer = new(4096);

    public IEnumerable<AnchorFrame> Append(ReadOnlySpan<byte> incoming)
    {
        for (var i = 0; i < incoming.Length; i++)
            _buffer.Add(incoming[i]);

        var parsed = new List<AnchorFrame>();
        while (true)
        {
            var headerIndex = FindHeader();
            if (headerIndex < 0)
            {
                // Keep a possible partial FF FF FF prefix for the next read.
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
            parsed.Add(ParseFrame(raw));
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

    private static AnchorFrame ParseFrame(byte[] raw)
    {
        var data = raw.AsSpan();
        var length = ReadU16(data, 4);
        var sequence = ReadU16(data, 6);
        var command = ReadU16(data, 8);
        var version = data.Length >= 12 ? ReadU16(data, 10) : (ushort)0;
        var anchorId = data.Length >= 16 ? ReadU32(data, 12) : 0u;

        if (command == 0x2001 && data.Length >= 37)
        {
            var checksum = (byte)0;
            for (var i = 0; i < data.Length - 1; i++)
                checksum ^= data[i];

            return new AnchorFrame(
                FrameKind.Position,
                length,
                sequence,
                command,
                version,
                anchorId,
                ReadU32(data, 16),
                ReadU32(data, 20),
                ReadI16(data, 24),
                ReadI16(data, 26),
                ReadU16(data, 28),
                ReadU16(data, 30),
                checksum == data[^1],
                DateTime.Now,
                raw);
        }

        return new AnchorFrame(
            command == 0x2002 ? FrameKind.Heartbeat : FrameKind.Unknown,
            length,
            sequence,
            command,
            version,
            anchorId,
            0,
            0,
            0,
            0,
            0,
            0,
            true,
            DateTime.Now,
            raw);
    }

    private static ushort ReadU16(ReadOnlySpan<byte> data, int offset) =>
        BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset, 2));

    private static short ReadI16(ReadOnlySpan<byte> data, int offset) =>
        BinaryPrimitives.ReadInt16BigEndian(data.Slice(offset, 2));

    private static uint ReadU32(ReadOnlySpan<byte> data, int offset) =>
        BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4));
}
