using System.Text;
using Spectre.Console;
using Spectre.Console.Rendering;
using tg_cli.ViewModels;

namespace tg_cli;

public interface IRenderer
{
    public int MaxVisibleChatsCount { get; }
    public int MaxVisibleMessagesCount { get; }
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

    private readonly IAnsiConsole _console;
    private readonly TgCliSettings _settings;

    private int ConsoleWidthWithoutBorders => _console.Profile.Width - 1 - 1 - 1;
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

    public int MaxVisibleMessagesCount =>
        ConsoleHeightWithoutBorders - StupidFuckingLineOnBottomHeight - CommandsInputHeight - TabsHeight;

    public Renderer(IAnsiConsole console, TgCliSettings settings)
    {
        _console = console;
        _settings = settings;
    }

    public void OnRenderRequested(VisibleInterface visibleInterface)
    {
        var chatListLayout = new Table {Border = TableBorder.None, ShowHeaders = false};
        chatListLayout.AddColumn(string.Empty);
        chatListLayout.Columns[0].Padding(0, 0);

        for (var i = 0; i < visibleInterface.Chats.Count; ++i)
        {
            var chat = visibleInterface.Chats[i];
            var isOnline = visibleInterface.Users.TryGetValue(chat.Id, out var user) && user.IsOnline;
            var chatMarkup = MarkupChat(chat, i == visibleInterface.SelectedChatIndex, isOnline);
            chatListLayout.AddRow(chatMarkup);
        }

        for (var i = 0; i < MaxVisibleChatsCount - visibleInterface.Chats.Count; ++i)
        {
            chatListLayout.AddRow(" "); // chat title
            chatListLayout.AddRow(" "); // chat preview
        }

        var messagesTable = new Table {Border = TableBorder.None, ShowHeaders = false, Expand = true};
        messagesTable.AddColumn(string.Empty);

        var messengerTable = new Table {Border = TableBorder.Square};
        messengerTable.AddColumn("Chats list");
        messengerTable.Columns[0].Width = ChatListWidth;
        messengerTable.Columns[0].Alignment = Justify.Left;

        var selectedChat = visibleInterface.SelectedChatIndex < visibleInterface.Chats.Count
            ? visibleInterface.Chats[visibleInterface.SelectedChatIndex]
            : null;

        messengerTable.AddColumn(selectedChat?.Title?.EscapeMarkup() ?? string.Empty);
        messengerTable.Columns[1].Width = ChatWidth;
        messengerTable.AddRow(chatListLayout, messagesTable);

        var tabs = MarkupTabs(visibleInterface.Folders, visibleInterface.SelectedFolderIndex);

        var mainTable = new Table {Border = TableBorder.None, ShowHeaders = false};
        mainTable.AddColumn(string.Empty);
        mainTable.Columns[0].Padding(0, 0);
        mainTable.AddRow(tabs);
        mainTable.AddRow(messengerTable);
        mainTable.AddRow(visibleInterface.CommandInput);
        
        var realChatWidth = ChatWidth - 6; // magic number
        var oddStyle = new Style(null, Color.Grey);
        var fakeConsole = new FakeConsole(realChatWidth, _console.Profile.Height);
        var linesCount = MaxVisibleMessagesCount;
        for (var i = visibleInterface.Messages.Count - 1; i >= 0; --i)
        {
            var message = visibleInterface.Messages[i];
            var text = new Text(message.Text.EscapeMarkup()).Fold();
            var segments = text.GetSegments(fakeConsole);
            var renderedText = segments.Select(s => s.Text).Aggregate((t, s) => t + s);
            var renderedLinesCount = renderedText.Count(c => c == '\n') + 1;
            linesCount -= renderedLinesCount;
            if (linesCount < 0)
                break;

            var style = i % 2 == 0 ? Style.Plain : oddStyle;
            messagesTable.InsertRow(0, new Text(renderedText, style));
        }

        _console.Clear();
        _console.Write(mainTable);
    }

    private static string MarkupTabs(IReadOnlyList<Folder> folders, int selectedFolderIndex)
    {
        StringBuilder sb = new(" ");
        for (var i = 0; i < folders.Count; ++i)
        {
            var folder = folders[i];
            var markup = i == selectedFolderIndex ? $"[underline]{folder.Title}[/]" : $"{folder.Title}";

            sb.Append(markup);

            var unreadChats = folder.Chats.Where(c => c.UnreadCount > 0).ToList();
            var color = unreadChats.All(c => c.IsMuted) ? MutedUnreadColor : UnreadColor;
            var unreadChatsCount = unreadChats.Count;
            if (unreadChatsCount > 0)
                sb.Append($" [{color}][[{unreadChatsCount}]][/]");

            if (i != folders.Count - 1)
                sb.Append(" | ");
        }

        var tabs = sb.ToString();
        return tabs;
    }

    private IRenderable MarkupChat(Chat chat, bool isSelected, bool isOnline)
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

        const int guideWidth = 4;
        var previewMessageWidth = ChatListWidth - guideWidth;
        var lastMessagePreview = "<empty>";

        if (chat.ChatAction is not null)
            lastMessagePreview = CropString(chat.ChatAction, previewMessageWidth);
        else if (chat.LastMessage is not null)
            lastMessagePreview = CropString(chat.LastMessage.Text, previewMessageWidth);

        var markup = $"{titleMarkup}{unreadMarkup}";
        var tree = new Tree(new Markup(markup)) {Style = new Style(null, null, Decoration.Dim)};
        tree.AddNode(new Markup(lastMessagePreview, new Style(null, null, Decoration.Dim)));

        return tree;
    }

    private static string CropString(string str, int width)
    {
        const char ellipsis = '\u2026';
        var lines = str.Split('\n');
        str = lines[0].EscapeMarkup();
        if (str.Length > width)
        {
            str = width < 1
                ? ellipsis.ToString()
                : str[..(width - 1)] + ellipsis;
        }
        else
        {
            str += new string(' ', width - str.Length);
        }

        return str;
    }
}