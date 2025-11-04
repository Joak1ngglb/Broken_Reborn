using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Admin.Actions;
using Intersect.Core;
using Intersect.Enums;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Server.Database;
using Intersect.Server.Database.Logging.Entities;
using Intersect.Server.Database.PlayerData;
using Intersect.Server.Database.PlayerData.Security;
using Intersect.Server.Core.Database.PlayerData.Players;
using Intersect.Server.Entities;
using Intersect.Server.Localization;
using Intersect.Server.Networking;
using Intersect.Server.Maps;
using Microsoft.EntityFrameworkCore;

namespace Intersect.Server.Admin.Actions
{
    public static partial class ActionProcessing
    {
        //BanAction
        public static void ProcessAction(Player player, BanAction action)
        {
            var target = Player.Find(action.Name);
            if (!player.Power.Ban) // Ban Permission Check.
            {
                PacketSender.SendChatMsg(player, Strings.Account.NotAllowed, ChatMessageType.Admin, Color.Red);

                return;
            }

            if (target == null) // If the target can't be found.
            {
                PacketSender.SendChatMsg(
                    player, Strings.Player.PlayerNotFound.ToString(action.Name), ChatMessageType.Admin, Color.Red
                );

                return;
            }

            var targetUsername = User.FindById(target.UserId);
            if (player.Power.CompareTo(target.Power) < 1) // Authority Comparison.
            {
                // Inform to whoever performed the action that they are
                // not allowed to do this due to the lack of authority over their target.
                PacketSender.SendChatMsg(
                    player, Strings.Account.NotAllowed.ToString(target.Name), ChatMessageType.Admin, Color.Red
                );

                // Log the failed ban attempt.
                UserActivityHistory.LogActivity(
                    target.UserId, target.Id, target.Client?.Ip,
                    UserActivityHistory.PeerType.Client, UserActivityHistory.UserAction.DisconnectBanFail,
                    $"{target.User?.Name},{target.Name}");
            }
            else if (Ban.Find(targetUsername) != null) // If the target is already banned.
            {
                PacketSender.SendChatMsg(
                    player, Strings.Account.AlreadyBanned.ToString(target.Name), ChatMessageType.Admin, Color.Red
                );
            }

            // If target is online, not yet banned and the banner has the authority to ban.
            else
            {
                // If BanIp is false, Client is null, or GetIp() returns null: resolve to string.Empty (no IP).
                var ip = (action.BanIp ? target.Client?.Ip : default) ?? string.Empty;

                // Ban the targeted user.
                Ban.Add(targetUsername, action.DurationDays, action.Reason ?? string.Empty, player.Name, ip);

                // Log the successful ban attempt.
                UserActivityHistory.LogActivity(
                    target.UserId, target.Id, target.Client?.Ip,
                    UserActivityHistory.PeerType.Client, UserActivityHistory.UserAction.DisconnectBan,
                    $"{target.User?.Name},{target.Name}"
                );

                // Disconnect the banned user.
                target.Client?.Disconnect();

                // Sends a global chat message to every user online about the banned player.
                PacketSender.SendGlobalMsg(Strings.Account.Banned.ToString(target.Name));
            }
        }

        //KickAction
        public static void ProcessAction(Player player, KickAction action)
        {
            var target = Player.FindOnline(action.Name);
            if (!player.Power.Kick) // Kick permission check.
            {
                PacketSender.SendChatMsg(player, Strings.Account.NotAllowed, ChatMessageType.Admin, Color.Red);

                return;
            }

            if (target == null) // If the target is offline.
            {
                PacketSender.SendChatMsg(player, Strings.Player.Offline, ChatMessageType.Admin, Color.Red);
            }
            else if (player.Power.CompareTo(target.Power) < 1) // Authority Comparison.
            {
                // Inform to whoever performed the action that they are
                // not allowed to do this due to the lack of authority over their target.
                PacketSender.SendChatMsg(
                    player, Strings.Account.NotAllowed.ToString(target.Name), ChatMessageType.Admin, Color.Red
                );

                // Log the failed kick attempt.
                UserActivityHistory.LogActivity(
                    target.UserId, target.Id, target.Client?.Ip,
                    UserActivityHistory.PeerType.Client, UserActivityHistory.UserAction.DisconnectKickFail,
                    $"{target.User?.Name},{target.Name}");
            }
            else
            {
                // Log the successful kick attempt.
                UserActivityHistory.LogActivity(
                    target.UserId, target.Id, target.Client?.Ip,
                    UserActivityHistory.PeerType.Client, UserActivityHistory.UserAction.DisconnectKick,
                    $"{target.User?.Name},{target.Name}");

                // Sends a global chat message to every user online about the kicked player.
                PacketSender.SendGlobalMsg(Strings.Player.Kicked.ToString(target.Name, player.Name));

                // Kick the target.
                target.Client?.Disconnect();
            }
        }

