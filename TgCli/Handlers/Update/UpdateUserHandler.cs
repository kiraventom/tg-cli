using TdLib;
using TgCli.ViewModels;

namespace TgCli.Handlers.Update;

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
