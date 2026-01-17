using System.Net;
using Intersect.Framework.Core.Localization;
using Intersect.Server.Database.PlayerData.Security;
using Intersect.Server.Localization;
using Intersect.Server.Web.Http;
using Intersect.Server.Web.Types;
using Intersect.Server.Web.Types.Localization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Intersect.Server.Web.Controllers.Api.V1
{
    [Route("api/v1/localization")]
    [Authorize(Roles = nameof(ApiRoles.UserManage))]
    public sealed partial class LocalizationController : IntersectController
    {
        [HttpPost("translations")]
        [ProducesResponseType(typeof(StatusMessageResponseBody), (int)HttpStatusCode.BadRequest, ContentTypes.Json)]
        [ProducesResponseType(typeof(StatusMessageResponseBody), (int)HttpStatusCode.NotFound, ContentTypes.Json)]
        [ProducesResponseType(typeof(TranslationUpsertResponseBody), (int)HttpStatusCode.OK, ContentTypes.Json)]
        public IActionResult UpsertTranslation([FromBody] TranslationUpsertRequestBody request)
        {
            if (request == null)
            {
                return BadRequest("Request body is required.");
            }

            if (string.IsNullOrWhiteSpace(request.EntityType) ||
                string.IsNullOrWhiteSpace(request.EntityId) ||
                string.IsNullOrWhiteSpace(request.Field) ||
                string.IsNullOrWhiteSpace(request.Lang))
            {
                return BadRequest("EntityType, EntityId, Field, and Lang are required.");
            }

            var currentHash = LocalizationRepository.Default.GetCurrentSourceHash(
                request.EntityType,
                request.EntityId,
                request.Field
            );

            if (string.IsNullOrWhiteSpace(currentHash))
            {
                return NotFound("No localization source found for the provided key.");
            }

            LocalizationRepository.Default.UpsertTranslation(
                request.EntityType,
                request.EntityId,
                request.Field,
                request.Lang,
                request.TranslatedText ?? string.Empty,
                TranslationStatus.Ok,
                currentHash
            );

            return Ok(new TranslationUpsertResponseBody(currentHash));
        }
    }
}
