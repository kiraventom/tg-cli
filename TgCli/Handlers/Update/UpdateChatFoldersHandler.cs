using TdLib;
using TgCli.ViewModels;

namespace TgCli.Handlers.Update;

public class UpdateChatFoldersHandler : UpdateHandler<TdApi.Update.UpdateChatFolders>
{
    public UpdateChatFoldersHandler(MainViewModel viewModel, Model model) : base(viewModel, model)
    {
    }

    protected override Task<bool> HandleAsync(TdApi.Update.UpdateChatFolders update)
    {
        foreach (var chatFolderInfo in update.ChatFolders)
        {
            var folder = new Folder(chatFolderInfo.Id, chatFolderInfo.Name.Text.Text);
            Model.Folders.Add(folder);
        }

        return Task.FromResult(true);
    }
}

