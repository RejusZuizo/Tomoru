using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Tomoru.Converters;

/// <summary>
/// A 0..1 fraction as a share of a length passed in the parameter — used by the
/// candle face to turn "how much of the block is left" into how tall the wax is.
///
/// <para>Never returns quite zero. A candle that reaches nothing pops out of
/// existence and takes its flame with it a moment before the phase ends;
/// leaving a sliver means it burns down to a stub and goes out with the chime,
/// which is the bit worth watching.</para>
/// </summary>
public class FractionConverter : IValueConverter
{
    private const double Stub = 3;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not double fraction || double.IsNaN(fraction))
            return Stub;

        var full = parameter is string s && double.TryParse(s, NumberStyles.Any,
                       CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 100d;

        return Stub + Math.Clamp(fraction, 0, 1) * (full - Stub);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
