using System.Collections.Generic;
using Intersect.Framework.Core.Localization;

namespace Intersect.Server.Web.Types.Localization;

public record TranslationUpsertResponseBody(
    string SourceHash,
    TranslationStatus Status,
    IReadOnlyList<int> MissingArguments
);
