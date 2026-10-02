namespace Play;

public static class BakedCache
{
    public static byte[][]? Packages { get; private set; }
    public static string? Recipe { get; private set; }
    public static int Revision { get; private set; }

    public static void Store(byte[][] packages, string recipe)
    {
        Packages = packages;
        Recipe = recipe;
        Revision++;
    }
}
