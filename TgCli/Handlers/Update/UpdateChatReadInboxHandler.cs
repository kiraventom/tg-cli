using TdLib;

namespace TgCli.Handlers.Update;

public class UpdateChatReadInboxHandler : UpdateHandler<TdApi.Update.UpdateChatReadInbox>
{
    public UpdateChatReadInboxHandler(MainViewModel viewModel, Model model) : base(viewModel, model)
    {
    }

    protected override Task<bool> HandleAsync(TdApi.Update.UpdateChatReadInbox update)
    {
        if (!Model.AllChatsFolder.ChatsDict.TryGetValue(update.ChatId, out var chat))
            return Task.FromResult(false); // TODO

        chat.UnreadCount = update.UnreadCount;
        return Task.FromResult(ViewModel.IsChatVisible(chat));
    }
}

