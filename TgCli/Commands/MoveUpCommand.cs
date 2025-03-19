namespace TgCli.Commands;

public class MoveUpCommand : InputCommand
{
    public MoveUpCommand(Model model) : base(model, "k")
    {

    }

    public override Task Execute(string parameter)
    {
        Model.SelectChatAt(Model.SelectedFolder.SelectedChatIndex - 1);
        return Task.CompletedTask;
    }
}

