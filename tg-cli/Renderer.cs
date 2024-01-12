using System.Text;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace tg_cli;

public interface IRenderer
{
    public int MaxVisibleChatsCount { get; }
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

    private int ConsoleWidthWithoutBorders => _console.Profile.Width - 1 - 1 - 1;
    private int ConsoleHeightWithoutBorders => _console.Profile.Height - 1 - 1 - 1 - 1;

    private int ChatWidth => (int) Math.Round(ConsoleWidthWithoutBorders * ChatWidthMod);
    private int ChatListWidth => ConsoleWidthWithoutBorders - ChatWidth;

    public int MaxVisibleChatsCount => ConsoleHeightWithoutBorders - StupidFuckingLineOnBottomHeight -
                                       CommandsInputHeight - TabsHeight;

    public Renderer(IAnsiConsole console)
    {
        _console = console;
    }

    public void OnRenderRequested(VisibleInterface visibleInterface)
    {
        var chatListLayout = new Table {Border = TableBorder.None, ShowHeaders = false};
        chatListLayout.AddColumn(string.Empty);
        chatListLayout.Columns[0].Padding(0, 0);

        for (var i = 0; i < visibleInterface.Chats.Count; ++i)
        {
            var chat = visibleInterface.Chats[i];
            var chatMarkup = MarkupChat(chat, i == visibleInterface.SelectedChatIndex);
            chatListLayout.AddRow(chatMarkup);
        }

        for (var i = 0; i < MaxVisibleChatsCount - visibleInterface.Chats.Count; ++i)
        {
            chatListLayout.AddRow(" ");
        }

        var chatPanel = new Markup("Messages here")
        {
            Justification = Justify.Center
        };

        var messengerTable = new Table {Border = TableBorder.Square};
        messengerTable.AddColumn("Chats list");
        messengerTable.Columns[0].Width = ChatListWidth;
        messengerTable.Columns[0].Alignment = Justify.Left;

        var selectedChat = visibleInterface.SelectedChatIndex < visibleInterface.Chats.Count
            ? visibleInterface.Chats[visibleInterface.SelectedChatIndex]
            : null;

        messengerTable.AddColumn(selectedChat?.Title?.EscapeMarkup() ?? string.Empty);
        messengerTable.Columns[1].Width = ChatWidth;
        messengerTable.AddRow(chatListLayout, chatPanel);

        var tabs = MarkupTabs(visibleInterface.Folders, visibleInterface.SelectedFolderIndex);

        var mainTable = new Table {Border = TableBorder.None, ShowHeaders = false};
        mainTable.AddColumn(string.Empty);
        mainTable.Columns[0].Padding(0, 0);
        mainTable.AddRow(tabs);
        mainTable.AddRow(messengerTable);
        mainTable.AddRow(visibleInterface.CommandInput);

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

    private IRenderable MarkupChat(Chat chat, bool isSelected)
    {
        const char ellipsis = '\u2026';
        const string infinity = "\u221e";

        var title = chat.Title;

        var unreadText = chat.UnreadCount == 0 ? string.Empty : chat.UnreadCount.ToString();
        if (unreadText.Length > UnreadCounterWidth)
            unreadText = infinity;

        var chatTitleWidth = unreadText.Length > 0 ? ChatListWidth - unreadText.Length - 1 : ChatListWidth;
        if (chat.Title.Length > chatTitleWidth)
        {
            title = chatTitleWidth < 1 ? ellipsis.ToString() : title[..(chatTitleWidth - 1)] + ellipsis;
        }
        else
        {
            title += new string(' ', chatTitleWidth - chat.Title.Length);
        }

        const string titleMarkupTemplate = "{0}";
        const string selectedTitleMarkupTemplate = $"[invert]{titleMarkupTemplate}[/]";
        const string unreadMarkupTemplate = "[{0}] {1}[/]";

        var currentTitleMarkupTemplate = isSelected ? selectedTitleMarkupTemplate : titleMarkupTemplate;
        var escapedTitle = title.EscapeMarkup();
        var titleMarkup = string.Format(currentTitleMarkupTemplate, escapedTitle);

        var currentUnreadColor = chat.IsMuted ? MutedUnreadColor : UnreadColor;
        currentUnreadColor += isSelected ? " invert" : string.Empty;
        var unreadMarkup = string.Format(unreadMarkupTemplate, currentUnreadColor, unreadText);

        var markup = $"{titleMarkup}{unreadMarkup}";
        return new Markup(markup);
    }
}