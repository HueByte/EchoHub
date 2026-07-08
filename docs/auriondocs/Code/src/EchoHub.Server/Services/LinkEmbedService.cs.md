# LinkEmbedService

> **File:** `src/EchoHub.Server/Services/LinkEmbedService.cs`  
> **Kind:** class

*Figure: How LinkEmbedService works.*

```mermaid
%%{init: {'theme':'base','themeVariables':{'background':'#faf7ef','primaryColor':'#f0e2c2','primaryTextColor':'#1f2840','primaryBorderColor':'#8a7548','secondaryColor':'#d9efec','secondaryBorderColor':'#1d8a80','secondaryTextColor':'#1f2840','tertiaryColor':'#f2ebd8','tertiaryBorderColor':'#8a7548','tertiaryTextColor':'#1f2840','lineColor':'#1d8a80','titleColor':'#1f2840','fontSize':'14px','edgeLabelBackground':'#faf7ef','clusterBkg':'#f2ebd8','clusterBorder':'#8a7548','actorBkg':'#f0e2c2','actorBorder':'#8a7548','actorTextColor':'#1f2840','actorLineColor':'#8a7548','signalColor':'#1d8a80','signalTextColor':'#1f2840','activationBkgColor':'#d9efec','activationBorderColor':'#1d8a80','noteBkgColor':'#f2ebd8','noteBorderColor':'#8a7548','noteTextColor':'#1f2840','labelBoxBkgColor':'#f0e2c2','labelBoxBorderColor':'#8a7548','labelTextColor':'#1f2840','transitionColor':'#1d8a80','transitionLabelColor':'#1f2840','stateLabelColor':'#1f2840','altBackground':'#f2ebd8'}}}%%
flowchart TB
Start["LinkEmbedService: TryGetEmbedsAsync(content)"]
Extract["ExtractUrls(content)"]
NoUrls{ "urls.Count == 0?" }
ReturnNull1["Return null (no URLs found)"]
Loop["For each url: call FetchEmbedForUrlAsync(url). Exceptions are logged and skipped"]
Fetch["FetchEmbedForUrlAsync(url)"]
ValidUri{ "Absolute http(s) URI and not private host?" }
ReturnNullF["Return null (invalid/private/failed)"]
FetchSteps["Create CTS with HubConstants.EmbedFetchTimeoutSeconds; send GET with client 'OgFetch'; read up to HubConstants.EmbedMaxHtmlBytes"]
CheckHtml{ "2xx response, Content-Type 'text/html', and non-empty HTML?" }
Parse["Parse OG tags; try og:title, fallback to \"<title>\""]
HasTitle{ "Title present?" }
BuildDto["Construct EmbedDto (truncate description per HubConstants limits) and return EmbedDto"]
AddEmbed["If EmbedDto not null, add to embeds list"]
FinalCheck{ "embeds.Count > 0?" }
ReturnEmbeds["Return List<EmbedDto>"]
ReturnNull2["Return null (no successful embeds)"]

Start --> Extract
Extract --> NoUrls
NoUrls --|"yes"| ReturnNull1
NoUrls --|"no"| Loop

Loop --> Fetch
Fetch --> ValidUri
ValidUri --|"no"| ReturnNullF
ValidUri --|"yes"| FetchSteps

FetchSteps --> CheckHtml
CheckHtml --|"no"| ReturnNullF
CheckHtml --|"yes"| Parse

Parse --> HasTitle
HasTitle --|"no"| ReturnNullF
HasTitle --|"yes"| BuildDto

BuildDto --> AddEmbed
AddEmbed --> Loop

Loop --> FinalCheck
FinalCheck --|"yes"| ReturnEmbeds
FinalCheck --|"no"| ReturnNull2
```

```csharp
public partial class LinkEmbedService
```


Detects URLs inside a message string and attempts to fetch Open Graph / HTML metadata for each link, returning a list of EmbedDto objects when useful previews were obtained. Use TryGetEmbedsAsync when you want non-fatal, best-effort link previews for chat messages or other user-provided content — the method never throws and will return null if no embeds are found or all fetch attempts fail.

## Remarks
This service centralizes safe, constrained fetching of remote pages for link previews. It validates URLs, skips non-http(s) or private hosts, applies a configured timeout and maximum HTML byte limit, and only processes responses with an HTML content-type. Failures for individual URLs are caught and logged at Debug level so callers receive a simple success/failure result (`List<EmbedDto>` or null) without needing to handle network or parsing exceptions. Limits such as the fetch timeout, maximum HTML bytes, and maximum description length come from HubConstants so the behavior is consistent and configurable across the application.

## Notes
- The method returns null to indicate "no useful embeds" rather than an empty list; callers should check for null.
- Requires an IHttpClientFactory to be available; the code requests a client named "OgFetch" (configure a named client if you need custom handlers, timeouts, or proxy settings).
- The service deliberately avoids fetching private/internal hosts and non-http(s) schemes to reduce security risk and accidental data exfiltration.
- Description text is truncated to HubConstants.EmbedMaxDescriptionLength and all text fields are HTML-decoded; large pages are read up to HubConstants.EmbedMaxHtmlBytes and the fetch is bounded by HubConstants.EmbedFetchTimeoutSeconds.