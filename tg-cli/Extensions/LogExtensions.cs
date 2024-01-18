using Serilog.Core;
using TdLib;
using tg_cli.ViewModels;

namespace tg_cli.Extensions;

public static class LogExtensions
{
    public static void LogUpdate(this Logger logger, TdApi.Update update, IReadOnlyDictionary<long, Chat> chatsDict)
    {
        switch (update)
        {
            case TdApi.Update.UpdateAuthorizationState updateAuthorizationState:
            {
                logger.Information("Authorization state: {state}",
                    updateAuthorizationState.AuthorizationState.DataType);
                break;
            }

            case TdApi.Update.UpdateChatFolders updateChatFolders:
            {
                var titles = updateChatFolders.ChatFolders.Select(cf => cf.Title);
                logger.Information("Chat folders: {titles}", string.Join(", ", titles));
                break;
            }

            case TdApi.Update.UpdateNewChat updateNewChat:
            {
                var chat = updateNewChat.Chat;
                logger.Information("New chat: {title} [{id}]", chat.Title, chat.Id);
                break;
            }

            case TdApi.Update.UpdateChatPosition updateChatPosition:
            {
                var listName = updateChatPosition.Position.List switch
                {
                    TdApi.ChatList.ChatListArchive chatListArchive => "Archive",
                    TdApi.ChatList.ChatListFolder chatListFolder => $"Folder '{chatListFolder.ChatFolderId}'",
                    TdApi.ChatList.ChatListMain chatListMain => "All chats",
                };
                
                logger.Information("Chat position: {order} in {list} ({titleAndId})", updateChatPosition.Position.Order,
                    listName, GetChatTitleAndId(updateChatPosition.ChatId, chatsDict));
                break;
            }

            case TdApi.Update.UpdateNewMessage updateNewMessage:
            {
                logger.Information("New message: '{contentType}' ({titleAndId})",
                    updateNewMessage.Message.Content.DataType,
                    GetChatTitleAndId(updateNewMessage.Message.ChatId, chatsDict));
                break;
            }

            case TdApi.Update.UpdateChatLastMessage updateChatLastMessage:
            {
                var content = updateChatLastMessage?.LastMessage?.Content?.GetContentString();

                var positions = updateChatLastMessage.Positions.Select(p => p.Order + " in " + p.List.DataType);
                var positionsStr = string.Join(", ", positions);

                logger.Information("Chat last message: '{content}', Positions={positions} ({titleAndId})",
                    content, positionsStr, GetChatTitleAndId(updateChatLastMessage.ChatId, chatsDict));
                break;
            }

            case TdApi.Update.UpdateUser updateUser:
            {
                logger.Information("User: '{firstName} {lastName}' @{username} [{id}]",
                    updateUser.User.FirstName, updateUser.User.LastName,
                    updateUser.User.Usernames?.ActiveUsernames?.FirstOrDefault(),
                    updateUser.User.Id);
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

                logger.Information("Option '{name}': {value}", updateOption.Name, valueStr);
                break;
            }

            case TdApi.Update.UpdateScopeNotificationSettings updateScopeNotificationSettings:
            {
                logger.Information(
                    "Scope notification settings '{scope}': Muted={isMuted}",
                    updateScopeNotificationSettings.Scope.DataType,
                    updateScopeNotificationSettings.NotificationSettings.MuteFor != 0);
                break;
            }

            case TdApi.Update.UpdateChatNotificationSettings updateChatNotificationSettings:
            {
                if (updateChatNotificationSettings.NotificationSettings.UseDefaultMuteFor)
                    return;

                logger.Information(
                    "Chat notification settings '{titleAndId}': Muted={isMuted}",
                    GetChatTitleAndId(updateChatNotificationSettings.ChatId, chatsDict),
                    updateChatNotificationSettings.NotificationSettings.MuteFor != 0);
                break;
            }

            case TdApi.Update.UpdateChatReadInbox updateChatReadInbox:
            {
                logger.Information(
                    "Chat read inbox: LastMessageId={lastMessageId}, UnreadCount={unreadCount} ({titleAndId})",
                    updateChatReadInbox.LastReadInboxMessageId, updateChatReadInbox.UnreadCount,
                    GetChatTitleAndId(updateChatReadInbox.ChatId, chatsDict));
                break;
            }

            case TdApi.Update.UpdateChatReadOutbox updateChatReadOutbox:
            {
                logger.Information(
                    "Chat read outbox: LastMessageId={lastMessageId}  ({titleAndId})",
                    updateChatReadOutbox.LastReadOutboxMessageId,
                    GetChatTitleAndId(updateChatReadOutbox.ChatId, chatsDict));
                break;
            }

            case TdApi.Update.UpdateChatAction updateChatAction:
            {
                var chatAction = updateChatAction.Action.GetChatActionString();
                var senderId = updateChatAction.SenderId switch
                {
                    TdApi.MessageSender.MessageSenderChat senderChat => senderChat.ChatId,
                    TdApi.MessageSender.MessageSenderUser senderUser => senderUser.UserId,
                };
                
                logger.Information(
                    "Chat action: {senderId} {chatAction} ({titleAndId})",
                    senderId, chatAction,
                    GetChatTitleAndId(updateChatAction.ChatId, chatsDict));
                break;
            }
            
            case TdApi.Update.UpdateUserStatus updateUserStatus:
            {
                var status = updateUserStatus.Status switch
                {
                    TdApi.UserStatus.UserStatusOnline => "online",
                    _ => "offline"
                };
                
                logger.Information("User status: {User} went {status}", updateUserStatus.UserId, status);
                break;
            }

            case TdApi.Update.UpdateSupergroup:
            case TdApi.Update.UpdateSupergroupFullInfo:
            case TdApi.Update.UpdateBasicGroup:
            case TdApi.Update.UpdateDeleteMessages:
            case TdApi.Update.UpdateUserFullInfo:
            case TdApi.Update.UpdateMessageContent:
            case TdApi.Update.UpdateMessageInteractionInfo:
            case TdApi.Update.UpdateMessageEdited:
            case TdApi.Update.UpdateChatMessageSender:
            case TdApi.Update.UpdateChatHasScheduledMessages:
            case TdApi.Update.UpdateChatUnreadReactionCount:
            // not ignore maybe
            case TdApi.Update.UpdateDefaultReactionType:
            case TdApi.Update.UpdateAnimationSearchParameters:
            case TdApi.Update.UpdateAccentColors:
            case TdApi.Update.UpdateAttachmentMenuBots:
            case TdApi.Update.UpdateSelectedBackground:
            case TdApi.Update.UpdateFileDownloads:
            case TdApi.Update.UpdateDiceEmojis:
            case TdApi.Update.UpdateActiveEmojiReactions:
            case TdApi.Update.UpdateChatThemes:
            case TdApi.Update.UpdateHavePendingNotifications:
            case TdApi.Update.UpdateConnectionState:
            case TdApi.Update.UpdateChatAvailableReactions:
            case TdApi.Update.UpdateStoryStealthMode:
            case TdApi.Update.UpdateChatIsTranslatable:
            case TdApi.Update.UpdateChatMessageAutoDeleteTime:
                //ignore
                break;

            default:
                logger.Information(update.DataType);
                break;
        }
    }