        //KillAction
        public static void ProcessAction(Player player, KillAction action)
        {
            var target = Player.FindOnline(action.Name);
            if (!player.Power.IsAdmin) // Admin permission check.
            {
                PacketSender.SendChatMsg(player, Strings.Account.NotAllowed, ChatMessageType.Admin, Color.Red);

                return;
            }

            if (target == null) // If the target is offline.
            {
                PacketSender.SendChatMsg(player, Strings.Player.Offline, ChatMessageType.Admin, Color.Red);
            }
            else if (player.Power.CompareTo(target.Power) < 1) // Authority Comparison.
            {
                // Inform to whoever performed the action that they are
                // not allowed to do this due to the lack of authority over their target.
                PacketSender.SendChatMsg(
                    player, Strings.Account.NotAllowed.ToString(target.Name), ChatMessageType.Admin, Color.Red
                );

                // Log the failed kill attempt.
                UserActivityHistory.LogActivity(
                    target.UserId, target.Id, target.Client?.Ip,
                    UserActivityHistory.PeerType.Client, UserActivityHistory.UserAction.KillFail,
                    $"{target.User?.Name},{target.Name}");
            }
            else
            {
                // Log the successful kill attempt.
                UserActivityHistory.LogActivity(
                    target.UserId, target.Id, target.Client?.Ip,
                    UserActivityHistory.PeerType.Client, UserActivityHistory.UserAction.Kill,
                    $"{target.User?.Name},{target.Name}");

                // Sends a global chat message to every user online about the killed player.
                PacketSender.SendGlobalMsg(Strings.Player.Killed.ToString(target.Name, player.Name));

                lock (target.EntityLock)
                {
                    // Kill the target.
                    target.Die();
                }
            }
        }

        //MuteAction
        public static void ProcessAction(Player player, MuteAction action)
        {
            var target = Player.Find(action.Name);
            if (!player.Power.Mute) // Mute permission check.
            {
                PacketSender.SendChatMsg(player, Strings.Account.NotAllowed, ChatMessageType.Admin, Color.Red);

                return;
            }

            if (target == null) // If the target can't be found.
            {
                PacketSender.SendChatMsg(
                    player, Strings.Player.PlayerNotFound.ToString(action.Name), ChatMessageType.Admin, Color.Red
                );

                return;
            }

            var targetUsername = User.FindById(target.UserId);
            if (player.Power.CompareTo(target.Power) < 1) // Authority Comparison.
            {
                // Inform to whoever performed the action that they are
                // not allowed to do this due to the lack of authority over their target.
                PacketSender.SendChatMsg(
                    player, Strings.Account.NotAllowed.ToString(target.Name), ChatMessageType.Admin, Color.Red
                );

                // Log the failed mute attempt.
                UserActivityHistory.LogActivity(
                    target.UserId, target.Id, target.Client?.Ip,
                    UserActivityHistory.PeerType.Client, UserActivityHistory.UserAction.MuteFail,
                    $"{target.User?.Name},{target.Name}");
            }
            else if (Mute.Find(targetUsername) != null) // If the target is already muted.
            {
                PacketSender.SendChatMsg(
                    player, Strings.Account.AlreadyMuted.ToString(target.Name), ChatMessageType.Admin, Color.Red
                );
            }

            // If target is online, not yet muted and the action performer has the authority to mute.
            else
            {
                // If BanIp is false, Client is null, or GetIp() returns null: resolve to string.Empty (no IP).
                var ip = (action.BanIp ? target.Client?.Ip : default) ?? string.Empty;

                // Mute the targeted user.
                Mute.Add(targetUsername, action.DurationDays, action.Reason ?? string.Empty, player.Name, ip);

                // Log the successful mute attempt.
                UserActivityHistory.LogActivity(
                    target.UserId, target.Id, target.Client?.Ip,
                    UserActivityHistory.PeerType.Client, UserActivityHistory.UserAction.Mute,
                    $"{target.User?.Name},{target.Name}"
                );

                // Sends a global chat message to every user online about the muted player.
                PacketSender.SendGlobalMsg(Strings.Account.Muted.ToString(target.Name));
            }
        }

