using TdLib;

namespace TgCli.Handlers.Update;

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

