using tg_cli.ViewModels;

namespace tg_cli;

public struct VisibleInterface
{
    public IReadOnlyList<Folder> Folders { get; }
    public IReadOnlyList<Chat> Chats { get; }
    public IReadOnlyDictionary<long, User> Users { get; }
    public int SelectedFolderIndex { get; }
    public int SelectedChatIndex { get; }
    public string CommandInput { get; }

    public VisibleInterface(IReadOnlyList<Chat> chats, int selectedChatIndex, string commandInput,
        IReadOnlyList<Folder> folders, int selectedFolderIndex, IReadOnlyDictionary<long, User> users)
    {
        Chats = chats;
        SelectedChatIndex = selectedChatIndex;
        CommandInput = commandInput;
        Folders = folders;
        SelectedFolderIndex = selectedFolderIndex;
        Users = users;
    }
}