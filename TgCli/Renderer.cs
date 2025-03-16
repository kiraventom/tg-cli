using System.Text;
using Spectre.Console;
using Spectre.Console.Rendering;
using TgCli.Extensions;
using TgCli.Utils;

namespace TgCli;

public interface IRenderer
{
    public int MaxVisibleChatsCount { get; }
    public int MaxVisibleMessagesCount { get; }
}

public interface IRenderFolder
{
    public int Id { get; }
    public string Title { get; }
    public int SelectedChatIndex { get; }
    public IRenderChat SelectedChat { get; }
    public int UnreadChatsCount { get; }
    public bool HasUnmutedChat { get; }
}

public interface IRenderChat
{
    public long Id { get; }
    public string Title { get; }
    public int UnreadCount { get; }
    public bool IsMuted { get; }
    public IReadOnlyDictionary<IRenderFolder, long> Positions { get; }
    public IRenderMessage LastMessage { get; }
    public string ChatAction { get; }
}

public interface IRenderMessage
{
    public long Id { get; }
    public string Text { get; }
}

public interface IRenderUser
{
    public long Id { get; }
    public bool IsOnline { get; }
}

public class Renderer : IRenderer
{
    private const string UnreadColor = "blue";
    private const string MutedUnreadColor = "gray";

    private const double ChatListWidthMod = 0.2;
    private const double ChatWidthMod = 1 - ChatListWidthMod;
    private const int UnreadCounterWidth = 3;

    private const int StupidFuckingLineOnBottomHeight = 1;
    private const int CommandsInputHeight = 1;
    private const int TabsHeight = 1;

    private const int ChatListLeft = 3;
    private const int ChatListTop = 5;
    private int MessagesLeft => ChatListWidth + 6;
    private const int MessagesTop = 5;

    private readonly IAnsiConsole _console;
    private readonly TgCliSettings _settings;
    private readonly RendererCache _cache;

    private int ConsoleWidthWithoutBorders => _console.Profile.Width - 7;
    private int ConsoleHeightWithoutBorders => _console.Profile.Height - 1 - 1 - 1 - 1;

    private int ChatWidth
    {
        get
        {
            var defaultChatWidth = (int) Math.Round(ConsoleWidthWithoutBorders * ChatWidthMod);
            if (Math.Abs(_settings.SeparatorOffset) >= defaultChatWidth)
                return 0;

            return defaultChatWidth - _settings.SeparatorOffset;
        }
    }

    private int ChatListWidth => ConsoleWidthWithoutBorders - ChatWidth;

    public int MaxVisibleChatsCount =>
        (ConsoleHeightWithoutBorders - StupidFuckingLineOnBottomHeight - CommandsInputHeight - TabsHeight) / 2;

    public int MaxVisibleMessagesCount => MaxVisibleChatsCount * 2;

    public Renderer(IAnsiConsole console, TgCliSettings settings)
    {
        _console = console;
        _settings = settings;
        _cache = new RendererCache(this);
    }

    public void OnRenderRequested(ChangedInterface changedInterface)
    {
        var fullRender = _console.Profile.Width != _cache.Width || _console.Profile.Height != _cache.Height;
        if (fullRender)
        {
            var fullInterface = _cache.LastInterface;
            fullInterface.UpdateFrom(changedInterface);
            changedInterface = fullInterface;

            _cache.Width = _console.Profile.Width;
            _cache.Height = _console.Profile.Height;
            _console.Clear();
            _console.Write(_cache.MainTable);
        }

        if (changedInterface.Chats is { } chats)
        {
            var newIndex = changedInterface.SelectedChatIndex;
            RenderChatList(chats, newIndex);
        }
        else if (changedInterface.SelectedChat is { } chat && changedInterface.SelectedChatIndex is { } chatIndex)
        {
            if (_cache.LastInterface.SelectedChat is { } oldChat && _cache.LastInterface.SelectedChatIndex is { } oldIndex)
            {
                RenderChatTitleInList(oldChat, oldIndex, false);
            }

            RenderChatTitleInList(chat, chatIndex, true);
            RenderChatTitleInHeader(chat);
        }

        if (changedInterface.Folders is { } folders)
        {
            RenderFolders(folders, changedInterface.SelectedFolderIndex);
        }
        
        if (changedInterface.SelectedFolderIndex is {} folderIndex)
        {
            RenderFolders(_cache.LastInterface.Folders, folderIndex);
        }

        if (changedInterface.CommandInput is { } commandInput)
        {
            RenderCommandInput(commandInput);
        }

        if (changedInterface.Messages is { } messages)
        {
            RenderMessages(messages);
        }

        _cache.LastInterface.UpdateFrom(changedInterface);
    }

