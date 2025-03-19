using TdLib;

namespace TgCli.Handlers.Update;

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

