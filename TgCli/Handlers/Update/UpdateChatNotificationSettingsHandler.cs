using TdLib;

namespace TgCli.Handlers.Update;

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

