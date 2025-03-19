namespace TgCli.Commands;

public class LoadChatsCommand : InputCommand
{
    private IClient Client { get; }

    public LoadChatsCommand(Model model, IClient client) : base(model, "R")
    {
        Client = client;
    }

    public override async Task Execute(string parameter)
    {
        await Client.LoadChatsAsync(Model.SelectedFolder.Id);
    }
}

