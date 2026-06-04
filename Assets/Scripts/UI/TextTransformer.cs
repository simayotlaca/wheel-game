using System.Globalization;
using System.Text;
using TMPro;

namespace VertigoWheel
{
internal static class TextTransformer
{
    private static StringBuilder builder = new StringBuilder();

    internal static void SetNumber(TMP_Text label, int value)
    {
        builder.Clear();
        builder.Append(value);
        label.SetText(builder);
    }

    internal static void SetCompactNumber(TMP_Text label, int value)
    {
        label.text = NumberFormatter.FormatCompact(value);
    }

    internal static void SetThousandsNumber(TMP_Text label, int value)
    {
        label.text = NumberFormatter.FormatThousands(value);
    }

    internal static void SetProgressCount(TMP_Text label, int current, int limit)
    {
        builder.Clear();
        builder.Append(current);
        builder.Append(" / ");
        builder.Append(limit);
        label.SetText(builder);
    }

    internal static void Clear(TMP_Text label)
    {
        label.text = string.Empty;
    }
}

internal static class NumberFormatter
{
    private const int Thousand = 1_000;
    private const int HundredThousand = 100_000;
    private const int Million = 1_000_000;
    private const int HundredMillion = 100_000_000;
    private const int DecimalWholeStep = 10;

    internal static string FormatThousands(int value)
    {
        return value.ToString("N0", CultureInfo.InvariantCulture);
    }

    internal static string FormatCompact(int value)
    {
        if (value < Thousand)
        {
            return value.ToString();
        }
        if (value < HundredThousand)
        {
            return FormatDecimalUnit(value, Thousand, "K");
        }
        if (value < Million)
        {
            return (value / Thousand) + "K";
        }
        if (value < HundredMillion)
        {
            return FormatDecimalUnit(value, Million, "M");
        }
        return (value / Million) + "M";
    }

    private static string FormatDecimalUnit(int value, int unit, string suffix)
    {
        int tenths = value / (unit / DecimalWholeStep);
        int whole = tenths / DecimalWholeStep;
        int frac = tenths % DecimalWholeStep;
        if (frac == 0)
        {
            return whole + suffix;
        }
        return whole + "." + frac + suffix;
    }
}
}
