namespace TgCli.Commands;

public class MoveToTopCommand : InputCommand
{
    public MoveToTopCommand(Model model) : base(model, "gg")
    {

    }

    public override Task Execute(string parameter)
    {
        Model.SelectChatAt(0);
        return Task.CompletedTask;
    }
}

