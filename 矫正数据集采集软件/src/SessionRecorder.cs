using System.Globalization;
using System.Text;
using System.Text.Json;
using DigitalKeyLab;

namespace CalibrationCollector;

public sealed class SessionRecorder : IDisposable
{
    public const double DistanceCorrectionM = -CalibrationModel.DistanceCorrectionM;
    public const double CurrentAngleCorrectionDeg = 0.0;
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private readonly object _sync = new();
    private readonly StreamWriter _rawWriter;
    private readonly StreamWriter _measurementWriter;
    private readonly StreamWriter _bluetoothWriter;
    private readonly StreamWriter _eventWriter;
    private readonly double _actualDistanceM;
    private readonly double _actualAngleDeg;
    private readonly double _plannedDurationSec;
    private readonly DateTimeOffset _startedAt;
    private bool _stopped;

    public SessionRecorder(string dataRoot, double actualDistanceM, double actualAngleDeg, double durationSec)
    {
        _actualDistanceM = actualDistanceM;
        _actualAngleDeg = actualAngleDeg;
        _plannedDurationSec = durationSec;
        _startedAt = DateTimeOffset.Now;
        var folderName = $"{_startedAt:yyyyMMdd_HHmmss_fff}_D{actualDistanceM:F2}m_A{actualAngleDeg:+0.0;-0.0;0.0}deg";
        SessionFolder = Path.Combine(dataRoot, Sanitize(folderName));
        Directory.CreateDirectory(SessionFolder);

        _rawWriter = CreateWriter("raw_serial.csv");
        _measurementWriter = CreateWriter("measurements.csv");
        _bluetoothWriter = CreateWriter("bluetooth_frames.csv");
        _eventWriter = CreateWriter("events.csv");

        _rawWriter.WriteLine("timestamp_iso,elapsed_ms,port,role,byte_count,hex,ascii");
        _measurementWriter.WriteLine("timestamp_iso,elapsed_ms,actual_subject_distance_m,expected_center_distance_m,actual_angle_deg,raw_center_distance_m,current_corrected_subject_distance_m,raw_center_distance_error_m,corrected_subject_distance_error_m,raw_azimuth_deg,current_corrected_azimuth_deg,raw_angle_error_deg,current_corrected_angle_error_deg,prefilter_center_distance_m,prefilter_azimuth_deg,model_calibrated_subject_distance_m,model_calibrated_azimuth_deg,smoothed_subject_distance_m,smoothed_azimuth_deg,model_distance_error_m,model_angle_error_deg,smoothed_distance_error_m,smoothed_angle_error_deg,prefilter_sample_count,smoothing_sample_count,smoothing_ready,processing_model,smoothing_method,smoothing_window_points,elevation_deg,tag_id_hex,tag_id_4bit,anchor_id_hex,sequence,command_hex,packet_length,tag_status,batch_sequence,checksum_valid,latest_allowed_id,raw_hex");
        _bluetoothWriter.WriteLine("timestamp_iso,elapsed_ms,message_type_hex,sequence,payload_hex,crc_valid,allowed_key_id,raw_hex");
        _eventWriter.WriteLine("timestamp_iso,elapsed_ms,event,details");
        WriteEvent("recording_started", $"planned_duration={durationSec:F1}s");
        WriteMetadata(null, "recording");
    }

    public string SessionFolder { get; }
    public long RawChunkCount { get; private set; }
    public long PositionFrameCount { get; private set; }
    public long ValidPositionFrameCount { get; private set; }
    public long BluetoothFrameCount { get; private set; }
    public DateTimeOffset StartedAt => _startedAt;

    public void WriteRaw(SerialRead read)
    {
        lock (_sync)
        {
            if (_stopped)
                return;
            _rawWriter.WriteLine(string.Join(",",
                Csv(read.ReceivedAt.ToString("O")),
                Number(ElapsedMs(read.ReceivedAt)),
                Csv(read.PortName),
                Csv(read.Role),
                read.Bytes.Length.ToString(Inv),
                Csv(Convert.ToHexString(read.Bytes)),
                Csv(ToPrintableAscii(read.Bytes))));
            _rawWriter.Flush();
            RawChunkCount++;
        }
    }

