using Intersect.Server.Core.CommandParsing;
using Intersect.Server.General;
using Intersect.Server.Localization;

namespace Intersect.Server.Core.Commands
{

    internal sealed partial class ReloadRewardsCommand : ServerCommand
    {

        public ReloadRewardsCommand() : base(Strings.Commands.ReloadRewards)
        {
        }

        protected override void HandleValue(ServerContext context, ParserResult result)
        {
            Codes.LoadRewards();
            Console.WriteLine(Strings.Commandoutput.rewardsreloaded);
        }
    }
}
