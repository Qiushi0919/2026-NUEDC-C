using System.Drawing;

namespace DigitalKeyLab;

public enum RangeZone
{
    Offline,
    OutsideAngle,
    OutsideRange,
    Sensing,
    Welcome,
    Unlock
}

public sealed record DoorDecision(
    RangeZone Zone,
    bool IdentityMatched,
    bool LightOn,
    bool Unlocked,
    string ZoneText,
    string ActionText,
    Color ZoneColor)
{
    public string EventKey => $"{Zone}:{IdentityMatched}:{LightOn}:{Unlocked}";
}

public static class DoorLogic
{
    public const double UnlockBoundaryM = 1.0;
    public const double WelcomeBoundaryM = 2.0;
    public const double SensingBoundaryM = 3.0;
    public const double HalfAngleDeg = 45.0;

    public static DoorDecision Evaluate(bool online, double distanceM, double angleDeg, int keyIdentityId, int? allowedKeyId)
    {
        if (!online)
            return Decision(RangeZone.Offline, false, false, false, "等待信标", "闭锁 · 等待定位数据", Color.FromArgb(100, 116, 139));

        var identityMatched = allowedKeyId.HasValue &&
            (keyIdentityId & 0x0F) == (allowedKeyId.Value & 0x0F);
        if (Math.Abs(angleDeg) > HalfAngleDeg)
            return Decision(RangeZone.OutsideAngle, identityMatched, false, false, "正面 ±45°之外", "闭锁 · 超出有效角度", Color.FromArgb(100, 116, 139));

        if (distanceM > SensingBoundaryM)
            return Decision(RangeZone.OutsideRange, identityMatched, false, false, "3 m 检测范围外", "闭锁 · 数字钥匙已离开", Color.FromArgb(100, 116, 139));

        var zone = distanceM < UnlockBoundaryM
            ? RangeZone.Unlock
            : distanceM < WelcomeBoundaryM
                ? RangeZone.Welcome
                : RangeZone.Sensing;

        if (!identityMatched)
            return Decision(zone, false, false, false, ZoneName(zone), "身份不匹配 · 保持闭锁", Color.FromArgb(239, 68, 68));

        return zone switch
        {
            RangeZone.Unlock => Decision(zone, true, true, true, "开锁区 0–1 m", "已验证 · 自动开锁", Color.FromArgb(34, 197, 94)),
            RangeZone.Welcome => Decision(zone, true, true, false, "迎宾区 1–2 m", "迎宾声光开启 · 门锁闭锁", Color.FromArgb(245, 158, 11)),
            _ => Decision(zone, true, false, false, "感应区 2–3 m", "身份已验证 · 等待靠近", Color.FromArgb(14, 165, 233))
        };
    }

    private static string ZoneName(RangeZone zone) => zone switch
    {
        RangeZone.Unlock => "开锁区 0–1 m",
        RangeZone.Welcome => "迎宾区 1–2 m",
        RangeZone.Sensing => "感应区 2–3 m",
        _ => "范围外"
    };

    private static DoorDecision Decision(RangeZone zone, bool matched, bool light, bool unlocked, string zoneText, string actionText, Color color) =>
        new(zone, matched, light, unlocked, zoneText, actionText, color);
}
