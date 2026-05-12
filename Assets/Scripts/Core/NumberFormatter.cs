
public static class NumberFormatter
{
    public static string FormatCompact(int value)
    {
        if (value < 0) value = 0;
        if (value < 1000) return value.ToString();

        if (value < 100_000)
        {
            int hundreds = value / 100;
            return (hundreds / 10) + "." + (hundreds % 10) + "K";
        }
        if (value < 1_000_000) return (value / 1000) + "K";

        if (value < 100_000_000)
        {
            int hundredK = value / 100_000;
            return (hundredK / 10) + "." + (hundredK % 10) + "M";
        }
        return (value / 1_000_000) + "M";
    }
}
