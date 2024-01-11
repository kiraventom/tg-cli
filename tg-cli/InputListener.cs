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

    public event Action<string> InputReceived;
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
                InputReceived?.Invoke(string.Empty);
                continue;
            }

            _inputBuilder.Append(cki!.Value.KeyChar);
            var input = _inputBuilder.ToString();
            var isCommand = TryParseCommand(input, out var command);
            if (!isCommand)
            {
                InputReceived?.Invoke(input);
                continue;
            }

            _inputBuilder.Clear();
            InputReceived?.Invoke(string.Empty);
            
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