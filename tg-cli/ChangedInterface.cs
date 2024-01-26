namespace tg_cli;

public class ChangedInterface
{
    public IReadOnlyList<IRenderFolder> Folders { get; private set; }
    public IReadOnlyList<IRenderChat> Chats { get; private set; }
    public IReadOnlyList<IRenderMessage> Messages { get; private set; }
    public IReadOnlyDictionary<long, IRenderUser> Users { get; private set; }
    public int? SelectedFolderIndex { get; private set; }
    public IRenderChat SelectedChat { get; private set; }
    public int? SelectedChatIndex { get; private set; }
    public string CommandInput { get; private set; }

    public bool IsChanged =>
        Folders is not null ||
        Chats is not null ||
        Messages is not null ||
        Users is not null ||
        SelectedFolderIndex is not null ||
        SelectedChat is not null ||
        CommandInput is not null;

    public ChangedInterface()
    {
    }

    public ChangedInterface(IReadOnlyList<IRenderFolder> folders, IReadOnlyList<IRenderChat> chats,
        IReadOnlyList<IRenderMessage> messages, IReadOnlyDictionary<long, IRenderUser> users, int? selectedFolderIndex,
        IRenderChat selectedChat, int? selectedChatIndex, string commandInput)
    {
        Folders = folders;
        Chats = chats;
        Messages = messages;
        Users = users;
        SelectedFolderIndex = selectedFolderIndex;
        SelectedChat = selectedChat;
        SelectedChatIndex = selectedChatIndex;
        CommandInput = commandInput;
    }

    public void UpdateFrom(ChangedInterface changedInterface)
    {
        var diff = Diff(this, changedInterface);

        if (diff.Folders is not null)
            Folders = diff.Folders;

        if (diff.Chats is not null)
            Chats = diff.Chats;

        if (diff.Messages is not null)
            Messages = diff.Messages;

        if (diff.Users is not null)
            Users = diff.Users;

        if (diff.SelectedFolderIndex is not null)
            SelectedFolderIndex = diff.SelectedFolderIndex;

        if (diff.SelectedChat is not null)
            SelectedChat = diff.SelectedChat;

        if (diff.SelectedChatIndex is not null)
            SelectedChatIndex = diff.SelectedChatIndex;

        if (diff.CommandInput is not null)
            CommandInput = diff.CommandInput;
    }

    public static ChangedInterface Diff(ChangedInterface old, ChangedInterface @new)
    {
        var diff = new ChangedInterface();

        if (old.Folders is null ||
            @new.Folders is not null && !old.Folders.SequenceEqual(@new.Folders, RenderFolderComparer.Instance))
            diff.Folders = @new.Folders;

        if (old.Chats is null ||
            @new.Chats is not null && !old.Chats.SequenceEqual(@new.Chats, RenderChatComparer.Instance))
            diff.Chats = @new.Chats;

        if (old.Messages is null ||
            @new.Messages is not null && !old.Messages.SequenceEqual(@new.Messages, RenderMessageComparer.Instance))
            diff.Messages = @new.Messages;

        if (old.Users is null ||
            @new.Users is not null &&
            !(old.Users.Keys.SequenceEqual(@new.Users.Keys) &&
              old.Users.Values.SequenceEqual(@new.Users.Values, RenderUserComparer.Instance)))
            diff.Users = @new.Users;

        if (old.SelectedFolderIndex != @new.SelectedFolderIndex)
            diff.SelectedFolderIndex = @new.SelectedFolderIndex;

        if (old.SelectedChat != @new.SelectedChat)
            diff.SelectedChat = @new.SelectedChat;

        if (old.SelectedChatIndex != @new.SelectedChatIndex)
            diff.SelectedChatIndex = @new.SelectedChatIndex;

        if (old.CommandInput != @new.CommandInput)
            diff.CommandInput = @new.CommandInput;

        return diff;
    }

    private class RenderFolderComparer : IEqualityComparer<IRenderFolder>
    {
        public static RenderFolderComparer Instance { get; } = new();

        private RenderFolderComparer()
        {
        }

        public bool Equals(IRenderFolder x, IRenderFolder y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            return x.Id == y.Id &&
                   x.Title == y.Title &&
                   x.SelectedChatIndex == y.SelectedChatIndex &&
                   RenderChatComparer.Instance.Equals(x.SelectedChat, y.SelectedChat) &&
                   x.UnreadChatsCount == y.UnreadChatsCount && 
                   x.HasUnmutedChat == y.HasUnmutedChat;
        }

        public int GetHashCode(IRenderFolder obj)
        {
            return HashCode.Combine(
                obj.Id, obj.Title, obj.SelectedChatIndex, obj.SelectedChat, obj.UnreadChatsCount, obj.HasUnmutedChat);
        }
    }

    private class RenderChatComparer : IEqualityComparer<IRenderChat>
    {
        public static RenderChatComparer Instance { get; } = new();

        private RenderChatComparer()
        {
        }

        public bool Equals(IRenderChat x, IRenderChat y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            return x.Id == y.Id &&
                   x.Title == y.Title &&
                   x.UnreadCount == y.UnreadCount &&
                   x.IsMuted == y.IsMuted &&
                   x.Positions.Keys.SequenceEqual(y.Positions.Keys, RenderFolderComparer.Instance) &&
                   x.Positions.Values.SequenceEqual(y.Positions.Values) &&
                   RenderMessageComparer.Instance.Equals(x.LastMessage, y.LastMessage) &&
                   x.ChatAction == y.ChatAction;
        }

        public int GetHashCode(IRenderChat obj)
        {
            return HashCode.Combine(
                obj.Id, obj.Title, obj.UnreadCount, obj.IsMuted, obj.Positions, obj.LastMessage, obj.ChatAction);
        }
    }

    private class RenderMessageComparer : IEqualityComparer<IRenderMessage>
    {
        public static RenderMessageComparer Instance { get; } = new();

        private RenderMessageComparer()
        {
        }

        public bool Equals(IRenderMessage x, IRenderMessage y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            return x.Id == y.Id && x.Text == y.Text;
        }

        public int GetHashCode(IRenderMessage obj) => HashCode.Combine(obj.Id, obj.Text);
    }

    private class RenderUserComparer : IEqualityComparer<IRenderUser>
    {
        public static RenderUserComparer Instance { get; } = new();

        private RenderUserComparer()
        {
        }

        public bool Equals(IRenderUser x, IRenderUser y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            return x.Id == y.Id && x.IsOnline == y.IsOnline;
        }

        public int GetHashCode(IRenderUser obj) => HashCode.Combine(obj.Id, obj.IsOnline);
    }
}