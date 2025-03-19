using TdLib;
using TgCli.Extensions;
using TgCli.ViewModels;

namespace TgCli.Handlers.Update;

public class UpdateChatLastMessageHandler : UpdateHandler<TdApi.Update.UpdateChatLastMessage>
{
    public UpdateChatLastMessageHandler(MainViewModel viewModel, Model model) : base(viewModel, model)
    {
    }

    protected override Task<bool> HandleAsync(TdApi.Update.UpdateChatLastMessage update)
    {
        if (update.LastMessage is null)
            return Task.FromResult(false);

        var content = update.LastMessage.Content?.GetContentString();
        if (!Model.AllChatsFolder.ChatsDict.TryGetValue(update.ChatId, out var chat))
            return Task.FromResult(false); // TODO

        chat.LastMessage = new Message(update.LastMessage.Id, content);

        foreach (var position in update.Positions) 
            Model.SetChatPosition(update.ChatId, position);

        return Task.FromResult(ViewModel.IsChatVisible(chat));
    }
}

