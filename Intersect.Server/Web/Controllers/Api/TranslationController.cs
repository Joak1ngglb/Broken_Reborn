using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Intersect.Core;
using Intersect.Framework.Core.Config;
using Intersect.Server.Web.Http;
using Intersect.Server.Web.Types;
using Intersect.Server.Web.Types.Translation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Intersect.Server.Web.Controllers.Api;

[Route("api/translation")]
public sealed class TranslationController : IntersectController
{
    private const string DefaultEndpoint =
        "https://jlrootsloud-3174sfw-resource.cognitiveservices.azure.com/";
    private const string DefaultDeploymentName = "gpt-4.1-mini";
    private const string DefaultApiVersion = "2024-05-01-preview";
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(30) };

    [HttpPost]
    [AllowAnonymous]
    [Consumes(typeof(TranslationRequestBody), ContentTypes.Json)]
    [ProducesResponseType(typeof(TranslationResponseBody), (int)HttpStatusCode.OK, ContentTypes.Json)]
    [ProducesResponseType(typeof(StatusMessageResponseBody), (int)HttpStatusCode.BadRequest, ContentTypes.Json)]
    [ProducesResponseType(typeof(StatusMessageResponseBody), (int)HttpStatusCode.ServiceUnavailable, ContentTypes.Json)]
    public async Task<IActionResult> Translate([FromBody] TranslationRequestBody request, CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return BadRequest(new StatusMessageResponseBody("Missing translation request body."));
        }

        if (string.IsNullOrWhiteSpace(request.TargetLanguage))
        {
            return BadRequest(new StatusMessageResponseBody("Target language is required."));
        }

        bool hasText = !string.IsNullOrWhiteSpace(request.Text);
        bool hasBatch = request.Batch is { Count: > 0 };

        if (!hasText && !hasBatch)
        {
            return BadRequest(new StatusMessageResponseBody("Provide text or batch values to translate."));
        }

        if (hasText && hasBatch)
        {
            return BadRequest(new StatusMessageResponseBody("Provide either text or batch values, not both."));
        }

        if (string.IsNullOrWhiteSpace(Options.Instance?.TranslationApiKey))
        {
            return StatusCode(
                (int)HttpStatusCode.ServiceUnavailable,
                new StatusMessageResponseBody("Translation API key is not configured.")
            );
        }

        try
        {
            if (hasText)
            {
                var translation = await RequestTranslationSingle(request.Text!, request.TargetLanguage!, cancellationToken);
                return Ok(new TranslationResponseBody { Translation = translation });
            }

            var translations = await RequestTranslationBatch(request.Batch!, request.TargetLanguage!, cancellationToken);
            return Ok(new TranslationResponseBody { Translations = translations });
        }
        catch (Exception exception)
        {
            ApplicationContext.Context.Value?.Logger.LogError(exception, "Translation request failed");
            return StatusCode((int)HttpStatusCode.BadGateway, new StatusMessageResponseBody("Translation request failed."));
        }
    }

    private async Task<string> RequestTranslationSingle(
        string text,
        string targetLanguage,
        CancellationToken cancellationToken
    )
    {
        var prompt = $"Translate to {targetLanguage}. Return ONLY translated text. Text: {text}";
        return await SendLlmRequest(prompt, cancellationToken);
    }

    private async Task<Dictionary<string, string>> RequestTranslationBatch(
        Dictionary<string, string> texts,
        string targetLanguage,
        CancellationToken cancellationToken
    )
    {
        var jsonPayload = JsonConvert.SerializeObject(texts);
        var prompt = $"You are a localization system. Translate the VALUES of the following JSON object to {targetLanguage}. \n" +
                     "Do NOT translate keys. Do NOT add explanations. Return ONLY the valid JSON object.\n" +
                     "Preserve formatting tokens ({0}, \\c{...}).\n\n" +
                     $"JSON:\n{jsonPayload}";

        var responseText = await SendLlmRequest(prompt, cancellationToken);
        responseText = responseText.Replace("```json", "").Replace("```", "").Trim();

        try
        {
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(responseText)
                ?? new Dictionary<string, string>();
        }
        catch (JsonException)
        {
            ApplicationContext.Context.Value?.Logger.LogWarning("LLM returned invalid JSON for batch.");
            return new Dictionary<string, string>();
        }
    }

    private static async Task<string> SendLlmRequest(string prompt, CancellationToken cancellationToken)
    {
        var settings = GetTranslationSettings();
        var requestUri = BuildRequestUri(settings);
        var requestBody = new
        {
            model = settings.DeploymentName,
            messages = new[]
            {
                new { role = "system", content = "You are a professional game localization assistant. Be concise." },
                new { role = "user", content = prompt },
            },
            temperature = 0.1,
            max_tokens = 2048,
        };

        var json = JsonConvert.SerializeObject(requestBody);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = content,
        };

        requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!string.IsNullOrWhiteSpace(Options.Instance?.TranslationApiKey))
        {
            requestMessage.Headers.Add("api-key", Options.Instance?.TranslationApiKey);
        }

        using var response = await HttpClient.SendAsync(requestMessage, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JObject.Parse(responseString);
        return result["choices"]?[0]?["message"]?["content"]?.ToString() ?? string.Empty;
    }

    private static TranslationSettings GetTranslationSettings()
    {
        var options = Options.Instance;

        var endpoint = string.IsNullOrWhiteSpace(options?.TranslationEndpoint)
            ? DefaultEndpoint
            : options!.TranslationEndpoint;
        var deploymentName = string.IsNullOrWhiteSpace(options?.TranslationDeploymentName)
            ? DefaultDeploymentName
            : options!.TranslationDeploymentName;
        var apiVersion = string.IsNullOrWhiteSpace(options?.TranslationApiVersion)
            ? DefaultApiVersion
            : options!.TranslationApiVersion;

        return new TranslationSettings(endpoint, deploymentName, apiVersion);
    }

    private static Uri BuildRequestUri(TranslationSettings settings)
    {
        var baseEndpoint = NormalizeEndpoint(settings.Endpoint);
        var requestPath =
            $"openai/deployments/{settings.DeploymentName}/chat/completions?api-version={settings.ApiVersion}";
        return new Uri(baseEndpoint, requestPath);
    }

    private static Uri NormalizeEndpoint(string endpoint)
    {
        var sanitized = endpoint.Trim();
        if (!sanitized.EndsWith("/", StringComparison.Ordinal))
        {
            sanitized += "/";
        }

        if (!Uri.TryCreate(sanitized, UriKind.Absolute, out var uri))
        {
            return new Uri(DefaultEndpoint);
        }

        var path = uri.AbsolutePath;
        var openAiIndex = path.IndexOf("/openai/", StringComparison.OrdinalIgnoreCase);
        if (openAiIndex < 0)
        {
            return uri;
        }

        var basePath = path[..(openAiIndex + 1)];
        if (!basePath.EndsWith("/", StringComparison.Ordinal))
        {
            basePath += "/";
        }

        var builder = new UriBuilder(uri)
        {
            Path = basePath,
            Query = string.Empty,
            Fragment = string.Empty,
        };

        return builder.Uri;
    }

    private sealed record TranslationSettings(string Endpoint, string DeploymentName, string ApiVersion);
}