    public void WriteMeasurement(AnchorFrame frame, ProcessedMeasurement? processed, int? latestAllowedId)
    {
        if (frame.Kind != AnchorFrameKind.Position)
            return;
        lock (_sync)
        {
            if (_stopped)
                return;
            var corrected = frame.CorrectedDistanceM;
            var expectedCenterDistance = _actualDistanceM - DistanceCorrectionM;
            var correctedAngle = frame.AzimuthDeg + CurrentAngleCorrectionDeg;
            _measurementWriter.WriteLine(string.Join(",",
                Csv(frame.ReceivedAt.ToString("O")),
                Number(ElapsedMs(frame.ReceivedAt)),
                Number(_actualDistanceM),
                Number(expectedCenterDistance),
                Number(_actualAngleDeg),
                Number(frame.RawDistanceM),
                Number(corrected),
                Number(frame.RawDistanceM - expectedCenterDistance),
                Number(corrected - _actualDistanceM),
                frame.AzimuthDeg.ToString(Inv),
                Number(correctedAngle),
                Number(frame.AzimuthDeg - _actualAngleDeg),
                Number(correctedAngle - _actualAngleDeg),
                Optional(processed?.PrefilterDistanceM),
                Optional(processed?.PrefilterAngleDeg),
                Optional(processed?.CalibratedDistanceM),
                Optional(processed?.CalibratedAngleDeg),
                Optional(processed?.SmoothedDistanceM),
                Optional(processed?.SmoothedAngleDeg),
                Optional(processed?.CalibratedDistanceM - _actualDistanceM),
                Optional(processed?.CalibratedAngleDeg - _actualAngleDeg),
                Optional(processed?.SmoothedDistanceM - _actualDistanceM),
                Optional(processed?.SmoothedAngleDeg - _actualAngleDeg),
                processed?.PrefilterSampleCount.ToString(Inv) ?? string.Empty,
                processed?.SmoothingSampleCount.ToString(Inv) ?? string.Empty,
                processed.HasValue ? (processed.Value.SmoothingReady ? "true" : "false") : string.Empty,
                processed.HasValue ? Csv(CalibrationModel.DisplayName) : string.Empty,
                processed.HasValue ? Csv("Median") : string.Empty,
                processed.HasValue ? MeasurementProcessor.OutputWindowSize.ToString(Inv) : string.Empty,
                frame.ElevationDeg.ToString(Inv),
                Csv($"0x{frame.TagId:X8}"),
                frame.FourBitId.ToString(Inv),
                Csv($"0x{frame.AnchorId:X8}"),
                frame.Sequence.ToString(Inv),
                Csv($"0x{frame.Command:X4}"),
                frame.PacketLength.ToString(Inv),
                frame.TagStatus.ToString(Inv),
                frame.BatchSequence.ToString(Inv),
                frame.ChecksumValid ? "true" : "false",
                latestAllowedId?.ToString(Inv) ?? string.Empty,
                Csv(Convert.ToHexString(frame.Raw))));
            _measurementWriter.Flush();
            PositionFrameCount++;
            if (frame.ChecksumValid)
                ValidPositionFrameCount++;
        }
    }

    public void WriteBluetooth(BluetoothFrame frame)
    {
        lock (_sync)
        {
            if (_stopped)
                return;
            _bluetoothWriter.WriteLine(string.Join(",",
                Csv(frame.ReceivedAt.ToString("O")),
                Number(ElapsedMs(frame.ReceivedAt)),
                Csv($"0x{frame.Type:X2}"),
                frame.Sequence.ToString(Inv),
                Csv(Convert.ToHexString(frame.Payload)),
                frame.CrcValid ? "true" : "false",
                frame.AllowedKeyId?.ToString(Inv) ?? string.Empty,
                Csv(Convert.ToHexString(frame.Raw))));
            _bluetoothWriter.Flush();
            BluetoothFrameCount++;
        }
    }

