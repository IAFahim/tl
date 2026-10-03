namespace Play;

public sealed record BakedEntry(string Key, byte[] Package);

public static class BakedCache
{
    public static IReadOnlyList<BakedEntry> Entries { get; private set; } = [];
    public static string? Recipe { get; private set; }
    public static int Revision { get; private set; }
    public static string? ViewKey { get; set; }
    public static string LastChange { get; private set; } = "";

    public static void Store(byte[][] packages, string[] keys, string recipe)
    {
        if (packages.Length != keys.Length)
            throw new ArgumentException("packages and keys must be parallel arrays");
        List<BakedEntry> next;
        string change;
        if (Recipe != recipe || Entries.Count == 0)
        {
            next = [];
            for (var i = 0; i < packages.Length; i++)
                next.Add(new BakedEntry(keys[i], packages[i]));
            change = "loaded " + string.Join(", ", keys);
        }
        else
        {
            next = [.. Entries];
            var ops = new List<string>();
            for (var i = 0; i < packages.Length; i++)
            {
                var at = next.FindIndex(e => e.Key == keys[i]);
                if (at >= 0)
                {
                    next[at] = new BakedEntry(keys[i], packages[i]);
                    ops.Add("replaced " + keys[i]);
                }
                else
                {
                    next.Add(new BakedEntry(keys[i], packages[i]));
                    ops.Add("added " + keys[i]);
                }
            }
            change = string.Join(", ", ops);
        }
        Entries = next;
        Recipe = recipe;
        Revision++;
        LastChange = change;
        if (ViewKey is { } view && next.All(e => e.Key != view)) ViewKey = null;
    }
}
