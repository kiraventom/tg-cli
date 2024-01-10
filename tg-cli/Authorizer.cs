using Spectre.Console;
using TdLib;

namespace tg_cli;

public static class Authorizer
{
    public static async Task OnAuthorizationStateUpdateReceived(IAnsiConsole console, TdClient client, TdApi.Update.UpdateAuthorizationState updateAuthState)
    {
        switch (updateAuthState.AuthorizationState)
        {
            case TdApi.AuthorizationState.AuthorizationStateWaitPhoneNumber:
                var phoneNumber = console.Ask<string>("Enter phone number in international format: ",
                    n => n.StartsWith('+') && n[1..].All(char.IsDigit));
                await client.SetAuthenticationPhoneNumberAsync(phoneNumber);
                break;

            case TdApi.AuthorizationState.AuthorizationStateWaitCode:
                var code = console.Ask<string>("Enter code: ",
                    c => c.All(char.IsDigit));
                await client.CheckAuthenticationCodeAsync(code);
                break;

            case TdApi.AuthorizationState.AuthorizationStateWaitPassword:
                var password = console.Ask<string>("Enter password: ", p => !string.IsNullOrEmpty(p));
                await client.CheckAuthenticationPasswordAsync(password);
                break;
        }
    }
}