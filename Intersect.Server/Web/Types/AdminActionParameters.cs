using System;

namespace Intersect.Server.Web.Types;

public partial struct AdminActionParameters
{
    public string Moderator { get; set; }

    public int Duration { get; set; }

    public bool Ip { get; set; }

    public string Reason { get; set; }

    public byte X { get; set; }

    public byte Y { get; set; }

    public Guid MapId { get; set; }

    public Guid ItemId { get; set; }

    public int Quantity { get; set; }

    public bool AllowBankOverflow { get; set; }

    public bool ReserveForTarget { get; set; }
}