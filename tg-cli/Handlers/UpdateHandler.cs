using TdLib;
using tg_cli.Extensions;
using tg_cli.ViewModels;

namespace tg_cli.Handlers;

public abstract class UpdateHandler<T> : IHandler<TdApi.Update>
{
    protected readonly MainViewModel ViewModel;
    protected readonly Model Model;

    protected UpdateHandler(MainViewModel viewModel, Model model)
    {
        ViewModel = viewModel;
        Model = model;
    }

    protected abstract Task<bool> HandleAsync(T update);

    public async Task<bool> HandleAsync(TdApi.Update obj)
    {
        if (obj is not T t)
            throw new NotSupportedException();

        return await HandleAsync(t);
    }

    public bool CanHandle(TdApi.Update update) => update is T;
}

public class UpdateNewChatHandler : UpdateHandler<TdApi.Update.UpdateNewChat>
{
    public UpdateNewChatHandler(MainViewModel viewModel, Model model) : base(viewModel, model)
    {
    }

    protected override Task<bool> HandleAsync(TdApi.Update.UpdateNewChat update)
    {
        var chat = update.Chat;
        var chatTitle = Utils.RemoveNonUtf16Characters(chat.Title);
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

public class UpdateChatPositionHandler : UpdateHandler<TdApi.Update.UpdateChatPosition>
{
    public UpdateChatPositionHandler(MainViewModel viewModel, Model model) : base(viewModel, model)
    {
    }

    protected override Task<bool> HandleAsync(TdApi.Update.UpdateChatPosition update)
    {
        var chat = Model.SetChatPosition(update.ChatId, update.Position);
        return Task.FromResult(chat is not null && ViewModel.IsChatVisible(chat));
    }
}

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

public class UpdateScopeNotificationSettingsHandler : UpdateHandler<TdApi.Update.UpdateScopeNotificationSettings>
{
    public UpdateScopeNotificationSettingsHandler(MainViewModel viewModel, Model model) : base(viewModel, model)
    {
    }

    protected override Task<bool> HandleAsync(TdApi.Update.UpdateScopeNotificationSettings update)
    {
        Model.MuteChannelsByDefault = update.NotificationSettings.MuteFor != 0;
        return Task.FromResult(false);
    }
}

public class UpdateChatNotificationSettingsHandler : UpdateHandler<TdApi.Update.UpdateChatNotificationSettings>
{
    public UpdateChatNotificationSettingsHandler(MainViewModel viewModel, Model model) : base(viewModel, model)
    {
    }

    protected override Task<bool> HandleAsync(TdApi.Update.UpdateChatNotificationSettings update)
    {
        if (!Model.AllChatsFolder.ChatsDict.TryGetValue(update.ChatId, out var chat))
            return Task.FromResult(false); // TODO

        if (!update.NotificationSettings.UseDefaultMuteFor)
            chat.IsMuted = update.NotificationSettings.MuteFor != 0;

        return Task.FromResult(ViewModel.IsChatVisible(chat));
    }
}

public class UpdateChatFoldersHandler : UpdateHandler<TdApi.Update.UpdateChatFolders>
{
    public UpdateChatFoldersHandler(MainViewModel viewModel, Model model) : base(viewModel, model)
    {
    }

    protected override Task<bool> HandleAsync(TdApi.Update.UpdateChatFolders update)
    {
        foreach (var chatFolderInfo in update.ChatFolders)
        {
            var folder = new Folder(chatFolderInfo.Id, chatFolderInfo.Title);
            Model.Folders.Add(folder);
        }

        return Task.FromResult(true);
    }
}

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

public class UpdateUserStatusHandler : UpdateHandler<TdApi.Update.UpdateUserStatus>
{
    public UpdateUserStatusHandler(MainViewModel viewModel, Model model) : base(viewModel, model)
    {
    }

    protected override Task<bool> HandleAsync(TdApi.Update.UpdateUserStatus update)
    {
        if (!Model.Users.TryGetValue(update.UserId, out var user))
            return Task.FromResult(false); // TODO

        var isOnline = update.Status switch
        {
            TdApi.UserStatus.UserStatusOnline => true,
            _ => false
        };

        user.IsOnline = isOnline;

        return Task.FromResult(
            Model.SelectedFolder.ChatsDict.TryGetValue(user.Id, out var chat) && ViewModel.IsChatVisible(chat));
    }
}

public class UpdateUserHandler : UpdateHandler<TdApi.Update.UpdateUser>
{
    public UpdateUserHandler(MainViewModel viewModel, Model model) : base(viewModel, model)
    {
    }

    protected override Task<bool> HandleAsync(TdApi.Update.UpdateUser update)
    {
        if (Model.Users.ContainsKey(update.User.Id))
            return Task.FromResult(false);

        Model.Users.Add(update.User.Id,
            new User(update.User.Id, update.User.FirstName, update.User.LastName,
                update.User.Usernames?.ActiveUsernames?.FirstOrDefault()));
        return Task.FromResult(false);
    }
}