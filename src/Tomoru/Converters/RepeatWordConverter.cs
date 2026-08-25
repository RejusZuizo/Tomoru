using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Tomoru.Models;
using Tomoru.Services;

namespace Tomoru.Converters;

/// <summary>
/// A <see cref="RepeatRule"/> as the word shown in the picker. Lowercasing the
/// enum would leave "none", which reads as a value rather than as "this is a
/// one-off" — the option most tickets want.
/// </summary>
public class RepeatWordConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is RepeatRule rule && rule != RepeatRule.None
            ? "repeats " + Recurrence.Label(rule)
            : "doesn't repeat";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