    private void RenderCommandInput(string commandInput)
    {
        var croppedCommandInput = CropString(commandInput, _console.Profile.Width);
        var commandInputText = new Text(croppedCommandInput);
        _console.WriteAt(commandInputText, 1, _console.Profile.Height - 1);
    }

    private void RenderFolders(IReadOnlyList<IRenderFolder> folders, int? folderIndex)
    {
        var tabs = MarkupTabs(folders, folderIndex);
        _console.WriteAt(tabs, 0, 0);
    }

    private void RenderMessages(IReadOnlyList<IRenderMessage> messages)
    {
        var realChatWidth = ChatWidth - 6; // magic number
        var oddStyle = new Style(null, Color.Grey);
        var fakeConsole = new FakeConsole(realChatWidth, _console.Profile.Height);
        var linesCount = MaxVisibleMessagesCount;

        var messageTextsList = new List<Text>();
        for (var i = messages.Count - 1; i >= 0; --i)
        {
            // var message = messages[i];
            // var text = new Text(message.Text.EscapeMarkup()).Fold();
            // var segments = text.GetSegments(fakeConsole);
            // var renderedText = segments.Select(s => s.Text).Aggregate((t, s) => t + s);
            // var renderedLinesCount = renderedText.Count(c => c == '\n') + 1;
            // linesCount -= renderedLinesCount;
            // if (linesCount < 0)
            //     break;
            //
            // var style = i % 2 == 0 ? Style.Plain : oddStyle;
            // messageTextsList.Insert(0, new Text(renderedText, style));

            var firstLine = messages[i].Text.Split('\n', (StringSplitOptions)0b11)[0];
            firstLine = StringUtils.RemoveNonUtf16Characters(firstLine);
            var text = CropString(firstLine.EscapeMarkup(), realChatWidth);

            var style = i % 2 == 0 ? Style.Plain : oddStyle;
            messageTextsList.Insert(0, new Text(text, style));
        }

        for (var i = 0; i < messageTextsList.Count; ++i)
        {
            var messageText = messageTextsList[i];
            var topOffset = MessagesTop + i;
            _console.WriteAt(messageText, MessagesLeft, topOffset);
        }
    }

    private void RenderChatTitleInHeader(IRenderChat chat)
    {
        // Render messages table title
        var escapedTitle = chat.Title.EscapeMarkup();
        var titleText = new Text(CropString(escapedTitle, ChatWidth));
        _console.WriteAt(titleText, ChatListWidth + 6, 3);
    }

    // private void RenderChatTitleInList(IRenderChat selectedChat, int selectedChatIndex, bool isSelected)
    // {
    //     // Render unselected chat in list
    //     if (_cache.LastInterface.SelectedChat is { } oldChat && _cache.LastInterface.SelectedChatIndex is { } oldIndex)
    //         test();
    //
    //     // Render selected chat in list
    //     var selectedChatMarkup = MarkupChat(selectedChat, true, false);
    //     var selectedChatTopOffset = ChatListTop + selectedChatIndex * 2;
    //     _console.WriteAt(selectedChatMarkup, ChatListLeft, selectedChatTopOffset);
    // }

