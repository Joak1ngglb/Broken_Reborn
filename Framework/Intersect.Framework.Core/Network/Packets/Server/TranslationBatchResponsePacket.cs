using System;
using MessagePack;
using Intersect.Network.Packets;

namespace Intersect.Network.Packets.Server;

[MessagePackObject]
public class TranslationBatchResponsePacket : IntersectPacket
{
    public TranslationBatchResponsePacket()
    {
    }

    public TranslationBatchResponsePacket(
        string targetLang,
        string? sourceLang,
        string scope,
        TranslationBatchEntry[] entries
    )
    {
        TargetLang = targetLang;
        SourceLang = sourceLang;
        Scope = scope;
        Entries = entries;
    }

    [Key(0)]
    public string TargetLang { get; set; } = string.Empty;

    [Key(1)]
    public string? SourceLang { get; set; }

    [Key(2)]
    public string Scope { get; set; } = string.Empty;

    [Key(3)]
    public TranslationBatchEntry[] Entries { get; set; } = Array.Empty<TranslationBatchEntry>();
}
