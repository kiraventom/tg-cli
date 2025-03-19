namespace TgCli.Commands;

public abstract class InputCommand
{
    protected string CommandText { get; }
    protected Model Model { get; }

    protected InputCommand(Model model, string commandText)
    {
        Model = model;
        CommandText = commandText;
    }

    public abstract Task Execute(string parameter);
    public virtual bool StartsWith(string input) => CommandText.StartsWith(input);
    public virtual bool Match(string input, out string parameter)
    {
        parameter = null;
        return CommandText == input;
    }
}