        //SetAccessAction
        public static void ProcessAction(Player player, SetAccessAction action)
        {
            var target = Player.FindOnline(action.Name);
            if (target == null || target.Client == null)
            {
                PacketSender.SendChatMsg(player, Strings.Player.Offline, ChatMessageType.Admin, Color.Red);

                return;
            }

            if (action.Name.Trim().ToLower() != player.Name.Trim().ToLower())
            {
                if (player.Power.IsAdmin)
                {
                    var power = UserRights.None;
                    if (action.Power == "Admin")
                    {
                        power = UserRights.Admin;
                    }
                    else if (action.Power == "Moderator")
                    {
                        power = UserRights.Moderation;
                    }

                    var targetClient = target.Client;
                    targetClient.Power = power;
                    if (targetClient.Power.IsAdmin)
                    {
                        PacketSender.SendGlobalMsg(Strings.Player.Admin.ToString(target.Name));
                    }
                    else if (targetClient.Power.IsModerator)
                    {
                        PacketSender.SendGlobalMsg(Strings.Player.Moderator.ToString(target.Name));
                    }
                    else
                    {
                        PacketSender.SendGlobalMsg(Strings.Player.Deadmin.ToString(target.Name));
                    }
                }
                else
                {
                    PacketSender.SendChatMsg(player, Strings.Player.AdminSetPower, ChatMessageType.Admin);
                }
            }
            else
            {
                PacketSender.SendChatMsg(player, Strings.Player.CannotAlterOwnPower, ChatMessageType.Admin);
            }
        }

        //SetFaceAction
        public static void ProcessAction(Player player, SetFaceAction action)
        {
            var target = Player.FindOnline(action.Name);
            if (target != null)
            {
                target.Face = action.Face;
                PacketSender.SendEntityDataToProximity(target);
            }
            else
            {
                PacketSender.SendChatMsg(player, Strings.Player.Offline, ChatMessageType.Admin, Color.Red);
            }
        }

        //SetSpriteAction
        public static void ProcessAction(Player player, SetSpriteAction action)
        {
            var target = Player.FindOnline(action.Name);
            if (target != null)
            {
                target.Sprite = action.Sprite;
                PacketSender.SendEntityDataToProximity(target);
            }
            else
            {
                PacketSender.SendChatMsg(player, Strings.Player.Offline, ChatMessageType.Admin, Color.Red);
            }
        }

        //UnbanAction
        public static void ProcessAction(Player player, UnbanAction action)
        {
            var unbannedPlayer = Player.Find(action.Name);
            if (!player.Power.Ban) // Should have ban authority permission in order to unban.
            {
                PacketSender.SendChatMsg(player, Strings.Account.NotAllowed, ChatMessageType.Admin, Color.Red);

                return;
            }

            if (unbannedPlayer == null) // Does the target exists?
            {
                PacketSender.SendChatMsg(
                    player, Strings.Player.PlayerNotFound.ToString(action.Name), ChatMessageType.Admin, Color.Red
                );

                return;
            }

            var targetUsername = User.FindById(unbannedPlayer.UserId);
            if (Ban.Find(targetUsername) == null) // If the target is not banned.
            {
                PacketSender.SendChatMsg(
                    player, Strings.Account.UnbanFail.ToString(action.Name), ChatMessageType.Admin, Color.Red
                );
            }
            else
            {
                Ban.Remove(targetUsername); // Unban the target.
                PacketSender.SendChatMsg(
                    player, Strings.Account.UnbanSuccess.ToString(action.Name), ChatMessageType.Admin, Color.Green
                );
            }
        }

        //UnmuteAction
        public static void ProcessAction(Player player, UnmuteAction action)
        {
            var unmuteUser = Player.Find(action.Name);
            if (!player.Power.Mute) // Should have mute authority permission in order to unmute.
            {
                PacketSender.SendChatMsg(player, Strings.Account.NotAllowed, ChatMessageType.Admin, Color.Red);

                return;
            }

            if (unmuteUser == null) // Does the target exists?
            {
                PacketSender.SendChatMsg(
                    player, Strings.Player.PlayerNotFound.ToString(action.Name), ChatMessageType.Admin, Color.Red
                );

                return;
            }

            var targetUsername = User.FindById(unmuteUser.UserId);
            if (Mute.Find(targetUsername) == null) // If the target is not muted.
            {
                PacketSender.SendChatMsg(
                    player, Strings.Account.UnmuteFail.ToString(action.Name), ChatMessageType.Admin, Color.Red
                );
            }
            else
            {
                Mute.Remove(targetUsername); // Unmute the target.
                PacketSender.SendChatMsg(
                    player, Strings.Account.UnmuteSuccess.ToString(action.Name), ChatMessageType.Admin, Color.Green
                );
            }
        }

