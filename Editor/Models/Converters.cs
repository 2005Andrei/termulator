using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;

namespace Editor.Converters;

public class StringToListConverter : IValueConverter
{
    public static readonly StringToListConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is List<string> list)
            return string.Join(", ", list);
        return string.Empty;
    }

    public object? ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    )
    {
        if (value is string s)
        {
            return s.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .ToList();
        }
        return new List<string>();
    }
}

public class StringToDictionaryConverter : IValueConverter
{
    public static readonly StringToDictionaryConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Dictionary<string, int> dict)
        {
            return string.Join(", ", dict.Select(kvp => $"{kvp.Key}:{kvp.Value}"));
        }
        return string.Empty;
    }

    public object? ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    )
    {
        var dict = new Dictionary<string, int>();
        if (value is string s)
        {
            var pairs = s.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var pair in pairs)
            {
                var parts = pair.Split(':', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2 && int.TryParse(parts[1].Trim(), out int val))
                {
                    dict[parts[0].Trim()] = val;
                }
            }
        }
        return dict;
    }
}
