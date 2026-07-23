# LinkEmbedService

> **File:** `src/EchoHub.Server/Services/LinkEmbedService.cs`  
> **Kind:** class

```csharp
public partial class LinkEmbedService
```


Detects URLs inside a message and attempts to build lightweight Open Graph-style preview data for each. Call `TryGetEmbedsAsync` when you need server-side preview cards for message text (for example, chat or activity feeds); the service will return a list of `EmbedDto` instances for URLs it successfully fetched and parsed, or `null` if no usable embeds were found. The method is resilient: it never throws to callers (failures are logged) and applies several safeguards (URI validation, scheme check, private-host blocking, content-type and size limits, timeout).

## Remarks
`LinkEmbedService` is a focused utility for fetching and extracting minimal preview metadata from remote pages. It integrates with `IHttpClientFactory` (expects a named client `"OgFetch"`) and logs issues through the injected `ILogger<LinkEmbedService>`. The implementation defends against common server-side preview hazards: it only allows absolute `http`/`https` URIs, rejects private/internal hosts via `IsPrivateHost`, enforces a read timeout using `HubConstants.EmbedFetchTimeoutSeconds`, and limits the amount of HTML read with `HubConstants.EmbedMaxHtmlBytes`. Parsing is performed by extracting Open Graph tags via `ParseOgTags`, falling back to the `<title>` tag (via `TitleTagRegex`), and optionally parsing a `theme-color` meta tag (via `ParseThemeColor` / `ThemeColorRegex`). Text fields are HTML-decoded and long descriptions are truncated to `HubConstants.EmbedMaxDescriptionLength`.

## Notes
- The service expects a named `IHttpClientFactory` client called `"OgFetch"`; if that client is not registered or misconfigured the fetches will fail and be logged at debug level. 
- Fetches are performed sequentially for each URL in `TryGetEmbedsAsync`, so large numbers of links or slow hosts may increase total latency; each fetch is nevertheless bounded by `HubConstants.EmbedFetchTimeoutSeconds`.
- HTML extraction relies on regex-based parsing (`TitleTagRegex`, `ThemeColorRegex`) and the `ParseOgTags` helper; this is intentionally pragmatic but may miss nonstandard or deeply nested metadata. The service also drops non-HTML responses, invalid URIs, non-HTTP(S) schemes, private hosts, and pages that lack any usable `title` or Open Graph title — in all those cases it returns `null` for that URL and proceeds without throwing.