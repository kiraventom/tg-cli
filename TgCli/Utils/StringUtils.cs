using System.Collections;
using TdLib;

namespace tg_cli.Utils;

public static class StringUtils
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

    static StringUtils()
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
}

public class CovariantKeyDictionaryWrapper<TIKey, TKey, TValue> : IReadOnlyDictionary<TIKey, TValue> where TKey : TIKey
{
    private readonly Dictionary<TKey, TValue> _dict;

    public int Count => _dict.Count;

    public TValue this[TIKey key] => _dict[(TKey) key];

    public IEnumerable<TIKey> Keys => _dict.Keys.Cast<TIKey>();
    public IEnumerable<TValue> Values => _dict.Values;

    public CovariantKeyDictionaryWrapper(Dictionary<TKey, TValue> dict)
    {
        _dict = dict;
    }

    public bool ContainsKey(TIKey key) => _dict.ContainsKey((TKey) key);
    public bool TryGetValue(TIKey key, out TValue value) => _dict.TryGetValue((TKey) key, out value);

    public IEnumerator<KeyValuePair<TIKey, TValue>> GetEnumerator() =>
        _dict.Select(p => new KeyValuePair<TIKey, TValue>(p.Key, p.Value)).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public class CovariantValueDictionaryWrapper<TKey, TIValue, TValue> : IReadOnlyDictionary<TKey, TIValue>
    where TValue : TIValue
{
    private readonly Dictionary<TKey, TValue> _dict;

    public int Count => _dict.Count;

    public TIValue this[TKey key] => _dict[key];

    public IEnumerable<TKey> Keys => _dict.Keys;
    public IEnumerable<TIValue> Values => _dict.Values.OfType<TIValue>();

    public CovariantValueDictionaryWrapper(Dictionary<TKey, TValue> dict)
    {
        _dict = dict;
    }

    public bool ContainsKey(TKey key) => _dict.ContainsKey(key);

    public bool TryGetValue(TKey key, out TIValue value)
    {
        value = default;
        var success = _dict.TryGetValue(key, out var val);
        if (!success)
            return false;

        value = val;
        return true;
    }

    public IEnumerator<KeyValuePair<TKey, TIValue>> GetEnumerator() =>
        _dict.Select(p => new KeyValuePair<TKey, TIValue>(p.Key, p.Value)).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}