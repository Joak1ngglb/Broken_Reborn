using Intersect.Network.Packets.Editor;
using Intersect.Network.Packets.Localization;
using MessagePack;

namespace Intersect.Network.Packets;


//We check for this class to see if an account must be an admin in order for the packet to process.
[MessagePackObject]
[Union(0, typeof(AddTilesetsPacket))]
[Union(1, typeof(CreateGameObjectPacket))]
[Union(2, typeof(CreateMapPacket))]
[Union(3, typeof(DeleteGameObjectPacket))]
[Union(4, typeof(EnterMapPacket))]
[Union(5, typeof(LinkMapPacket))]
[Union(6, typeof(MapListUpdatePacket))]
[Union(7, typeof(MapUpdatePacket))]
[Union(8, typeof(NeedMapPacket))]
[Union(9, typeof(RequestGridPacket))]
[Union(10, typeof(RequestOpenEditorPacket))]
[Union(11, typeof(SaveGameObjectPacket))]
[Union(12, typeof(SaveTimeDataPacket))]
[Union(13, typeof(UnlinkMapPacket))]
[Union(14, typeof(TranslationUpsertPacket))]
[Union(15, typeof(TranslationBatchUpsertPacket))]
[Union(16, typeof(TranslationPendingRequestPacket))]
[Union(17, typeof(TranslationPendingResponsePacket))]

public abstract partial class EditorPacket : IntersectPacket
{

}
