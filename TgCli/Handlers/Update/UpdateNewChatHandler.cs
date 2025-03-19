using TdLib;
using TgCli.Utils;
using TgCli.ViewModels;

namespace TgCli.Handlers.Update;

public class UpdateNewChatHandler : UpdateHandler<TdApi.Update.UpdateNewChat>
{
    public UpdateNewChatHandler(MainViewModel viewModel, Model model) : base(viewModel, model)
    {
    }

    protected override Task<bool> HandleAsync(TdApi.Update.UpdateNewChat update)
    {
        var chat = update.Chat;
        var chatTitle = StringUtils.RemoveNonUtf16Characters(chat.Title);
        var newChat = new Chat(chat.Id, chatTitle)
        {
            UnreadCount = chat.UnreadCount,
        };

        if (!newChat.IsPrivate)
            newChat.IsMuted = Model.MuteChannelsByDefault;

        Model.AllChatsFolder.Chats.Add(newChat);
        return Task.FromResult(ViewModel.IsChatVisible(newChat));
    }
}

