using Spectre.Console;
using Spectre.Console.Rendering;

namespace TgCli.Extensions;

public static class AnsiConsoleExtensions
{
    public static T Ask<T>(this IAnsiConsole console, string prompt, Predicate<T> condition)
    {
        while (true)
        {
            var value = console.Ask<T>(prompt);
            if (condition.Invoke(value))
                return value;
        }
    }

    public static Table SetRow(this Table table, int i, params IRenderable[] columns)
    {
        if (table.Rows.Count <= i)
        {
            table.AddRow(columns);
            return table;
        }

        for (var j = 0; j < columns.Length; ++j)
            table.UpdateCell(i, j, columns[j]);

        return table;
    }

    public static void WriteAt(this IAnsiConsole console, IRenderable renderable, int left, int top) 
    {
        console.Cursor.SetPosition(left, top);
        console.Write(renderable);
    }
}