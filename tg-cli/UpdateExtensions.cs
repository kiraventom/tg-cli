using SettingsManagement;
using TdLib;

namespace tg_cli;

public static class UpdateExtensions
{
    public static void Log(this TdApi.Update update, IReadOnlyDictionary<long, Chat> chatsDict)
    {
        switch (update)
        {
            case TdApi.Update.UpdateNewChat updateNewChat:
            {
                var chat = updateNewChat.Chat;
                Program.Logger.Information("New chat: {title} ({id})", chat.Title, chat.Id);
                break;
            }

            case TdApi.Update.UpdateChatPosition updateChatPosition:
            {
                Program.Logger.Information("Chat position: {order} in {list} ({title})",
                    updateChatPosition.Position.Order,
                    updateChatPosition.Position.List.DataType,
                    chatsDict.TryGetValue(updateChatPosition.ChatId, out var chat)
                        ? chat.Title
                        : updateChatPosition.ChatId);
                break;
            }

            case TdApi.Update.UpdateNewMessage updateNewMessage:
            {
                Program.Logger.Information("New message: '{contentType}' ({title})",
                    updateNewMessage.Message.Content.DataType,
                    chatsDict.TryGetValue(updateNewMessage.Message.ChatId, out var chat)
                        ? chat.Title
                        : updateNewMessage.Message.ChatId);
                break;
            }

            case TdApi.Update.UpdateChatLastMessage updateChatLastMessage:
            {
                Program.Logger.Information("Chat last message: '{contentType}' ({title})",
                    updateChatLastMessage?.LastMessage?.Content?.DataType ?? "<null>",
                    chatsDict.TryGetValue(updateChatLastMessage.ChatId, out var chat)
                        ? chat.Title
                        : updateChatLastMessage.ChatId);
                break;
            }

            case TdApi.Update.UpdateUser updateUser:
            {
                Program.Logger.Information("User: '{user}' ({id})", updateUser.User.FirstName, updateUser.User.Id);
                break;
            }

            case TdApi.Update.UpdateOption updateOption:
            {
                var valueStr = updateOption.Value switch
                {
                    TdApi.OptionValue.OptionValueBoolean b => b.Value.ToString(),
                    TdApi.OptionValue.OptionValueInteger i => i.Value.ToString(),
                    TdApi.OptionValue.OptionValueString s => s.Value,
                    TdApi.OptionValue.OptionValueEmpty => "<null>",
                    _ => throw new NotSupportedException()
                };

                Program.Logger.Information("Option '{name}': {value}", updateOption.Name, valueStr);
                break;
            }

            case TdApi.Update.UpdateScopeNotificationSettings updateScopeNotificationSettings:
            {
                Program.Logger.Information(
                    "Scope notification settings '{scope}': {isMuted}",
                    updateScopeNotificationSettings.Scope.DataType,
                    updateScopeNotificationSettings.NotificationSettings.MuteFor != 0);
                break;
            }

            default:
                Program.Logger.Information(update.DataType);
                break;
        }
    }
}