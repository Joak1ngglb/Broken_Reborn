using System;
using System.Collections.Generic;
using System.Linq;
using MessagePack;

namespace Intersect.Admin.Actions;

[MessagePackObject]
public partial class BroadcastMailAction : AdminAction
{
    public BroadcastMailAction()
    {
    }

    public const int MaxAttachments = 5;

    public BroadcastMailAction(
        string title,
        string message,
        IEnumerable<BroadcastMailAttachment> attachments,
        bool onlineOnly
    )
    {
        Title = title;
        Message = message;
        OnlineOnly = onlineOnly;
        Attachments =
            attachments?.Where(attachment => attachment != null)
                .Select(attachment => new BroadcastMailAttachment(attachment.ItemId, attachment.Quantity))
                .ToList() ?? new List<BroadcastMailAttachment>();
    }

    [Key(1)]
    public override Enums.AdminAction Action { get; } = Enums.AdminAction.BroadcastMail;

    [Key(2)]
    public string Title { get; set; } = string.Empty;

    [Key(3)]
    public string Message { get; set; } = string.Empty;

    [Key(4)]
    public bool OnlineOnly { get; set; }

    [Key(5)]
    public List<BroadcastMailAttachment> Attachments { get; set; } = new();
}

[MessagePackObject]
public sealed class BroadcastMailAttachment
{
    public BroadcastMailAttachment()
    {
    }

    public BroadcastMailAttachment(Guid itemId, int quantity)
    {
        ItemId = itemId;
        Quantity = quantity;
    }

    [Key(0)]
    public Guid ItemId { get; set; } = Guid.Empty;

    [Key(1)]
    public int Quantity { get; set; } = 1;
}
