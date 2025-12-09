using System;
using System.Collections.Generic;
using System.Linq;

namespace Intersect.Client.Maps;

public static class MapMarkerManager
{
    private static readonly List<MapMarker> Markers = new();

    public static MapMarker AddMarker(Guid mapId, string label, int x, int y)
    {
        var sanitizedLabel = string.IsNullOrWhiteSpace(label) ? string.Empty : label.Trim();
        var marker = new MapMarker(Guid.NewGuid(), mapId, sanitizedLabel, x, y, DateTime.UtcNow);
        Markers.Add(marker);
        return marker;
    }

    public static int CountForMap(Guid mapId)
    {
        return Markers.Count(marker => marker.MapId == mapId);
    }

    public static IReadOnlyList<MapMarker> GetMarkers(Guid mapId)
    {
        return Markers.Where(marker => marker.MapId == mapId).ToList();
    }

    public static bool RemoveMarker(Guid markerId)
    {
        return Markers.RemoveAll(marker => marker.Id == markerId) > 0;
    }
}
