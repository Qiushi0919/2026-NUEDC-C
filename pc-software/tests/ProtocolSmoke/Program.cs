using DigitalKeyLab;
using System.Diagnostics;
using System.IO.Ports;
using System.Text.Json;

static byte[] Hex(string value) => value.Split(' ', StringSplitOptions.RemoveEmptyEntries)
    .Select(part => Convert.ToByte(part, 16)).ToArray();

static void Expect(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var defaultSettings = new AppSettings();
Expect(defaultSettings.AutoConnect &&
       defaultSettings.PreferredPort == AppSettings.AnchorPortName &&
       defaultSettings.PreferredControlPort == AppSettings.ControlPortName,
    "Fresh installations must default to base station COM22 and Bluetooth/control COM21");
Expect(defaultSettings.OutputSmoothingWindow == 30 &&
       defaultSettings.OutputSmoothingMethod == nameof(SmoothingMethod.Median),
    "Fresh installations must default to 30-point median output smoothing");
Expect(defaultSettings.GlobalCalibrationAdjustments.Count == 70 &&
       defaultSettings.GlobalCalibrationAdjustments.All(item =>
           item.DistanceCorrectionM == 0 && item.AngleCorrectionDeg == 0),
    "Global calibration must provide 10 angle ranges x 7 distance ranges with zero defaults");
defaultSettings.GlobalCalibrationAdjustments[0].DistanceCorrectionM = 1.5m;
defaultSettings.GlobalCalibrationAdjustments[0].AngleCorrectionDeg = -2.2m;
var restoredSettings = JsonSerializer.Deserialize<AppSettings>(
    JsonSerializer.Serialize(defaultSettings))!;
Expect(restoredSettings.GlobalCalibrationAdjustments.Count == 70 &&
       restoredSettings.GlobalCalibrationAdjustments[0].DistanceCorrectionM == 1.5m &&
       restoredSettings.GlobalCalibrationAdjustments[0].AngleCorrectionDeg == -2.2m,
    "Global calibration corrections must survive settings serialization");

var globalAdjustments = GlobalCalibrationModel.Normalize(new[]
{
    new GlobalCalibrationAdjustment
    {
        AngleRangeIndex = 6,
        DistanceRangeIndex = 2,
        DistanceCorrectionM = 0.2m,
        AngleCorrectionDeg = -1.5m
    },
    new GlobalCalibrationAdjustment
    {
        AngleRangeIndex = 9,
        DistanceRangeIndex = 6,
        DistanceCorrectionM = -0.1m,
        AngleCorrectionDeg = 0.5m
    }
});
var globallyCalibrated = GlobalCalibrationModel.Apply(1.25, 12.0, globalAdjustments);
Expect(Math.Abs(globallyCalibrated.DistanceM - 1.45) < 1e-9 &&
       Math.Abs(globallyCalibrated.AngleDeg - 10.5) < 1e-9 &&
       globallyCalibrated.AngleRangeIndex == 6 && globallyCalibrated.DistanceRangeIndex == 2,
    "Global calibration must use the cell selected by raw distance and raw angle");
var boundaryCalibrated = GlobalCalibrationModel.Apply(3.50, 50.0, globalAdjustments);
Expect(Math.Abs(boundaryCalibrated.DistanceM - 3.40) < 1e-9 &&
       Math.Abs(boundaryCalibrated.AngleDeg - 50.5) < 1e-9,
    "Global calibration must include the final 50-degree and 3.50-meter boundaries");
var negativeOuterBoundary = GlobalCalibrationModel.Apply(0.25, -50.0, globalAdjustments);
Expect(negativeOuterBoundary.HasMatchingRange &&
       negativeOuterBoundary.AngleRangeIndex == 0 &&
       negativeOuterBoundary.DistanceRangeIndex == 0,
    "Global calibration must include the negative 50-degree boundary");
var nextRange = GlobalCalibrationModel.Apply(1.50, 20.0, globalAdjustments);
Expect(Math.Abs(nextRange.DistanceM - 1.50) < 1e-9 &&
       Math.Abs(nextRange.AngleDeg - 20.0) < 1e-9 &&
       nextRange.AngleRangeIndex == 7 && nextRange.DistanceRangeIndex == 3,
    "Shared boundaries must belong to the range beginning at that boundary");
var outsideGlobalRange = GlobalCalibrationModel.Apply(3.60, 51.0, globalAdjustments);
Expect(!outsideGlobalRange.HasMatchingRange &&
       Math.Abs(outsideGlobalRange.DistanceM - 3.60) < 1e-9 &&
       Math.Abs(outsideGlobalRange.AngleDeg - 51.0) < 1e-9,
    "Measurements outside the configured grid must pass through unchanged");
Expect(GlobalCalibrationModel.AddCorrection(0, 0.01m) == 0.01m &&
       GlobalCalibrationModel.AddCorrection(0.01m, -0.1m) == -0.09m,
    "Global calibration must preserve one-centimeter distance adjustments");
var clearableAdjustments = GlobalCalibrationModel.CreateDefaultAdjustments();
clearableAdjustments[0].DistanceCorrectionM = 1.25m;
clearableAdjustments[^1].AngleCorrectionDeg = -3.5m;
GlobalCalibrationModel.ClearAll(clearableAdjustments);
Expect(clearableAdjustments.All(item =>
        item.DistanceCorrectionM == 0 && item.AngleCorrectionDeg == 0),
    "One-click global calibration reset must clear every saved correction");

Expect(CalibrationModel.ClampDisplayAngle(52.0) == 45.0 &&
       CalibrationModel.ClampDisplayAngle(-51.0) == -45.0 &&
       CalibrationModel.ClampDisplayAngle(12.5) == 12.5,
    "Final displayed angle must be clamped to plus or minus 45 degrees");

var frameAverager = new ValidFrameAverager(6);
AveragedRawMeasurement averagedRaw = default;
for (var index = 0; index < 5; index++)
{
    Expect(!frameAverager.TryAdd(index + 1, -5 + index * 2, out averagedRaw),
        $"Valid frame {index + 1} must wait for a complete six-frame average");
}
Expect(frameAverager.TryAdd(6, 5, out averagedRaw) &&
       averagedRaw.SourceFrameCount == 6 &&
       Math.Abs(averagedRaw.DistanceM - 3.5) < 1e-9 &&
       Math.Abs(averagedRaw.AngleDeg) < 1e-9,
    "Six valid frames must produce their arithmetic mean for distance and angle");
Expect(!frameAverager.TryAdd(10, 10, out averagedRaw),
    "A new averaging group must start after each six-frame output");
frameAverager.Reset();
for (var index = 1; index <= 5; index++)
    Expect(!frameAverager.TryAdd(2, 12, out averagedRaw),
        $"Reset averaging frame {index} must wait for the sixth frame");
Expect(frameAverager.TryAdd(2, 12, out averagedRaw) &&
       Math.Abs(averagedRaw.DistanceM - 2) < 1e-9 &&
       Math.Abs(averagedRaw.AngleDeg - 12) < 1e-9,
    "The sixth valid frame after reset must output the new group mean");

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

var smoother = new MeasurementSmoother();
Expect(smoother.WindowSize == 30 && smoother.Method == SmoothingMethod.Mean,
    "Output smoother defaults must be 30-point mean");
Expect(Math.Abs(MeasurementSmoother.EstimateDelaySeconds(30, 10) - 1.45) < 1e-9,
    "30-point smoothing delay at 10Hz must be 1.45 seconds");
Expect(double.IsNaN(MeasurementSmoother.EstimateDelaySeconds(30, 0)),
    "Unknown frame rate must not report a fake smoothing delay");
smoother.Configure(3, SmoothingMethod.Mean);
smoother.Add(1, 10);
smoother.Add(2, 20);
var mean = smoother.Add(3, 30);
Expect(Math.Abs(mean.DistanceM - 2) < 1e-9 && Math.Abs(mean.AngleDeg - 20) < 1e-9,
    "Three-point mean smoothing mismatch");
mean = smoother.Add(7, 70);
Expect(Math.Abs(mean.DistanceM - 4) < 1e-9 && Math.Abs(mean.AngleDeg - 40) < 1e-9,
    "Sliding mean must keep only the latest samples");
smoother.Configure(3, SmoothingMethod.Median);
var median = smoother.Current();
Expect(Math.Abs(median.DistanceM - 3) < 1e-9 && Math.Abs(median.AngleDeg - 30) < 1e-9,
    "Median smoothing mismatch");
smoother.Configure(2, SmoothingMethod.Mean);
mean = smoother.Current();
Expect(mean.SampleCount == 2 && Math.Abs(mean.DistanceM - 5) < 1e-9 && Math.Abs(mean.AngleDeg - 50) < 1e-9,
    "Changing the window must retain the newest samples");

var angleCases = new (double Raw, double Expected)[]
{
    (-20, -25), (-15, -20), (-12.5, -15), (-10, -10),
    (0, 0), (10, 10), (12.5, 15), (15, 20), (20, 25)
};
foreach (var (raw, expected) in angleCases)
{
    Expect(Math.Abs(CalibrationModel.CorrectAngle(raw) - expected) < 1e-9,
        $"Piecewise angle mapping mismatch: raw={raw}, expected={expected}");
}
var calibrated = CalibrationModel.Apply(1.25, 12.5);
Expect(Math.Abs(calibrated.DistanceM - 1.25) < 1e-9 && Math.Abs(calibrated.AngleDeg - 15.0) < 1e-9,
    "Field calibration must preserve center distance and apply y=2x-10 in the positive transition");
var baseline = CalibrationModel.ApplyBaseline(1.25, 12.5);
Expect(Math.Abs(baseline.DistanceM - 1.25) < 1e-9 && Math.Abs(baseline.AngleDeg - 12.5) < 1e-9,
    "Baseline mode must preserve center distance and the raw sensor angle");

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
