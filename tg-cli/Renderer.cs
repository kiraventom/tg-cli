using Spectre.Console;
using Spectre.Console.Rendering;

namespace tg_cli;

public class Renderer
{
    private static readonly Style SelectedChatStyle = new(Color.Black, Color.White);

    private const double ChatListWidthMod = 0.2;
    private const double ChatWidthMod = 1 - ChatListWidthMod;

    private readonly IAnsiConsole _console;

    private int ConsoleWidthWithoutBorders => _console.Profile.Width - 1 - 1 - 1;

    private int ChatWidth => (int) Math.Round(ConsoleWidthWithoutBorders * ChatWidthMod);
    private int ChatListWidth => ConsoleWidthWithoutBorders - ChatWidth;

    public Renderer(IAnsiConsole console)
    {
        _console = console;
    }

    public void OnRenderRequested(VisibleInterface visibleInterface)
    {
        _console.Clear();

        var chatListTable = new Table {Border = TableBorder.None, ShowHeaders = false};
        chatListTable.AddColumn(string.Empty);
        chatListTable.Columns[0].Padding(0, 0);
        
        for (var i = 0; i < visibleInterface.Chats.Count; ++i)
        {
            var chatTitle = visibleInterface.Chats[i];
            var markup = MarkupChatTitle(chatTitle, ChatListWidth, i == visibleInterface.SelectedIndex);
            chatListTable.AddRow(markup);
        }

        var chatPanel = new Markup("Messages here")
        {
            Justification = Justify.Center
        };

        var mainTable = new Table {Border = TableBorder.Rounded};
        mainTable.AddColumn("Chats list");
        mainTable.Columns[0].Width = ChatListWidth;
        mainTable.Columns[0].Alignment = Justify.Left;

        var selectedChatTitle = visibleInterface.Chats[visibleInterface.SelectedIndex];
        mainTable.AddColumn(selectedChatTitle.EscapeMarkup());
        mainTable.Columns[1].Width = ChatWidth;
        
        mainTable.AddRow(chatListTable, chatPanel);

        _console.Write(mainTable);
    }

    private static IRenderable MarkupChatTitle(string title, int crop, bool isSelected)
    {
        const char ellipsis = '\u2026';

        if (crop > 0)
        {
            if (title.Length > crop)
                title = title[..(crop - 1)] + ellipsis;
        }

        return new Markup(title.EscapeMarkup(), isSelected ? SelectedChatStyle : null);
    }
}