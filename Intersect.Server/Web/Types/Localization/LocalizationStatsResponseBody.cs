using System.Collections.Generic;

namespace Intersect.Server.Web.Types.Localization;

public sealed record LocalizationStatsResponseBody(
    int WindowMinutes,
    long TotalRequests,
    long ValidRequests,
    long EmptyRequests,
    long UnknownEntityTypeRequests,
    long RepositoryMissingSourceCount,
    long RepositoryMissingCurrentHashTranslationCount,
    IReadOnlyDictionary<string, long> MissesByEntityTypeAndField
);
