using DigitalKeyLab;
using System.Diagnostics;
using System.IO.Ports;

static byte[] Hex(string value) => value.Split(' ', StringSplitOptions.RemoveEmptyEntries)
    .Select(part => Convert.ToByte(part, 16)).ToArray();

static void Expect(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

if (args.Length == 2 && args[0] == "--live")
{
    var liveParser = new ProtocolParser();
    using var port = new SerialPort(args[1], 115200, Parity.None, 8, StopBits.One)
    {
        ReadTimeout = 250,
        DtrEnable = false,
        RtsEnable = false
    };
    port.Open();
    var watch = Stopwatch.StartNew();
    var positions = 0;
    var heartbeats = 0;
    while (watch.Elapsed < TimeSpan.FromSeconds(4))
    {
        var count = port.BytesToRead;
        if (count == 0)
        {
            Thread.Sleep(20);
            continue;
        }
        var bytes = new byte[count];
        var read = port.Read(bytes, 0, bytes.Length);
        foreach (var frame in liveParser.Append(bytes.AsSpan(0, read)))
        {
            if (frame.Kind == FrameKind.Position)
            {
                positions++;
                if (positions <= 6)
                    Console.WriteLine($"LIVE_POSITION seq={frame.Sequence} anchor={frame.AnchorIdHex} tag={frame.TagIdHex} id4={frame.FourBitId} distance={frame.DistanceM:F2}m angle={frame.AzimuthDeg}deg elevation={frame.ElevationDeg} checksum={frame.ChecksumValid} raw={Convert.ToHexString(frame.Raw)}");
            }
            else if (frame.Kind == FrameKind.Heartbeat)
            {
                heartbeats++;
            }
        }
    }
    port.Close();
    Console.WriteLine($"LIVE_TEST_PASS positions={positions} heartbeats={heartbeats} duration={watch.Elapsed.TotalSeconds:F1}s");
    Expect(positions + heartbeats > 0, "No valid live frames received");
    return;
}

var parser = new ProtocolParser();
var positionBytes = Hex("FF FF FF FF 00 25 00 0B 20 01 01 00 00 00 AA A2 00 00 AA A1 00 00 00 19 00 12 FF CA 12 34 00 0B 00 00 00 00 1E");
var firstHalf = positionBytes.AsSpan(0, 13);
var secondHalf = positionBytes.AsSpan(13);
Expect(!parser.Append(firstHalf).Any(), "Fragmented frame parsed too early");
var frames = parser.Append(secondHalf).ToArray();
Expect(frames.Length == 1, "Position frame count mismatch");
var p = frames[0];
Expect(p.Kind == FrameKind.Position, "Position command not recognized");
Expect(p.PacketLength == 37, "Packet length mismatch");
Expect(p.AnchorId == 0x0000AAA2, "Anchor ID mismatch");
Expect(p.TagId == 0x0000AAA1, "Tag ID mismatch");
Expect(p.FourBitId == 1, "Four-bit ID mismatch");
Expect(p.DistanceCm == 25, "Distance mismatch");
Expect(p.AzimuthDeg == 18, "Azimuth mismatch");
Expect(p.ElevationDeg == -54, "Elevation mismatch");
Expect(p.ChecksumValid, "Checksum mismatch");

var heartbeat = Hex("00 7E FF FF FF FF 00 10 00 A0 20 02 01 02 00 00 12 D5");
var h = parser.Append(heartbeat).Single();
Expect(h.Kind == FrameKind.Heartbeat, "Heartbeat command not recognized");
Expect(h.AnchorId == 0x000012D5, "Heartbeat anchor ID mismatch");

var unlocked = DoorLogic.Evaluate(true, 0.8, 0, 1, 1);
Expect(unlocked.Unlocked && unlocked.LightOn && unlocked.IdentityMatched, "Unlock decision mismatch");
var welcome = DoorLogic.Evaluate(true, 1.5, 10, 1, 1);
Expect(!welcome.Unlocked && welcome.LightOn && welcome.Zone == RangeZone.Welcome, "Welcome decision mismatch");
var sensing = DoorLogic.Evaluate(true, 2.5, -20, 1, 1);
Expect(!sensing.Unlocked && !sensing.LightOn && sensing.Zone == RangeZone.Sensing, "Sensing decision mismatch");
var outside = DoorLogic.Evaluate(true, 3.1, 0, 1, 1);
Expect(outside.Zone == RangeZone.OutsideRange, "Outside-range decision mismatch");
var wrongId = DoorLogic.Evaluate(true, 0.5, 0, 2, 1);
Expect(!wrongId.IdentityMatched && !wrongId.Unlocked && !wrongId.LightOn, "Wrong-ID safety mismatch");
var wrongAngle = DoorLogic.Evaluate(true, 0.5, 46, 1, 1);
Expect(wrongAngle.Zone == RangeZone.OutsideAngle && !wrongAngle.Unlocked, "Angle boundary mismatch");

for (var keyIdentityId = 0; keyIdentityId <= 15; keyIdentityId++)
{
    var matching = DoorLogic.Evaluate(true, 0.5, 0, keyIdentityId, keyIdentityId);
    Expect(matching.IdentityMatched && matching.Unlocked, $"Key ID {keyIdentityId} should match the DIP allowed ID");
    var different = DoorLogic.Evaluate(true, 0.5, 0, keyIdentityId, (keyIdentityId + 1) & 0xF);
    Expect(!different.IdentityMatched && !different.Unlocked, $"Key ID {keyIdentityId} should be rejected by a different DIP allowed ID");
}

var noAllowedId = DoorLogic.Evaluate(true, 0.5, 0, 5, null);
Expect(!noAllowedId.IdentityMatched && !noAllowedId.Unlocked, "Missing DIP allowed ID must keep the lock closed");

var idReportExpected = Hex("AA 55 01 10 00 01 05 6E C3");
var idReportBuilt = DigitalKeyControlProtocol.BuildIdReport(0, 5);
Expect(idReportBuilt.SequenceEqual(idReportExpected), "DIP ID_REPORT frame mismatch");

var controlParser = new ControlFrameParser();
Expect(!controlParser.Append(idReportExpected.AsSpan(0, 4)).Any(), "Control frame parsed before completion");
var idFrames = controlParser.Append(idReportExpected.AsSpan(4)).ToArray();
Expect(idFrames.Length == 1 && idFrames[0].Type == ControlMessageType.IdReport, "ID_REPORT parse mismatch");
Expect(idFrames[0].Payload.SequenceEqual(new byte[] { 5 }), "DIP payload mismatch");

var status = new ControlLockStatus(
    5,
    ControlAuth.Passed,
    ControlZone.Welcome,
    ControlEvent.EnterWelcome,
    1500,
    15,
    ControlStateFlags.KeyPresent | ControlStateFlags.IdMatched |
        ControlStateFlags.WelcomeActive | ControlStateFlags.Locked,
    80);
var statusExpected = Hex("AA 55 01 20 00 09 05 02 02 02 DC 05 0F 17 50 A4 D9");
var statusBuilt = DigitalKeyControlProtocol.BuildLockStatus(0, status);
Expect(statusBuilt.SequenceEqual(statusExpected), "LOCK_STATUS frame mismatch");
var statusFrame = controlParser.Append(statusBuilt).Single();
Expect(statusFrame.Type == ControlMessageType.LockStatus && statusFrame.Payload.Length == 9,
    "LOCK_STATUS parse mismatch");

var damaged = idReportExpected.ToArray();
damaged[^1] ^= 0x01;
Expect(!controlParser.Append(damaged).Any() && controlParser.CrcErrorCount == 1,
    "CRC error frame should be rejected");

Console.WriteLine("PROTOCOL_SMOKE_TEST_PASS");
Console.WriteLine($"Parsed ID={p.TagIdHex}, distance={p.DistanceM:F2}m, azimuth={p.AzimuthDeg}deg, checksum={p.ChecksumValid}");
