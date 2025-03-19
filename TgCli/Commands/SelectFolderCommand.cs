namespace TgCli.Commands;

public class SelectFolderCommand : ParametrizedInputCommand
{
    public SelectFolderCommand(Model model) : base(model, "g_t", "0123456789")
    {

    }

    public override Task Execute(string parameter)
    {
        if (parameter == "$")
        {
            Model.SelectFolderAt(Model.Folders.Count - 1);
            return Task.CompletedTask;
        }

        var index = int.Parse(parameter);
        if (index > Model.Folders.Count - 1)
            index = Model.Folders.Count - 1;

        Model.SelectFolderAt(index);
        return Task.CompletedTask;
    }
}

