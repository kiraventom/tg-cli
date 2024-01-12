using System.Text;
using Spectre.Console;

namespace tg_cli;

public enum CommandType
{
    Quit,
    MoveUp,
    MoveDown,
    MoveToTop,
    MoveToBottom,
    NextFolder,
    PreviousFolder,
    SelectFolder,
    LastFolder,
    MoveSeparatorToLeft,
    MoveSeparatorToRight,
}

public class Command
{
    public CommandType Type { get; }
    public string Parameter { get; }

    public Command(CommandType type)
    {
        Type = type;
    }

    public Command(CommandType type, string parameter) : this(type)
    {
        Parameter = parameter;
    }
}

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
        switch (command.Type)
        {
            case CommandType.Quit:
                _cts.Cancel();
                break;
        }
    }

    private static bool TryParseCommand(string input, out Command command)
    {
        input = ApplyTemplate(input, out var parameter);
        
        command = input switch
        {
            "q" => new Command(CommandType.Quit),
            "j" => new Command(CommandType.MoveDown),
            "k" => new Command(CommandType.MoveUp),
            "gg" => new Command(CommandType.MoveToTop),
            "G" => new Command(CommandType.MoveToBottom),
            "gt" => new Command(CommandType.NextFolder),
            "gT" => new Command(CommandType.PreviousFolder),
            "g%t" when parameter == "$" => new Command(CommandType.LastFolder),
            "g%t" => new Command(CommandType.SelectFolder, parameter),
            "<C-w>h" => new Command(CommandType.MoveSeparatorToLeft),
            "<C-w>l" => new Command(CommandType.MoveSeparatorToRight),
            _ => null
        };
        
        return command is not null;
    }
    
    private static string ApplyTemplate(string input, out string parameter)
    {
        parameter = string.Empty;
        
        var templates = new[] { "g%t" };
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