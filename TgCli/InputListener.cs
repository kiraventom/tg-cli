using System.Text;
using Serilog;
using Spectre.Console;
using TgCli.Commands;

namespace TgCli;

public class InputListener
{
    private readonly IAnsiConsole _console;
    private readonly StringBuilder _inputBuilder = new();
    private readonly ILogger _logger;

    private IReadOnlyCollection<InputCommand> InputCommands { get; }
    public event Action<string> InputUpdated;

    public InputListener(IAnsiConsole console, ILogger logger, IReadOnlyCollection<InputCommand> commands)
    {
        _console = console;
        _logger = logger;
        InputCommands = commands;
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