        //WarpMeToAction
        public static void ProcessAction(Player player, WarpMeToAction action)
        {
            var target = Player.FindOnline(action.Name);
            if (target == player) // Disallows to target this action on whoever performs it.
            {
                PacketSender.SendChatMsg(player, Strings.Player.CannotWarpToYourself, ChatMessageType.Admin, Color.Red);

                return;
            }

            if (target != null)
            {
                var forceInstanceChange = target.InstanceType != player.InstanceType;
                player.AdminWarp(target.MapId, (byte)target.X, (byte)target.Y, target.MapInstanceId, target.InstanceType, forceInstanceChange);
                PacketSender.SendChatMsg(player, Strings.Player.WarpedTo.ToString(target.Name), ChatMessageType.Admin);
                PacketSender.SendChatMsg(
                    target, Strings.Player.WarpedToYou.ToString(player.Name), ChatMessageType.Notice
                );
            }
            else
            {
                PacketSender.SendChatMsg(player, Strings.Player.Offline, ChatMessageType.Admin, Color.Red);
            }
        }

        //WarpToLocationAction
        public static void ProcessAction(Player player, WarpToLocationAction action)
        {
            player.Warp(action.MapId, action.X, action.Y, 0, true);
        }

        //WarpToMapAction
        public static void ProcessAction(Player player, WarpToMapAction action)
        {
            player.Warp(action.MapId, player.X, player.Y);
        }

        //WarpToMeAction
        public static void ProcessAction(Player player, WarpToMeAction action)
        {
            var target = Player.FindOnline(action.Name);
            if (target == null)
            {
                PacketSender.SendChatMsg(player, Strings.Player.Offline, ChatMessageType.Admin, Color.Red);

                return;
            }

            if (target == player) // Disallows to target this action on whoever performs it.
            {
                PacketSender.SendChatMsg(player, Strings.Player.CannotWarpToYourself, ChatMessageType.Admin, Color.Red);

                return;
            }

            if (player.Power.CompareTo(target.Power) < 1) // Authority Comparison.
            {
                // Inform to whoever performed the action that they are
                // not allowed to do this due to the lack of authority over their target.
                PacketSender.SendChatMsg(
                    player, Strings.Account.NotAllowed.ToString(target.Name), ChatMessageType.Admin, Color.Red
                );
            }
            else
            {
                var forceInstanceChange = target.InstanceType != player.InstanceType;

                target.AdminWarp(player.MapId, (byte)player.X, (byte)player.Y, player.MapInstanceId, player.InstanceType, forceInstanceChange);
                PacketSender.SendChatMsg(
                    player, Strings.Player.HasWarpedTo.ToString(target.Name), ChatMessageType.Admin, player.Name
                );

                PacketSender.SendChatMsg(
                    target, Strings.Player.BeenWarpedTo.ToString(player.Name), ChatMessageType.Notice, player.Name
                );
            }
        }

        //GiveItem
        public static void ProcessAction(Player player, GiveItemAction action)
        {
            if (!player.Power.IsAdmin)
            {
                PacketSender.SendChatMsg(player, Strings.Account.NotAllowed, ChatMessageType.Admin, Color.Red);

                return;
            }

            if (action.Quantity <= 0)
            {
                PacketSender.SendChatMsg(player, Strings.Player.InvalidQuantity, ChatMessageType.Admin, Color.Red);

                return;
            }

            if (!ItemDescriptor.TryGet(action.ItemId, out var descriptor))
            {
                PacketSender.SendChatMsg(player, Strings.Player.InvalidItem, ChatMessageType.Admin, Color.Red);

                return;
            }

            var target = Player.FindOnline(action.Name);
            if (target == null)
            {
                PacketSender.SendChatMsg(player, Strings.Player.Offline, ChatMessageType.Admin, Color.Red);

                return;
            }

            if (player.Power.CompareTo(target.Power) < 1)
            {
                PacketSender.SendChatMsg(
                    player, Strings.Account.NotAllowed.ToString(target.Name), ChatMessageType.Admin, Color.Red
                );

                return;
            }

            var descriptorName = descriptor?.Name ?? action.ItemId.ToString();
            if (!target.TryGiveItem(action.ItemId, action.Quantity, ItemHandling.Normal, action.AllowBankOverflow))
            {
                PacketSender.SendChatMsg(
                    player,
                    Strings.Player.ItemGiveFailed.ToString(action.Quantity, descriptorName, target.Name),
                    ChatMessageType.Admin,
                    Color.Red
                );

                return;
        }

        PacketSender.SendChatMsg(
            player,
            Strings.Player.ItemGiveSuccess.ToString(action.Quantity, descriptorName, target.Name),
            ChatMessageType.Admin,
            Color.Green
        );

        PacketSender.SendChatMsg(
            target,
            Strings.Player.ItemReceivedFromAdmin.ToString(player.Name, action.Quantity, descriptorName),
            ChatMessageType.Admin,
            Color.Green
        );
    }