    private void RenderChatTitleInList(IRenderChat chat, int chatIndex, bool isSelected)
    {
        var chatMarkup = MarkupChat(chat, isSelected, false);
        var chatTopOffset = ChatListTop + chatIndex * 2;
        _console.WriteAt(chatMarkup, ChatListLeft, chatTopOffset);
    }

    private void RenderChatList(IReadOnlyList<IRenderChat> chats, int? newSelectedChatIndex)
    {
        for (var i = 0; i < chats.Count; ++i)
        {
            var chat = chats[i];
            // var isOnline = changedInterface.Users.TryGetValue(chat.Id, out var user) && user.IsOnline;
            // var chatMarkup = MarkupChat(chat, i == changedInterface.SelectedChatIndex, isOnline);

            var topOffset = ChatListTop + i * 2;
            var chatMarkup = MarkupChat(chat, false, false);
            var lastMessagePreview = MarkupLastMessage(chat);
            _console.WriteAt(chatMarkup, ChatListLeft, topOffset);
            _console.WriteAt(lastMessagePreview, ChatListLeft, topOffset + 1);
        }

        for (var i = chats.Count; i < MaxVisibleChatsCount; ++i)
        {
            var topOffset = ChatListTop + i * 2;
            _console.WriteAt(new Text(CropString(" ", ChatListWidth)), ChatListLeft, topOffset);
            _console.WriteAt(new Text(CropString(" ", ChatListWidth)), ChatListLeft, topOffset + 1);
        }

        // If scroll changed top index
        if (chats.Count > 0 && _cache.LastInterface.SelectedChatIndex is { } oldIndex)
        {
            int newIndex;
            IRenderChat oldChat;
            
            // If new index is not equal to previous (e.g., 'gg' or 'G')
            if (newSelectedChatIndex is not null)
            {
                newIndex = newSelectedChatIndex.Value;
                oldChat = chats[oldIndex];
            }
            else // If new index is equal to previous (e.g., 'j' on the bottom of the list)
            {
                newIndex = oldIndex;
                oldChat = _cache.LastInterface.SelectedChat;
            }
            
            if (oldChat is null)
                return;

            var newSelectedChat = chats[newIndex];
            
            RenderChatTitleInList(oldChat, oldIndex, false);
            RenderChatTitleInList(newSelectedChat, newIndex, true);
            RenderChatTitleInHeader(newSelectedChat);
        }
    }

    private static IRenderable MarkupTabs(IReadOnlyList<IRenderFolder> folders, int? selectedFolderIndex)
    {
        StringBuilder sb = new(" ");
        for (var i = 0; i < folders.Count; ++i)
        {
            var folder = folders[i];
            var markup = i == selectedFolderIndex ? $"[underline]{folder.Title}[/]" : $"{folder.Title}";

            sb.Append(markup);

            var color = folder.HasUnmutedChat ? UnreadColor : MutedUnreadColor;
            var unreadChatsCount = folder.UnreadChatsCount;
            if (unreadChatsCount > 0)
                sb.Append($" [{color}][[{unreadChatsCount}]][/]");

            if (i != folders.Count - 1)
                sb.Append(" | ");
        }

        var tabs = sb.ToString();
        return new Markup(tabs);
    }

