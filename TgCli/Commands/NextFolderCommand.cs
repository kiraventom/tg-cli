namespace TgCli.Commands;

public class NextFolderCommand : InputCommand
{
    public NextFolderCommand(Model model) : base(model, "gt")
    {

    }

    public override Task Execute(string parameter)
    {
        Model.SelectFolderAt(Model.SelectedFolderIndex + 1);
        return Task.CompletedTask;
    }
}

