namespace TgCli.Commands;

public class MoveDownCommand : InputCommand
{
    public MoveDownCommand(Model model) : base(model, "j")
    {

    }

    public override Task Execute(string parameter)
    {
        Model.SelectChatAt(Model.SelectedFolder.SelectedChatIndex + 1);
        return Task.CompletedTask;
    }
}

