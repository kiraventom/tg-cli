using TdLib;
using TgCli.Extensions;

namespace TgCli.Handlers.Update;

public class UpdateChatActionHandler : UpdateHandler<TdApi.Update.UpdateChatAction>
{
    public UpdateChatActionHandler(MainViewModel viewModel, Model model) : base(viewModel, model)
    {
    }

    protected override Task<bool> HandleAsync(TdApi.Update.UpdateChatAction update)
    {
        if (!Model.AllChatsFolder.ChatsDict.TryGetValue(update.ChatId, out var chat))
            return Task.FromResult(false); // TODO

        // TODO: doesn't work for private chats
        var chatAction = update.Action.GetChatActionString();
        if (chatAction is null)
            return Task.FromResult(false);

        var senderId = update.SenderId switch
        {
            TdApi.MessageSender.MessageSenderChat senderChat => senderChat.ChatId,
            TdApi.MessageSender.MessageSenderUser senderUser => senderUser.UserId,
        };

        if (!chat.IsPrivate)
        {
            if (!Model.Users.TryGetValue(senderId, out var user))
                return Task.FromResult(false); // TODO

            chatAction = user.FirstName + " is " + chatAction.ToLower();
        }

        chat.ChatAction = chatAction;

        return Task.FromResult(ViewModel.IsChatVisible(chat));
    }
}

