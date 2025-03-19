namespace TgCli.Commands;

public class MoveToBottomCommand : InputCommand
{
    public MoveToBottomCommand(Model model) : base(model, "G")
    {

    }

    public override Task Execute(string parameter)
    {
        Model.SelectChatAt(Model.SelectedFolder.Chats.Count - 1);
        return Task.CompletedTask;
    }
}