    private static string GetChatTitleAndId(long id, IReadOnlyDictionary<long, Chat> chatsDict)
    {
        var title = chatsDict.TryGetValue(id, out var chat) ? chat.Title : string.Empty;
        var idStr = $"[{id}]";
        if (string.IsNullOrEmpty(title))
            return idStr;

        return title + ' ' + idStr;
    }
}

public static class TgCliExtensions
{
    public static string GetContentString(this TdApi.MessageContent messageContent)
    {
        var contentStr = messageContent switch
        {
            TdApi.MessageContent.MessageText mt => mt.Text.Text,
            TdApi.MessageContent.MessagePhoto => "Photo",
            TdApi.MessageContent.MessageAudio => "Audio",
            TdApi.MessageContent.MessageVideo => "Video",
            TdApi.MessageContent.MessageVoiceNote => "Voice message",
            TdApi.MessageContent.MessageVideoNote => "Video message",
            TdApi.MessageContent.MessageDocument => "Document",
            TdApi.MessageContent.MessageSticker => "Sticker",
            TdApi.MessageContent.MessageAnimatedEmoji e => e.Emoji,
            null => "<null>",
            _ => messageContent.DataType
        };

        var isMessageEmpty = string.IsNullOrEmpty(contentStr);
        contentStr = Utils.RemoveNonUtf16Characters(contentStr);

        if (string.IsNullOrEmpty(contentStr))
            contentStr = isMessageEmpty ? "<empty>" : "<non utf-16>";

        return contentStr;
    }

    public static string GetChatActionString(this TdApi.ChatAction chatAction)
    {
        return chatAction switch
        {
            TdApi.ChatAction.ChatActionChoosingSticker => "Choosing sticker...",
            TdApi.ChatAction.ChatActionRecordingVideoNote => "Recording video message...",
            TdApi.ChatAction.ChatActionRecordingVoiceNote => "Recording voice message...",
            TdApi.ChatAction.ChatActionTyping => "Typing...",
            TdApi.ChatAction.ChatActionUploadingDocument => "Uploading document...",
            TdApi.ChatAction.ChatActionUploadingPhoto => "Uploading photo...",
            TdApi.ChatAction.ChatActionUploadingVideo => "Uploading video...",
            TdApi.ChatAction.ChatActionUploadingVideoNote => "Uploading video message...",
            TdApi.ChatAction.ChatActionUploadingVoiceNote => "Uploading voice message...",
            _ => null
        };
    }
}