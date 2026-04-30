namespace backend.ModernServices.Formatting;

public static class UnitConversion
{
    public const int TwipsPerPoint = 20;
    public const double TwipsPerCm = 567.0;

    public static int PointsToTwips(int points) => points * TwipsPerPoint;

    public static double TwipsToPoints(int twips) => twips / (double)TwipsPerPoint;

    public static double TwipsToCentimeters(int twips) => twips / 1440.0 * 2.54;

    public static double? HalfPointsToPoints(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        return double.TryParse(value, out var halfPoints)
            ? halfPoints / 2.0
            : null;
    }

    public static int ParseTwips(string? value)
    {
        return !string.IsNullOrEmpty(value) && int.TryParse(value, out var twips)
            ? twips
            : 0;
    }

    public static int? ParseOptionalTwips(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        return int.TryParse(value, out var twips)
            ? twips
            : null;
    }
}
