namespace tg_cli.Handlers;

public interface IHandler<in T>
{
    public bool CanHandle(T obj);
    public Task<bool> HandleAsync(T obj);
}

public class HandlersCollection<T>
{
    private readonly List<IHandler<T>> _handlers = new();

    public void Register(IHandler<T> handler) => _handlers.Add(handler);

    public async Task<bool> HandleAsync(T update)
    {
        var handler = _handlers.FirstOrDefault(h => h.CanHandle(update));
        if (handler is null)
            return false;
            
        return await handler.HandleAsync(update);
    }
}