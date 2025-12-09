using System;

namespace Intersect.Client.Maps;

public record MapMarker(
    Guid Id,
    Guid MapId,
    string Label,
    int X,
    int Y,
    DateTime CreatedUtc
);
