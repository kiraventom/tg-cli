namespace TgCli.Commands;

public class QuitCommand : InputCommand
{
    public QuitCommand(Model model) : base(model, "q")
    {
    }

    public override Task Execute(string parameter)
    {
        // TODO
        return Task.CompletedTask;
    }
}
