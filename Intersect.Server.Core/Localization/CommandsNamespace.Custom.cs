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
    public partial class CommandsNamespace
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly LocaleCommand ReloadRewards = new LocaleCommand
        {
            Name = @"reloadrewards",
            Description = @"Reload all rewards.",
            Help = @"reload all rewards from rewards.json."
        };
    }
}

