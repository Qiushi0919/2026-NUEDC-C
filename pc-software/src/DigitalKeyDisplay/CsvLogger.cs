using System.Text;

namespace DigitalKeyLab;

public sealed class CsvLogger : IDisposable
{
    private StreamWriter? _writer;
    public string FilePath { get; private set; } = string.Empty;

    public void Start()
    {
        Stop();
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "C题数字钥匙实验记录");
        Directory.CreateDirectory(folder);
        FilePath = Path.Combine(folder, $"定位记录_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        _writer = new StreamWriter(FilePath, false, new UTF8Encoding(true));
        _writer.WriteLine("时间,模式,基站ID,信标ID,钥匙身份ID,DIP允许ID,距离m,方位角deg,X横向m,Y前向m,身份验证,区域,动作,校验");
        _writer.Flush();
    }

    public void Write(AnchorFrame frame, string mode, double distance, double angle,
        int keyIdentityId, int? allowedKeyId, DoorDecision decision)
    {
        if (_writer is null)
            return;
        var x = distance * Math.Sin(angle * Math.PI / 180.0);
        var y = distance * Math.Cos(angle * Math.PI / 180.0);
        _writer.WriteLine(string.Join(',',
            frame.ReceivedAt.ToString("yyyy-MM-dd HH:mm:ss.fff"),
            mode,
            frame.AnchorIdHex,
            frame.TagIdHex,
            Convert.ToString(keyIdentityId & 0x0F, 2).PadLeft(4, '0'),
            allowedKeyId.HasValue ? Convert.ToString(allowedKeyId.Value & 0x0F, 2).PadLeft(4, '0') : string.Empty,
            distance.ToString("F3"),
            angle.ToString("F2"),
            x.ToString("F3"),
            y.ToString("F3"),
            decision.IdentityMatched ? "通过" : "不通过",
            decision.ZoneText,
            decision.ActionText,
            frame.ChecksumValid ? "正常" : "异常"));
        if (frame.Sequence % 20 == 0)
            _writer.Flush();
    }

    public void Stop()
    {
        _writer?.Flush();
        _writer?.Dispose();
        _writer = null;
    }

    public void Dispose() => Stop();
}