    //BroadcastMail
    public static void ProcessAction(Player player, BroadcastMailAction action)
    {
        if (!player.Power.IsAdmin)
        {
            PacketSender.SendChatMsg(player, Strings.Account.NotAllowed, ChatMessageType.Admin, Color.Red);

            return;
        }

        var subject = action.Title?.Trim() ?? string.Empty;
        var message = action.Message?.Trim() ?? string.Empty;
        var attachments = action.Attachments?.Where(attachment => attachment != null).ToList() ?? new List<BroadcastMailAttachment>();

        if (string.IsNullOrWhiteSpace(subject))
        {
            PacketSender.SendChatMsg(player, Strings.Mails.broadcastmissingtitle, ChatMessageType.Admin, Color.Red);

            return;
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            PacketSender.SendChatMsg(player, Strings.Mails.broadcastmissingmessage, ChatMessageType.Admin, Color.Red);

            return;
        }

        if (attachments.Count == 0)
        {
            PacketSender.SendChatMsg(player, Strings.Mails.broadcastmissingitem, ChatMessageType.Admin, Color.Red);

            return;
        }

        if (attachments.Count > BroadcastMailAction.MaxAttachments)
        {
            PacketSender.SendChatMsg(
                player,
                Strings.Mails.broadcasttoomanyattachments.ToString(BroadcastMailAction.MaxAttachments),
                ChatMessageType.Admin,
                Color.Red
            );

            return;
        }

        foreach (var attachment in attachments)
        {
            if (attachment.ItemId == Guid.Empty || !ItemDescriptor.TryGet(attachment.ItemId, out _))
            {
                PacketSender.SendChatMsg(player, Strings.Mails.broadcastmissingitem, ChatMessageType.Admin, Color.Red);

                return;
            }

            if (attachment.Quantity <= 0)
            {
                PacketSender.SendChatMsg(player, Strings.Mails.broadcastinvalidquantity, ChatMessageType.Admin, Color.Red);

                return;
            }
        }

        var attachmentDefinitions = attachments
            .Select(attachment => (attachment.ItemId, attachment.Quantity))
            .ToList();

        if (attachmentDefinitions.Count == 0)
        {
            PacketSender.SendChatMsg(player, Strings.Mails.broadcastmissingitem, ChatMessageType.Admin, Color.Red);

            return;
        }

        try
        {
            using var context = DbInterface.CreatePlayerContext(readOnly: false);

            var processedRecipients = new HashSet<Guid>();
            var mailEntries = new List<MailBox>();
            var onlineDeliveries = new List<(Player Recipient, MailBox Mail)>();
            var onlinePlayersById = Player.OnlinePlayers.ToDictionary(p => p.Id);

            void QueueMail(Guid recipientId)
            {
                if (!processedRecipients.Add(recipientId))
                {
                    return;
                }

                var mail = new MailBox
                {
                    PlayerId = recipientId,
                    SenderId = player.Id,
                    Title = subject,
                    Message = message,
                    Attachments = attachmentDefinitions
                        .Select(definition => new MailAttachment
                        {
                            ItemId = definition.ItemId,
                            Quantity = definition.Quantity,
                        })
                        .ToList(),
                };

                mailEntries.Add(mail);
                context.Player_MailBox.Add(mail);

                if (onlinePlayersById.TryGetValue(recipientId, out var onlineRecipient))
                {
                    onlineDeliveries.Add((onlineRecipient, mail));
                }
            }

            if (action.OnlineOnly)
            {
                foreach (var onlineRecipient in onlinePlayersById.Values)
                {
                    QueueMail(onlineRecipient.Id);
                }
            }
            else
            {
                foreach (var recipientId in context.Players.AsNoTracking().Select(p => p.Id))
                {
                    QueueMail(recipientId);
                }
            }

            if (mailEntries.Count == 0)
            {
                PacketSender.SendChatMsg(player, Strings.Mails.broadcastnorecipients, ChatMessageType.Admin, Color.Red);

                return;
            }

            context.SaveChanges();

            foreach (var (recipient, mail) in onlineDeliveries)
            {
                mail.SenderPlayer = player;
                recipient.MailBoxs.Add(mail);
                PacketSender.SendChatMsg(recipient, Strings.Mails.newmail, ChatMessageType.Trading, Color.Green);
                PacketSender.SendOpenMailBox(recipient);
            }

            PacketSender.SendChatMsg(player, Strings.Mails.broadcastsent, ChatMessageType.Admin, Color.Green);
        }
        catch (Exception exception)
        {
            ApplicationContext.CurrentContext.Logger.LogError(exception, "Failed to broadcast mail");
            PacketSender.SendChatMsg(player, Strings.Mails.broadcastfailed, ChatMessageType.Admin, Color.Red);
        }
    }

