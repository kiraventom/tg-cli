using TgCli.Extensions;
using TgCli.ViewModels;

namespace TgCli.Commands;

public class LoadMessagesCommand : InputCommand
{
    private IClient Client { get; }
    private IRenderer Renderer { get; }

    public LoadMessagesCommand(Model model, IClient client, IRenderer renderer) : base(model, "l")
    {
        Client = client;
        Renderer = renderer;
    }

    public override async Task Execute(string parameter)
    {
        var chatId = Model.SelectedFolder.SelectedChat.Id;
        long lastMessageId = 0;
        if (Model.SelectedFolder.SelectedChat.Messages.Any())
        {
            lastMessageId = Model.SelectedFolder.SelectedChat.Messages.First().Id;
        }
        else if (Model.SelectedFolder.SelectedChat.LastMessage is not null)
        {
            lastMessageId = Model.SelectedFolder.SelectedChat.LastMessage.Id;
            Model.SelectedFolder.SelectedChat.Messages.Insert(0, Model.SelectedFolder.SelectedChat.LastMessage);
        }

        var messagesCount = Renderer.MaxVisibleMessagesCount;

        while (Model.SelectedFolder.SelectedChat.Messages.Count < messagesCount)
        {
            var messages = await Client.LoadMessagesAsync(chatId, lastMessageId, 0, messagesCount);
            if (messages?.Messages_ is null)
                return;

            foreach (var message in messages.Messages_)
            {
                var messageViewModel = new Message(message.Id, message.Content.GetContentString());
                Model.SelectedFolder.SelectedChat.Messages.Insert(0, messageViewModel);
            }
        }
    }
}