    private IRenderable MarkupChat(IRenderChat chat, bool isSelected, bool isOnline)
    {
        const string infinity = "\u221e";

        var unreadText = chat.UnreadCount == 0 ? string.Empty : chat.UnreadCount.ToString();
        if (unreadText.Length > UnreadCounterWidth)
            unreadText = infinity;

        var chatTitleWidth = unreadText.Length > 0 ? ChatListWidth - unreadText.Length - 1 : ChatListWidth;
        var title = CropString(chat.Title, chatTitleWidth);

        const string titleMarkupTemplate = "{0}";
        const string onlineTitleMarkupTemplate = "[green]{0}[/]";
        const string selectedTitleMarkupTemplate = "[invert]{0}[/]";
        const string unreadMarkupTemplate = "[{0}] {1}[/]";

        var markupTemplate = titleMarkupTemplate;
        markupTemplate = isOnline ? string.Format(onlineTitleMarkupTemplate, markupTemplate) : markupTemplate;
        markupTemplate = isSelected ? string.Format(selectedTitleMarkupTemplate, markupTemplate) : markupTemplate;

        var escapedTitle = title.EscapeMarkup();
        var titleMarkup = string.Format(markupTemplate, escapedTitle);

        var currentUnreadColor = chat.IsMuted ? MutedUnreadColor : UnreadColor;
        currentUnreadColor += isSelected ? " invert" : string.Empty;
        var unreadMarkup = string.Format(unreadMarkupTemplate, currentUnreadColor, unreadText);
        var markup = chat.UnreadCount == 0 ? $"{titleMarkup}" : $"{titleMarkup}{unreadMarkup}";

        return new Markup(markup);
    }

    private IRenderable MarkupLastMessage(IRenderChat chat)
    {
        const int offsetWidth = 4;
        var previewMessageWidth = ChatListWidth - offsetWidth;
        var lastMessagePreview = CropString("<empty>", previewMessageWidth);

        if (chat.ChatAction is { } chatAction)
            lastMessagePreview = CropString(chatAction, previewMessageWidth);
        else if (chat.LastMessage is { } lastMessage)
            lastMessagePreview = CropString(lastMessage.Text, previewMessageWidth);

        var lastMessageMarkup = new Markup(lastMessagePreview, new Style(null, null, Decoration.Dim));
        var padder = new Padder(lastMessageMarkup, new Padding(offsetWidth, 0, 0, 0));

        return padder;
    }

    private static string CropString(string str, int width)
    {
        const char ellipsis = '\u2026';
        var lines = str.Split('\n');
        str = lines[0].EscapeMarkup();
        if (str.Length > width)
        {
            str = width <= 1
                ? ellipsis.ToString()
                : str[..(width - 1)] + ellipsis;
        }
        else
        {
            str += new string(' ', width - str.Length);
        }

        return str;
    }

    private class RendererCache
    {
        public Table MainTable { get; }
        public Table MessengerTable { get; }
        public Table ChatListTable { get; }
        public Table MessagesTable { get; }
        public ChangedInterface LastInterface { get; } = new();
        public int Width { get; set; }
        public int Height { get; set; }

        public RendererCache(Renderer renderer)
        {
            ChatListTable = new Table {Border = TableBorder.None, ShowHeaders = false};
            ChatListTable.AddColumn(string.Empty);
            ChatListTable.Columns[0].Padding(0, 0);

            MessagesTable = new Table {Border = TableBorder.None, ShowHeaders = false, Expand = true};
            MessagesTable.AddColumn(string.Empty);

            MessengerTable = new Table {Border = TableBorder.Square};
            MessengerTable.AddColumn("Chats list");
            MessengerTable.Columns[0].Width = renderer.ChatListWidth;
            MessengerTable.Columns[0].Alignment = Justify.Left;
            MessengerTable.AddColumn(string.Empty);
            MessengerTable.Columns[1].Width = renderer.ChatWidth;

            var emptyTable = new Table {Border = TableBorder.None, ShowHeaders = false};
            emptyTable.AddColumn(string.Empty);
            for (var i = 0; i < renderer.MaxVisibleChatsCount; ++i)
            {
                emptyTable.AddRow(" ");
                emptyTable.AddRow(" ");
            }

            MessengerTable.AddRow(emptyTable, emptyTable);

            MainTable = new Table {Border = TableBorder.None, ShowHeaders = false};
            MainTable.AddColumn(string.Empty);
            MainTable.Columns[0].Padding(0, 0);
            MainTable.AddRow(string.Empty);
            MainTable.AddRow(MessengerTable);
            MainTable.AddRow(string.Empty);
        }
    }
}