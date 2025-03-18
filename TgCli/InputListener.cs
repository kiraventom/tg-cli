using System.Text;
using Serilog;
using Spectre.Console;

namespace TgCli;

public abstract class InputCommand(string CommandText)
{
    public abstract Task Execute(string parameter);
    public virtual bool StartsWith(string input) => CommandText.StartsWith(input);
    public virtual bool Match(string input, out string parameter)
    {
        parameter = null;
        return CommandText == input;
    }
}

public abstract class ParametrizedInputCommand : InputCommand
{
    private string CommandHead { get; }
    private string CommandTail { get; }
    private HashSet<char> AllowedParameters { get; }
    private char ParameterPlaceholder { get; }

    protected ParametrizedInputCommand(string commandTemplate, string allowedParameters, char parameterPlaceholder = '_') : base(commandTemplate)
    {
        var templateSplit = commandTemplate.Split(parameterPlaceholder, StringSplitOptions.TrimEntries);
        if (templateSplit.Length != 2)
            throw new NotSupportedException($"Template '{commandTemplate}' contains less or more than one '{parameterPlaceholder}'");

        if (allowedParameters.Length == 0)
            throw new NotSupportedException("Command has no allowed parameters");

        CommandHead = templateSplit[0];
        CommandTail = templateSplit[1];

        AllowedParameters = allowedParameters.ToCharArray().ToHashSet();
        ParameterPlaceholder = parameterPlaceholder;
    }

    public sealed override bool StartsWith(string input)
    {
        if (input.Length <= CommandHead.Length)
            return CommandHead.StartsWith(input);

        var inputHead = input[..CommandHead.Length];
        if (input.Length - CommandHead.Length == 1)
            return CommandHead == inputHead && AllowedParameters.Contains(input[^1]);

        var inputParameter = input[CommandHead.Length + 1];
        if (input.Length <= CommandHead.Length + 1 + CommandTail.Length)
            return
                CommandHead == inputHead
                && AllowedParameters.Contains(inputParameter)
                && CommandTail.StartsWith(input[(CommandHead.Length + 1 + 1)..]);

        return false;
    }

    public sealed override bool Match(string input, out string parameter)
    {
        parameter = null;
        if (input.Length != CommandHead.Length + 1 + CommandTail.Length)
            return false;

        var inputHead = input[..CommandHead.Length];
        var inputParameter = input[CommandHead.Length + 1];
        var inputTail = input[(CommandHead.Length + 1 + 1)..];

        var didMatch =
            inputHead == CommandHead
            && AllowedParameters.Contains(inputParameter)
            && inputTail == CommandTail;

        if (didMatch)
            parameter = inputParameter.ToString();

        return didMatch;
    }
}

public class InputListener
{
    private readonly IAnsiConsole _console;
    private readonly StringBuilder _inputBuilder = new();
    private readonly ILogger _logger;

    private IReadOnlyCollection<InputCommand> InputCommands { get; } = new InputCommand[]
    {
        new QuitCommand(),
        new MoveDownCommand(),
        new MoveUpCommand(),
        new MoveToTopCommand(),
        new MoveToBottomCommand(),
        new NextFolderCommand(),
        new PreviousFolderCommand(),
        new SelectFolderCommand(),
        new LoadChatsCommand(),
        new LoadMessagesCommand(),
    };

    public event Action<string> InputUpdated;

    public InputListener(IAnsiConsole console, ILogger logger)
    {
        _console = console;
        _logger = logger;
    }

    public async Task StartListen()
    {
        while (true)
        {
            var readKeyResult = await _console.Input.ReadKeyAsync(intercept: true, CancellationToken.None);

            var cki = readKeyResult!.Value;

            if (cki.Key == ConsoleKey.Escape)
            {
                ResetInput();
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

            var potentialCommands = InputCommands.Where(c => c.StartsWith(input));

            var potentialCommandsCount = 0;
            foreach (var potentialCommand in potentialCommands)
            {
                ++potentialCommandsCount;
                if (potentialCommand.Match(input, out var parameter))
                {
                    ResetInput();
                    await potentialCommand.Execute(parameter);
                    break;
                }
            }

            if (potentialCommandsCount == 0)
            {
                ResetInput();
            }
            else
            {
                InputUpdated?.Invoke(input);
            }

        }
    }

    private void ResetInput()
    {
        _inputBuilder.Clear();
        InputUpdated?.Invoke(string.Empty);
    }
}