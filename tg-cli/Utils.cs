using System.Text;
using TdLib;

namespace tg_cli;

public static class Utils
{
    private static readonly string[] EmptyChars =
    {
        "\u200c", "\u200d", "\u200b", "\u2060", // Zero-width characters
    };

    private static readonly string[] EmojiVariants =
    {
        "\ufe0f", "\ufe0e", // Emoji variant forms
    };

    private static readonly string[] CombiningDiacriticalMarks;

    static Utils()
    {
        var list = new List<string>();
        for (var c = '\u0300'; c <= '\u036f'; ++c)
        {
            list.Add(c.ToString());
        }

        CombiningDiacriticalMarks = list.ToArray();
    }

    // HACK to fix Spectre bug that causes strings with surrogate pairs and zero-widths break aligning
    public static string RemoveNonUtf16Characters(string input)
    {
        input = string.Join(string.Empty, input.Where(c => !char.IsSurrogate(c)));
        input = EmptyChars.Aggregate(input,
            (current, zeroWidth) => current.Replace(zeroWidth, string.Empty));

        input = EmojiVariants.Aggregate(input,
            (current, emoji) => current.Replace(emoji, string.Empty));

        input = CombiningDiacriticalMarks.Aggregate(input,
            (current, diacritics) => current.Replace(diacritics, string.Empty));

        return input;
    }

    public static IReadOnlyList<string> WrapText(string text, int width)
    {
        List<string> wrappedLines = new();
        var lines = text.Split('\n');
        foreach (var line in lines)
        {
            var words = line.Split(' ');
            var i = 0;
            while (i < words.Length)
            {
                StringBuilder lineBuilder = new();
                for (; i < words.Length; ++i)
                {
                    var word = words[i];
                    if (lineBuilder.Length + word.Length + 1 > width)
                    {
                        if (lineBuilder.Length == 0)
                        {
                            var chunks = word.Chunk(width).Select(c => new string(c));
                            wrappedLines.AddRange(chunks);
                        }
                        
                        break;
                    }

                    lineBuilder.Append(word).Append(' ');
                }
                
                wrappedLines.Add(lineBuilder.ToString());
            }
        }
        
        return wrappedLines;
    }
}