    //SpawnItem
    public static void ProcessAction(Player player, SpawnItemAction action)
    {
        if (!player.Power.IsAdmin)
            {
                PacketSender.SendChatMsg(player, Strings.Account.NotAllowed, ChatMessageType.Admin, Color.Red);

                return;
            }

            if (action.Quantity <= 0)
            {
                PacketSender.SendChatMsg(player, Strings.Player.InvalidQuantity, ChatMessageType.Admin, Color.Red);

                return;
            }

            if (!ItemDescriptor.TryGet(action.ItemId, out var descriptor))
            {
                PacketSender.SendChatMsg(player, Strings.Player.InvalidItem, ChatMessageType.Admin, Color.Red);

                return;
            }

            var target = Player.FindOnline(action.Name);
            if (target == null)
            {
                PacketSender.SendChatMsg(player, Strings.Player.Offline, ChatMessageType.Admin, Color.Red);

                return;
            }

            if (player.Power.CompareTo(target.Power) < 1)
            {
                PacketSender.SendChatMsg(
                    player, Strings.Account.NotAllowed.ToString(target.Name), ChatMessageType.Admin, Color.Red
                );

                return;
            }

            if (!MapController.TryGetInstanceFromMap(target.MapId, target.MapInstanceId, out var mapInstance) ||
                mapInstance == null)
            {
                var descriptorNameMissing = descriptor?.Name ?? action.ItemId.ToString();
                PacketSender.SendChatMsg(
                    player,
                    Strings.Player.ItemSpawnFailed.ToString(action.Quantity, descriptorNameMissing, target.Name),
                    ChatMessageType.Admin,
                    Color.Red
                );

                return;
            }

            var descriptorName = descriptor?.Name ?? action.ItemId.ToString();
            var spawnedItem = new Item(action.ItemId, action.Quantity);
            mapInstance.SpawnItem(
                null,
                target.X,
                target.Y,
                spawnedItem,
                action.Quantity,
                action.ReserveForTarget ? target.Id : Guid.Empty
            );

            PacketSender.SendChatMsg(
                player,
                Strings.Player.ItemSpawnSuccess.ToString(action.Quantity, descriptorName, target.Name),
                ChatMessageType.Admin,
                Color.Green
            );

            PacketSender.SendChatMsg(
                target,
                Strings.Player.ItemSpawnedNearYou.ToString(player.Name, action.Quantity, descriptorName),
                ChatMessageType.Admin,
                Color.Green
            );
        }

        //ReturnToOverworld
        public static void ProcessAction(Player player, ReturnToOverworldAction action)
        {
            var target = Player.FindOnline(action.PlayerName);
            if (target == null)
            {
                PacketSender.SendChatMsg(player, Strings.Player.Offline, Enums.ChatMessageType.Admin);

                return;
            }

            target.WarpToLastOverworldLocation(false);
            PacketSender.SendChatMsg(target, Strings.Player.OverworldReturned.ToString(target.Name), Enums.ChatMessageType.Notice, player.Name);

            if (player == null || target.Name == player.Name)
            {
                return;
            }

            PacketSender.SendChatMsg(player, Strings.Player.OverworldReturnAdmin.ToString(target.Name), Enums.ChatMessageType.Admin, player.Name);
        }
    }
}
