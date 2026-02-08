using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Intersect.Localization;
using Newtonsoft.Json;

namespace Intersect.Server.Localization;
public partial class Strings
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

    private sealed partial class RootNamespace
    {
        public readonly RewardsNamespace Rewards = new RewardsNamespace();
    }

    public static RewardsNamespace Rewards => Root.Rewards;
}
