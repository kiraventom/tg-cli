namespace TgCli.Commands;

public class PreviousFolderCommand : InputCommand
{
    public PreviousFolderCommand(Model model) : base(model, "gT")
    {

    }

    public override Task Execute(string parameter)
    {
        Model.SelectFolderAt(Model.SelectedFolderIndex - 1);
        return Task.CompletedTask;
    }
}

