using TdLib;
using TgCli.ViewModels;

namespace TgCli;

public class Model
{
    public Dictionary<long, User> Users { get; } = new();
    public List<Folder> Folders { get; } = new();

    public bool MuteChannelsByDefault { get; set; }

    public int SelectedFolderIndex { get; set; }

    public Folder AllChatsFolder => Folders[0];
    public Folder SelectedFolder => Folders[SelectedFolderIndex];
    
    public Model()
    {
        Folders.Add(new Folder(-1, "All chats"));
    }

    public void SelectChatAt(int index)
    {
        if (index < 0)
            index = 0;

        if (index > SelectedFolder.Chats.Count - 1)
            index = SelectedFolder.Chats.Count - 1;

        if (index == SelectedFolder.SelectedChatIndex)
            return;

        SelectedFolder.SelectedChatIndex = index;
    }

    public void SelectFolderAt(int index)
    {
        if (index < 0)
            index = Folders.Count - 1;

        if (index > Folders.Count - 1)
            index = 0;

        if (index == SelectedFolderIndex)
            return;

        SelectedFolderIndex = index;
    }

    public Chat SetChatPosition(long chatId, TdApi.ChatPosition position)
    {
        var folder = position.List switch
        {
            TdApi.ChatList.ChatListFolder chatListFolder => Folders.First(f => f.Id == chatListFolder.ChatFolderId),
            TdApi.ChatList.ChatListMain => AllChatsFolder,
            TdApi.ChatList.ChatListArchive => null, // TODO
            _ => throw new NotSupportedException()
        };

        if (folder is null)
            return null;

        if (!folder.ChatsDict.TryGetValue(chatId, out var chat))
        {
            chat = AllChatsFolder.ChatsDict[chatId];
            folder.Chats.Add(chat);
        }

        chat.Positions[folder] = position.Order;
        folder.TriggerSort();

        return chat;
    }
}