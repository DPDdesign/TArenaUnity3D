public static class UnitFactionResolver
{
    public const int UnknownFactionId = 0;
    public const int BarbarianFactionId = 1;
    public const int LizardFactionId = 2;
    public const int GolemElementalFactionId = 3;

    public static int ResolveFactionId(string unitId)
    {
        switch (Normalize(unitId))
        {
            case "rusher":
            case "thrower":
            case "axeman":
            case "heavyhitter":
                return BarbarianFactionId;

            case "specialist":
            case "healer":
            case "trapper":
            case "tank":
                return LizardFactionId;

            case "stonegolem":
            case "fireelemental":
            case "fleshgolem":
            case "wisp":
                return GolemElementalFactionId;

            default:
                return UnknownFactionId;
        }
    }

    private static string Normalize(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        char[] buffer = new char[value.Length];
        int length = 0;
        for (int i = 0; i < value.Length; i++)
        {
            char c = char.ToLowerInvariant(value[i]);
            if (char.IsLetterOrDigit(c))
            {
                buffer[length++] = c;
            }
        }

        return new string(buffer, 0, length);
    }
}
