using Spectre.Console;
using TdLib;
using TgCli.Extensions;

namespace TgCli;

public class Authorizer
{
    private readonly IAnsiConsole _console;

    public Authorizer(IAnsiConsole console)
    {
        _console = console;
    }
    
    public async void OnClientUpdateReceived(object sender, TdApi.Update update)
    {
        var client = (TdClient)sender;
        if (update is not TdApi.Update.UpdateAuthorizationState updateAuthState)
            return;
            
        switch (updateAuthState.AuthorizationState)
        {
            case TdApi.AuthorizationState.AuthorizationStateWaitPhoneNumber:
                var phoneNumber = _console.Ask<string>("Enter phone number in international format: ",
                    n => n.StartsWith('+') && n[1..].All(char.IsDigit));
                await client.SetAuthenticationPhoneNumberAsync(phoneNumber);
                break;

            case TdApi.AuthorizationState.AuthorizationStateWaitCode:
                var code = _console.Ask<string>("Enter code: ",
                    c => c.All(char.IsDigit));
                await client.CheckAuthenticationCodeAsync(code);
                break;

            case TdApi.AuthorizationState.AuthorizationStateWaitPassword:
                var password = _console.Ask<string>("Enter password: ", p => !string.IsNullOrEmpty(p));
                await client.CheckAuthenticationPasswordAsync(password);
                break;
        }
    }
}