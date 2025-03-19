using TdLib;

namespace TgCli.Handlers.Update;

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

