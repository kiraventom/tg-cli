using tg_cli.ViewModels;

namespace tg_cli;

public struct VisibleInterface
{
    public IReadOnlyList<Folder> Folders { get; }
    public IReadOnlyList<Chat> Chats { get; }
    public IReadOnlyList<Message> Messages { get; }
    public IReadOnlyDictionary<long, User> Users { get; }
    public int SelectedFolderIndex { get; }
    public int SelectedChatIndex { get; }
    public string CommandInput { get; }

    public VisibleInterface(IReadOnlyList<Folder> folders, IReadOnlyList<Chat> chats, IReadOnlyList<Message> messages,
        int selectedChatIndex, string commandInput, int selectedFolderIndex, IReadOnlyDictionary<long, User> users)
    {
        Folders = folders;
        Chats = chats;
        Messages = messages;
        SelectedChatIndex = selectedChatIndex;
        CommandInput = commandInput;
        SelectedFolderIndex = selectedFolderIndex;
        Users = users;
    }
}