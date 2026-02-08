using Intersect.Server.General;

namespace Intersect.Server.Database;
public partial class DbInterface
{
    private static void LoadRewards()
    {
        Codes.LoadRewards();
    }
}
