using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Tomoru.Converters;

/// <summary>
/// A 0..1 fraction as degrees around a circle, for the ring clock face's arc.
///
/// <para>Stops just short of a full turn. At exactly 360 the arc's two ends
/// meet and the round cap draws over the start, so a finished block would
/// render identically to one that hasn't begun — the one moment the face most
/// needs to be unambiguous.</para>
/// </summary>
public class SweepConverter : IValueConverter
{
    private const double FullTurn = 359.99;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is double fraction && !double.IsNaN(fraction)
            ? Math.Clamp(fraction, 0, 1) * FullTurn
            : 0d;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