    public void WriteEvent(string eventName, string details)
    {
        lock (_sync)
        {
            if (_stopped)
                return;
            var now = DateTimeOffset.Now;
            _eventWriter.WriteLine(string.Join(",",
                Csv(now.ToString("O")), Number(ElapsedMs(now)), Csv(eventName), Csv(details)));
            _eventWriter.Flush();
        }
    }

    public void Stop(string reason)
    {
        lock (_sync)
        {
            if (_stopped)
                return;
            var endedAt = DateTimeOffset.Now;
            _eventWriter.WriteLine(string.Join(",",
                Csv(endedAt.ToString("O")), Number(ElapsedMs(endedAt)),
                Csv("recording_stopped"), Csv(reason)));
            _eventWriter.Flush();
            _stopped = true;
            _rawWriter.Dispose();
            _measurementWriter.Dispose();
            _bluetoothWriter.Dispose();
            _eventWriter.Dispose();
            WriteMetadata(endedAt, reason);
        }
    }

    private StreamWriter CreateWriter(string filename) => new(
        Path.Combine(SessionFolder, filename), false, new UTF8Encoding(true));

    private void WriteMetadata(DateTimeOffset? endedAt, string status)
    {
        var metadata = new
        {
            format_version = 2,
            status,
            started_at = _startedAt,
            ended_at = endedAt,
            actual_distance_m = _actualDistanceM,
            actual_distance_definition = "现场标定显示距离：期望基站中心距 = 实际距离 + 0.25m",
            expected_center_distance_m = _actualDistanceM - DistanceCorrectionM,
            actual_angle_deg = _actualAngleDeg,
            distance_correction_m = DistanceCorrectionM,
            corrected_distance_formula = "max(0, raw_distance_m - 0.25)",
            current_angle_correction_deg = CurrentAngleCorrectionDeg,
            current_corrected_angle_formula = "raw_azimuth_deg (no fixed angle offset)",
            processing = new
            {
                calibration_model = CalibrationModel.DisplayName,
                calibration_model_version = CalibrationModel.Version,
                prefilter = new { method = "median", window_points = MeasurementProcessor.PrefilterWindowSize },
                output_smoothing = new { method = "median", window_points = MeasurementProcessor.OutputWindowSize },
                pipeline = "raw -> 5-point median -> field piecewise calibration v2 -> 30-point median",
                warmup_note = "smoothing_ready=true only after 30 valid position frames; raw values are always retained"
            },
            planned_duration_sec = _plannedDurationSec,
            bluetooth = new { port = "COM21", baud = 115200, mode = "receive_only" },
            anchor = new { port = "COM22", baud = 115200, mode = "receive_only" },
            counts = new
            {
                raw_chunks = RawChunkCount,
                position_frames = PositionFrameCount,
                valid_position_frames = ValidPositionFrameCount,
                bluetooth_frames = BluetoothFrameCount
            }
        };
        File.WriteAllText(
            Path.Combine(SessionFolder, "session.json"),
            JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true }),
            new UTF8Encoding(true));
    }

    private double ElapsedMs(DateTimeOffset time) => (time - _startedAt).TotalMilliseconds;
    private static string Number(double value) => value.ToString("0.###", Inv);
    private static string Optional(double? value) => value.HasValue ? Number(value.Value) : string.Empty;
    private static string Csv(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
    private static string ToPrintableAscii(byte[] bytes) => new(bytes.Select(value =>
        value is >= 32 and <= 126 ? (char)value : '.').ToArray());
    private static string Sanitize(string value)
    {
        foreach (var invalid in Path.GetInvalidFileNameChars())
            value = value.Replace(invalid, '_');
        return value;
    }

    public void Dispose() => Stop("disposed");
}
