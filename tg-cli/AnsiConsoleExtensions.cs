using Spectre.Console;

namespace tg_cli;

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
}