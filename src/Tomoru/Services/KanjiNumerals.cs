using System.Text;

namespace Tomoru.Services;

/// <summary>
/// Numbers as Japanese numerals, for the kanji clock face.
///
/// <para>Only needs 0–59, which is the easy half of the system: units are one
/// character each, ten is 十 on its own, the teens are 十 followed by the unit,
/// and the tens are the unit followed by 十. The irregularity people trip on —
/// 一十 is wrong for ten, and 二十一 rather than 二十and一 — is exactly what the
/// tests below pin.</para>
/// </summary>
public static class KanjiNumerals
{
    private static readonly string[] Units =
        { "零", "一", "二", "三", "四", "五", "六", "七", "八", "九" };

    private const string Ten = "十";

    /// <summary>A number 0–59 in kanji. Out-of-range values come back as the
    /// plain digits rather than throwing — a clock face is not worth a crash.</summary>
    public static string From(int n)
    {
        if (n is < 0 or > 59)
            return n.ToString();

        if (n < 10)
            return Units[n];

        var tens = n / 10;
        var ones = n % 10;

        var sb = new StringBuilder();

        // Ten is 十, not 一十 — the leading one is dropped.
        if (tens > 1)
            sb.Append(Units[tens]);
        sb.Append(Ten);

        // Twenty is 二十, not 二十零 — a zero unit is simply absent.
        if (ones > 0)
            sb.Append(Units[ones]);

        return sb.ToString();
    }

    /// <summary>"十八分四十秒" — the minutes and seconds of a countdown, each
    /// with its counter.
    ///
    /// <para>Zero is dropped on whichever side can spare it: under a minute
    /// this reads as the seconds alone rather than 零分, and exactly on a
    /// minute as the minutes alone rather than 零秒. Both are how the time
    /// would actually be said, and 二十五分零秒 sitting on screen at the start
    /// of every block is the version that looks machine-made.</para></summary>
    public static string Countdown(int minutes, int seconds)
    {
        if (minutes <= 0)
            return $"{From(seconds)}秒";

        return seconds == 0
            ? $"{From(minutes)}分"
            : $"{From(minutes)}分{From(seconds)}秒";
    }
}
