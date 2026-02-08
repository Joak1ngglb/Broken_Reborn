using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.GameObjects;
using Intersect.Server.Entities;
using Intersect.Server.Localization;
using Intersect.Server.Networking;
using Newtonsoft.Json;

namespace Intersect.Server.General
{
    public static class Codes
    {
        private const string DefaultPath = @"resources/rewards.json";
        private static List<RewardCode> Rewards { get; set; }

        public static void LoadRewards()
        {
            if (!File.Exists(DefaultPath))
            {
                Rewards = new List<RewardCode>()
                {
                    new RewardCode()
                    {
                        Active = true,
                        Code = "EXAMPLECODE",
                        ExpireDate = DateTime.Now.AddMonths(1),
                        Items = new List<RewardItem>()
                        {
                            new RewardItem()
                            {
                                Name = "Health Potion",
                                Quantity = 5
                            },
                            new RewardItem()
                            {
                                Name = "Mana Potion",
                                Quantity = 5
                            }
                        },
                        Limit = null
                    }
                };
                SaveRewards();
            }

            Rewards = JsonConvert.DeserializeObject<List<RewardCode>>(File.ReadAllText(DefaultPath));
        }

        public static void SaveRewards()
        {
            File.WriteAllText(DefaultPath, JsonConvert.SerializeObject(Rewards));
        }

        public static void TryRedeemCode(Player player, string code)
        {
            var reward = Rewards.FirstOrDefault(x => x.Code.ToLower() == code.ToLower() &&
                                                     x.Active &&
                                                     (!x.ExpireDate.HasValue || DateTime.Now < x.ExpireDate.Value));

            if (reward == null)
            {
                PacketSender.SendChatMsg(player, Strings.Rewards.codenotfound.ToString(code.ToUpper()), Enums.ChatMessageType.Inventory);
                return;
            }

            if (reward.LastUsages.Contains(player.Name.ToLower()))
            {
                PacketSender.SendChatMsg(player, Strings.Rewards.alreadyredeemed, Enums.ChatMessageType.Inventory);
                return;
            }

            if (reward.Limit.HasValue && reward.Limit.Value <= reward.LastUsages.Count)
            {
                PacketSender.SendChatMsg(player, Strings.Rewards.rewardlimit, Enums.ChatMessageType.Inventory);
                return;
            }

            var items = reward.Items.Select(x => new
            {
                Item = ItemDescriptor.ItemPairs.FirstOrDefault(y => y.Value.ToLower() == x.Name.ToLower()),
                Quantity = x.Quantity
            }).ToList();

            if (items.Any(x => !player.CanGiveItem(x.Item.Key, x.Quantity)))
            {
                PacketSender.SendChatMsg(player, Strings.Rewards.inventoryfull, Enums.ChatMessageType.Inventory);
                return;
            }

            items.ForEach(x => {
                player.TryGiveItem(x.Item.Key, x.Quantity);
                PacketSender.SendChatMsg(player, Strings.Rewards.received.ToString(x.Quantity, x.Item.Value), Enums.ChatMessageType.Inventory);
            });

            PacketSender.SendInventory(player);
            PacketSender.SendChatMsg(player, Strings.Rewards.redeem, Enums.ChatMessageType.Inventory);
            reward.LastUsages.Add(player.Name.ToLower());
            SaveRewards();
        }
    }
}
