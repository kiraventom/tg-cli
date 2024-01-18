using System.Text;
using Spectre.Console;

namespace tg_cli;

public class InputListener
{
    private readonly IAnsiConsole _console;
    private readonly StringBuilder _inputBuilder = new();

    private readonly CancellationTokenSource _cts = new();

    public event Action<string> InputReceived;
    public event Action<Command> CommandReceived;

    public InputListener(IAnsiConsole console)
    {
        _console = console;
        CommandReceived += OnCommandReceived;
    }

    public async Task StartListen()
    {
        while (true)
        {
            var readKeyResult = await _console.Input.ReadKeyAsync(true, _cts.Token);
            if (readKeyResult is null)
                return;

            var cki = readKeyResult!.Value;

            if (cki.Key == ConsoleKey.Escape)
            {
                _inputBuilder.Clear();
                InputReceived?.Invoke(string.Empty);
                continue;
            }

            var newInput = cki.Modifiers switch
            {
                ConsoleModifiers.Control => $"<C-{cki.Key.ToString().ToLower()}>",
                ConsoleModifiers.Alt => $"<A-{cki.Key.ToString().ToLower()}>",
                _ => cki.KeyChar.ToString()
            };

            _inputBuilder.Append(newInput);
            var input = _inputBuilder.ToString();
            var isCommand = TryParseCommand(input, out var command);
            if (!isCommand)
            {
                InputReceived?.Invoke(input);
                continue;
            }

            _inputBuilder.Clear();
            InputReceived?.Invoke(string.Empty);
            CommandReceived?.Invoke(command);
        }
    }

    private void OnCommandReceived(Command command)
    {
        switch (command)
        {
            case QuitCommand:
                _cts.Cancel();
                break;
        }
    }

    private static bool TryParseCommand(string input, out Command command)
    {
        input = ApplyTemplate(input, out var parameter);

        command = input switch
        {
            "q" => new QuitCommand(),
            "j" => new MoveDownCommand(),
            "k" => new MoveUpCommand(),
            "gg" => new MoveToTopCommand(),
            "G" => new MoveToBottomCommand(),
            "gt" => new NextFolderCommand(),
            "gT" => new PreviousFolderCommand(),
            "g%t" when parameter == "$" => new LastFolderCommand(),
            "g%t" => new SelectFolderCommand(parameter),
            "<C-w>h" => new MoveSeparatorToLeftCommand(),
            "<C-w>l" => new MoveSeparatorToRightCommand(),
            "R" => new LoadChatsCommand(),
            "l" => new LoadMessagesCommand(),
            _ => null
        };

        return command is not null;
    }

    private static string ApplyTemplate(string input, out string parameter)
    {
        parameter = string.Empty;

        var templates = new[] {"g%t"};
        foreach (var template in templates)
        {
            var index = template.IndexOf('%');
            var start = template[..index];
            var end = template[(index + 1)..];

            if (input.StartsWith(start) && input.EndsWith(end))
            {
                parameter = input[start.Length..^end.Length];
                if (parameter.Length < 1)
                    continue;

                input = template;
            }
        }

        return input;
    }
}