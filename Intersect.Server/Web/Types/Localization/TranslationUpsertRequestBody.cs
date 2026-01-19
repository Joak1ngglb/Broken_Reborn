namespace Intersect.Server.Web.Types.Localization;

public record TranslationUpsertRequestBody(
    string EntityType,
    string EntityId,
    string Field,
    string Lang,
    string TranslatedText
);
