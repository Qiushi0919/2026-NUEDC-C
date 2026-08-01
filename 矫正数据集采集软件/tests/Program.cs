using System.Buffers.Binary;
using System.Drawing;
using System.Globalization;
using CalibrationCollector;

var now = DateTimeOffset.Now;
var anchorRaw = BuildAnchorFrame();
var anchorParser = new AnchorProtocolParser();
Assert(anchorParser.Append(anchorRaw.AsSpan(0, 11), now).Count == 0, "partial anchor frame must wait");
var anchorFrames = anchorParser.Append(anchorRaw.AsSpan(11), now);
Assert(anchorFrames.Count == 1, "anchor frame count");
var anchorFrame = anchorFrames[0];
Assert(anchorFrame.Kind == AnchorFrameKind.Position, "anchor frame kind");
Assert(anchorFrame.ChecksumValid, "anchor checksum");
Assert(anchorFrame.RawDistanceM == 1.25, "raw center distance");
Assert(Math.Abs(anchorFrame.CorrectedDistanceM - 1.00) < 0.0001, "minus 0.25m correction");
Assert(anchorFrame.AzimuthDeg == -15, "azimuth");
Assert(anchorFrame.FourBitId == 10, "four bit id");

var calibrationProcessor = new MeasurementProcessor();
ProcessedMeasurement processed = default;
for (var i = 0; i < MeasurementProcessor.OutputWindowSize; i++)
    processed = calibrationProcessor.Add(1.25, 12.5);
Assert(processed.PrefilterSampleCount == 5, "five-point prefilter must fill at five samples");
Assert(processed.SmoothingSampleCount == 30 && processed.SmoothingReady,
    "30-point median must report ready after 30 valid samples");
Assert(Math.Abs(processed.SmoothedDistanceM - 1.0) < 1e-8,
    "collector distance must use the deployed calibration model before smoothing");
Assert(Math.Abs(processed.SmoothedAngleDeg - 15.0) < 1e-8,
    "collector angle must use the deployed calibration model before smoothing");

var bluetoothRaw = BuildBluetoothFrame(10);
var bluetoothParser = new BluetoothProtocolParser();
var bluetoothFrames = bluetoothParser.Append(bluetoothRaw, now);
Assert(bluetoothFrames.Count == 1, "bluetooth frame count");
Assert(bluetoothFrames[0].CrcValid, "bluetooth crc");
Assert(bluetoothFrames[0].AllowedKeyId == 10, "DIP allowed id");

var testRoot = Path.Combine(AppContext.BaseDirectory, "smoke-data");
if (Directory.Exists(testRoot))
    Directory.Delete(testRoot, true);
var recorder = new SessionRecorder(testRoot, 1.00, -15.0, 20.0);
recorder.WriteRaw(new SerialRead("COM22", "anchor", now, anchorRaw));
var frameProcessed = new MeasurementProcessor().Add(anchorFrame.RawDistanceM, anchorFrame.AzimuthDeg);
recorder.WriteMeasurement(anchorFrame, frameProcessed, 10);
recorder.WriteBluetooth(bluetoothFrames[0]);
recorder.Stop("smoke_test");

var measurementPath = Path.Combine(recorder.SessionFolder, "measurements.csv");
var measurementText = File.ReadAllText(measurementPath);
Assert(measurementText.Contains("expected_center_distance_m"), "center-distance column");
Assert(measurementText.Contains("smoothed_subject_distance_m"), "smoothed distance column");
Assert(measurementText.Contains("smoothing_ready"), "smoothing warmup marker");
Assert(measurementText.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length == 2, "one measurement row");
var measurementLines = measurementText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
var measurementHeaders = measurementLines[0].TrimStart('\uFEFF').Split(',');
var measurementFields = measurementLines[1].Split(',');
Assert(Math.Abs(Field("actual_subject_distance_m") - 1.0) < 1e-9, "subject distance value");
Assert(Math.Abs(Field("expected_center_distance_m") - 1.25) < 1e-9, "expected center distance value");
Assert(Math.Abs(Field("raw_center_distance_m") - 1.25) < 1e-9, "raw center distance must be retained");
Assert(Math.Abs(Field("smoothed_subject_distance_m") - frameProcessed.SmoothedDistanceM) < 1e-3,
    "processed distance must be written alongside the raw frame");
Assert(File.Exists(Path.Combine(recorder.SessionFolder, "raw_serial.csv")), "raw serial file");
Assert(File.Exists(Path.Combine(recorder.SessionFolder, "bluetooth_frames.csv")), "bluetooth file");
Assert(File.Exists(Path.Combine(recorder.SessionFolder, "session.json")), "session metadata");
Directory.Delete(testRoot, true);

using (var form = new MainForm())
using (var bitmap = new Bitmap(form.Width, form.Height))
{
    form.StartPosition = FormStartPosition.Manual;
    form.Location = new Point(-32000, -32000);
    form.Show();
    Application.DoEvents();
    form.PerformLayout();
    form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
    bitmap.Save(Path.Combine(AppContext.BaseDirectory, "ui-preview.png"));
    form.Close();
}

Console.WriteLine("PASS: protocol parsing, 1.00m->1.25m field distance convention, and dataset writing");

double Field(string name)
{
    var index = Array.IndexOf(measurementHeaders, name);
    Assert(index >= 0, "missing CSV field " + name);
    return double.Parse(measurementFields[index], CultureInfo.InvariantCulture);
}

static byte[] BuildAnchorFrame()
{
    var raw = new byte[37];
    raw.AsSpan(0, 4).Fill(0xFF);
    BinaryPrimitives.WriteUInt16BigEndian(raw.AsSpan(4, 2), 37);
    BinaryPrimitives.WriteUInt16BigEndian(raw.AsSpan(6, 2), 0x1234);
    BinaryPrimitives.WriteUInt16BigEndian(raw.AsSpan(8, 2), 0x2001);
    BinaryPrimitives.WriteUInt16BigEndian(raw.AsSpan(10, 2), 1);
    BinaryPrimitives.WriteUInt32BigEndian(raw.AsSpan(12, 4), 0x11223344);
    BinaryPrimitives.WriteUInt32BigEndian(raw.AsSpan(16, 4), 0x1234560A);
    BinaryPrimitives.WriteUInt32BigEndian(raw.AsSpan(20, 4), 125);
    BinaryPrimitives.WriteInt16BigEndian(raw.AsSpan(24, 2), -15);
    BinaryPrimitives.WriteInt16BigEndian(raw.AsSpan(26, 2), 3);
    BinaryPrimitives.WriteUInt16BigEndian(raw.AsSpan(28, 2), 2);
    BinaryPrimitives.WriteUInt16BigEndian(raw.AsSpan(30, 2), 9);
    byte checksum = 0;
    for (var i = 0; i < raw.Length - 1; i++)
        checksum ^= raw[i];
    raw[^1] = checksum;
    return raw;
}

static byte[] BuildBluetoothFrame(byte id)
{
    var raw = new byte[] { 0xAA, 0x55, 0x01, 0x10, 0x07, 0x01, id, 0x00, 0x00 };
    var crc = BluetoothProtocolParser.ComputeCrc(raw.AsSpan(2, 5));
    BinaryPrimitives.WriteUInt16LittleEndian(raw.AsSpan(7, 2), crc);
    return raw;
}

static void Assert(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException("FAIL: " + message);
}
