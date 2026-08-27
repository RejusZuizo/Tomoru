using System.Collections.Generic;
using System.Linq;

namespace Tomoru.Services;

/// <summary>A zen-mode clock face the shop can sell and zen mode can wear.</summary>
public record ClockFace(string Id, string Jp, string En, int Price, string Blurb);

/// <summary>
/// The catalogue of zen-mode clock faces.
///
/// <para>Zen mode is the one screen you look at for twenty-five minutes at a
/// stretch, which makes it the surface worth earning something for — and the
/// only cosmetic in the app you'd actually notice while using it. Faces are
/// drawn from ordinary shapes and text rather than assets: nothing to license,
/// nothing to ship, and no third-party control to break on the next Avalonia
/// major the way the icon pack did.</para>
///
/// <para>Every face reads the same four things — phase colour, time left,
/// progress through the block, and round position — so switching changes how
/// the timer looks and never what it tells you.</para>
/// </summary>
public static class ClockFaces
{
    public const string DefaultId = "digital";

    public static readonly IReadOnlyList<ClockFace> All = new[]
    {
        new ClockFace("digital", "数字", "digital", 0,
            "the plain readout — big numerals and a bar"),

        new ClockFace("kanji", "漢数字", "kanji", 150,
            "the time written out: 十八分四十秒"),

        new ClockFace("ring", "輪", "ring", 200,
            "the block drawn as a single turn"),

        new ClockFace("candle", "灯", "candle", 400,
            "a candle that burns down as the block does"),

        // Segments (one cell per minute) is designed and not built. It stays
        // out of the catalogue rather than sitting in it greyed out — a shop
        // that sells something it can't deliver is worse than a short one.
    };

    public static ClockFace Find(string? id) =>
        All.FirstOrDefault(f => f.Id == id) ?? All[0];

    /// <summary>The one every install starts with, and the only free one.</summary>
    public static bool IsFree(string id) => Find(id).Price == 0;
}
