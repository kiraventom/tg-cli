using System.Text;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace tg_cli;

public class FakeConsole : IAnsiConsole
{
    public Profile Profile { get; }
    public RenderPipeline Pipeline { get; }
    public IAnsiConsoleCursor Cursor => throw new NotSupportedException();
    public IAnsiConsoleInput Input => throw new NotSupportedException();
    public IExclusivityMode ExclusivityMode => throw new NotSupportedException();

    public FakeConsole(int width, int height)
    {
        Profile = new Profile(new FakeConsoleOutput(width, height), Encoding.Default);
        Pipeline = new RenderPipeline();
    }
    
    public void Clear(bool home) => throw new NotSupportedException();

    public void Write(IRenderable renderable) => throw new NotSupportedException();
}

public class FakeConsoleOutput : IAnsiConsoleOutput
{
    public int Width { get; }
    public int Height { get; }
    public TextWriter Writer => throw new NotSupportedException();
    public bool IsTerminal => throw new NotSupportedException();

    public FakeConsoleOutput(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public void SetEncoding(Encoding encoding) => throw new NotSupportedException();
}