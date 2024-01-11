using Spectre.Console;
using Spectre.Console.Rendering;

namespace tg_cli;

public interface IRenderer
{
    public int VisibleChatsCount { get; }
}

public class Renderer : IRenderer
{
    private const double ChatListWidthMod = 0.2;
    private const double ChatWidthMod = 1 - ChatListWidthMod;
    private const int UnreadCounterWidth = 3;

    private readonly IAnsiConsole _console;

    private int ConsoleWidthWithoutBorders => _console.Profile.Width - 1 - 1 - 1;
    private int ConsoleHeightWithoutBorders => _console.Profile.Height - 1 - 1 - 1 - 1;

    private int ChatWidth => (int) Math.Round(ConsoleWidthWithoutBorders * ChatWidthMod);
    private int ChatListWidth => ConsoleWidthWithoutBorders - ChatWidth;

    public int VisibleChatsCount => ConsoleHeightWithoutBorders - 1 - 1;

    public Renderer(IAnsiConsole console)
    {
        _console = console;
    }

    public void OnRenderRequested(VisibleInterface visibleInterface)
    {
        var chatListTable = new Table {Border = TableBorder.None, ShowHeaders = false};
        chatListTable.AddColumn(string.Empty);
        chatListTable.Columns[0].Padding(0, 0);

        for (var i = 0; i < visibleInterface.Chats.Count; ++i)
        {
            var chat = visibleInterface.Chats[i];
            var chatMarkup = MarkupChat(chat, i == visibleInterface.SelectedIndex);
            chatListTable.AddRow(chatMarkup);
        }

        var chatPanel = new Markup("Messages here")
        {
            Justification = Justify.Center
        };

        var messengerTable = new Table {Border = TableBorder.Rounded};
        messengerTable.AddColumn("Chats list");
        messengerTable.Columns[0].Width = ChatListWidth;
        messengerTable.Columns[0].Alignment = Justify.Left;

        var selectedChat = visibleInterface.Chats[visibleInterface.SelectedIndex];
        messengerTable.AddColumn(selectedChat.Title.EscapeMarkup());
        messengerTable.Columns[1].Width = ChatWidth;

        messengerTable.AddRow(chatListTable, chatPanel);

        var mainTable = new Table {Border = TableBorder.None, ShowHeaders = false};
        mainTable.AddColumn(string.Empty);
        mainTable.Columns[0].Padding(0, 0);
        mainTable.AddRow(messengerTable);
        mainTable.AddRow(visibleInterface.CommandInput);

        _console.Clear();
        _console.Write(mainTable);
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

        var titleMarkup = isSelected ? $"[invert]{title.EscapeMarkup()}[/]" : title.EscapeMarkup();
        var unreadMarkup = isSelected ? $"[blue invert] {unreadText}[/]" : $"[blue] {unreadText}[/]";
        var markup = $"{titleMarkup}{unreadMarkup}";
        return new Markup(markup);
    }
}