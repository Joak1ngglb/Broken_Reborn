using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Intersect.Localization;
using Newtonsoft.Json;

namespace Intersect.Server.Localization;
public static partial class Strings
{
    public sealed partial class ChatNamespace
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly LocalizedString redeemcodecmd = @"/redeem";
    }

    public sealed partial class CommandOutputNamespace
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public readonly LocalizedString rewardsreloaded = @"Rewards reloaded.";
    }

    public sealed partial class RewardsNamespace : LocaleNamespace
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly LocalizedString codenotfound = @"No active reward with code {0}.";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly LocalizedString alreadyredeemed = @"You already redeemed this code.";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly LocalizedString redeem = @"Congratulations! You successfully redeemed this code.";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly LocalizedString inventoryfull = @"Not enough space in inventory.";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly LocalizedString rewardlimit = @"This code reached the use limit.";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly LocalizedString received = @"You received {0}x {1}(s).";
    }

    public sealed partial class PlayerShopsNamespace : LocaleNamespace
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly LocalizedString OutOfRange = @"You must stand next to the shop to interact.";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly LocalizedString AlreadyHaveActive = @"❌ You already have an active shop.";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly LocalizedString CannotOpenHere = @"❌ You cannot open a shop here.";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly LocalizedString AddAtLeastOneItem = @"Add at least one item to the shop.";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly LocalizedString DefaultName = @"Shop";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly LocalizedString InvalidSelectedSlot = @"The selected slot is invalid.";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly LocalizedString SlotEmpty = @"Slot {00} is empty.";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly LocalizedString PriceMustBeGreaterThanZero = @"Price must be greater than 0.";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly LocalizedString InvalidListingQuantity = @"Invalid quantity for listing.";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly LocalizedString NonStackableSellOne = @"Non-stackable items must be sold one by one.";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly LocalizedString NotEnoughUnitsInSlot = @"You do not have enough units in slot {00}.";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly LocalizedString NoConfiguredItems = @"The shop has no configured items.";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly LocalizedString ShopActivated = @"🛒 Your shop is now active.";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly LocalizedString ShopCreateFailed = @"❌ The shop could not be created.";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly LocalizedString InvalidQuantity = @"Invalid quantity.";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly LocalizedString WorldCurrencyNotConfigured = @"The world currency is not configured.";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly LocalizedString ItemAlreadySold = @"That item has already been sold.";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly LocalizedString NotEnoughStock = @"There is not enough stock available.";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly LocalizedString NotEnoughGold = @"You do not have enough gold.";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly LocalizedString InventoryFull = @"Your inventory is full.";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly LocalizedString CurrencyWithdrawFailed = @"Could not withdraw currency.";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly LocalizedString PurchaseFailed = @"Could not complete purchase.";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly LocalizedString FallbackItemName = @"item";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly LocalizedString PurchaseSuccess = @"🛍️ You bought {00}x {01}.";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly LocalizedString ShopUnavailable = @"The shop is no longer available.";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly LocalizedString ShopAlreadyClosed = @"This shop has already been closed.";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly LocalizedString ShopExpired = @"This shop has expired.";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly LocalizedString NoActiveShop = @"You do not have an active shop.";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly LocalizedString ActiveShopNotFoundUseClose = @"Could not find the active shop. Use /pshop close to close and claim if needed.";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly LocalizedString WarpedToActiveShop = @"You were moved to your active shop.";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly LocalizedString CloseNowFailed = @"The shop could not be closed right now.";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly LocalizedString GoldDeliveredDetail = @"💰 {00} gold delivered";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly LocalizedString ItemsReturnedDetail = @"📦 {00} items returned";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly LocalizedString NoPendingSales = @"no pending sales";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly LocalizedString ShopClosedSummary = @"📋 Your shop ""{00}"" was closed: {01}.";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly LocalizedString CommandUsage = @"Usage: /pshop go (go to shop) or /pshop close (close and claim).";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly LocalizedString GoldLiquidatedDetail = @"💰 {00} gold liquidated";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly LocalizedString NoPendingBalance = @"no pending balance";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly LocalizedString ExpiredLiquidationApplied = @"📩 Liquidation applied for your expired shop {00}: {01}.";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly LocalizedString ActiveShopStillOpen = @"🛒 Your shop ""{00}"" is still open at ({01}, {02}). Options: go to shop or close and claim.";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly LocalizedString ErrorMissingInventorySlot = @"Missing inventory slot for one of the listed items.";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly LocalizedString ErrorQuantityMustBeGreaterThanZeroForSlot = @"Quantity for slot {00} must be greater than 0.";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly LocalizedString ErrorSlotItemMismatch = @"Item in slot {00} does not match the listing.";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly LocalizedString ErrorReserveItemsFailedForSlot = @"Could not reserve items from slot {00}.";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly LocalizedString ErrorLocationOccupied = @"There is already a shop at this location.";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public LocalizedString ErrorUniquenessViolation = @"You already have an active shop or the location is occupied by another active shop.";
    }


  
}
