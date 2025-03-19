using TdLib;

namespace TgCli.Handlers.Update;

public abstract class UpdateHandler<T> : IHandler<TdApi.Update>
{
    protected readonly MainViewModel ViewModel;
    protected readonly Model Model;

    protected UpdateHandler(MainViewModel viewModel, Model model)
    {
        ViewModel = viewModel;
        Model = model;
    }

    protected abstract Task<bool> HandleAsync(T update);

    public async Task<bool> HandleAsync(TdApi.Update obj)
    {
        if (obj is not T t)
            throw new NotSupportedException();

        return await HandleAsync(t);
    }

    public bool CanHandle(TdApi.Update update) => update is T;
}
