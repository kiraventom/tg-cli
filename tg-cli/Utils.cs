namespace tg_cli;

public static class Utils
{
    private static readonly string[] EmptyChars = 
    {
        "\u200c", "\u200d", "\u200b", "\u2060", // Zero-width characters
        "\ufe0f", "\ufe0e" // Emoji variant forms
        };

    // HACK to fix Spectre bug that causes strings with surrogate pairs and zero-widths break aligning
    public static string RemoveNonUtf16Characters(string input)
    {
        input = string.Join(string.Empty, input.Where(c => !char.IsSurrogate(c)));
        input = EmptyChars.Aggregate(input,
            (current, zeroWidth) => current.Replace(zeroWidth, string.Empty));

        return input;
    }
}