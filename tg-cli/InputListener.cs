using System.Text;
using Spectre.Console;
using TdLib;

namespace tg_cli;

public enum Command
{
    None,
    Quit,
    MoveUp,
    MoveDown,
    MoveToTop,
    MoveToBottom
}

public class InputListener
{
    private readonly IAnsiConsole _console;
    private readonly StringBuilder _inputBuilder = new();

    public event Action<Command> CommandReceived;

    public InputListener(IAnsiConsole console)
    {
        _console = console;
    }

    public async Task StartListen()
    {
        while (true)
        {
            var cki = await _console.Input.ReadKeyAsync(true, CancellationToken.None);
            if (cki!.Value.Key == ConsoleKey.Escape)
            {
                _inputBuilder.Clear();
                continue;
            }

            _inputBuilder.Append(cki!.Value.KeyChar);
            var isCommand = TryParseCommand(_inputBuilder.ToString(), out var command);
            if (!isCommand)
                continue;

            _inputBuilder.Clear();
            
            if (command == Command.Quit)
                return;
                
            CommandReceived?.Invoke(command);
        }
    }

    private static bool TryParseCommand(string input, out Command command)
    {
        command = input switch
        {
            "q" => Command.Quit,
            "j" => Command.MoveDown,
            "k" => Command.MoveUp,
            "gg" => Command.MoveToTop,
            "G" => Command.MoveToBottom,
            _ => Command.None
        };

        return command != Command.None;
    }
}