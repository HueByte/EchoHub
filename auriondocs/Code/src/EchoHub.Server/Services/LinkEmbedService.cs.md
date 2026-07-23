# LinkEmbedService

> **File:** `src/EchoHub.Server/Services/LinkEmbedService.cs`  
> **Kind:** class

*Figure: How LinkEmbedService works.*

```mermaid
%%{init: {'theme':'base','themeVariables':{'background':'#faf7ef','primaryColor':'#f0e2c2','primaryTextColor':'#1f2840','primaryBorderColor':'#8a7548','secondaryColor':'#d9efec','secondaryBorderColor':'#1d8a80','secondaryTextColor':'#1f2840','tertiaryColor':'#f2ebd8','tertiaryBorderColor':'#8a7548','tertiaryTextColor':'#1f2840','lineColor':'#1d8a80','titleColor':'#1f2840','fontSize':'14px','edgeLabelBackground':'#faf7ef','clusterBkg':'#f2ebd8','clusterBorder':'#8a7548','actorBkg':'#f0e2c2','actorBorder':'#8a7548','actorTextColor':'#1f2840','actorLineColor':'#8a7548','signalColor':'#1d8a80','signalTextColor':'#1f2840','activationBkgColor':'#d9efec','activationBorderColor':'#1d8a80','noteBkgColor':'#f2ebd8','noteBorderColor':'#8a7548','noteTextColor':'#1f2840','labelBoxBkgColor':'#f0e2c2','labelBoxBorderColor':'#8a7548','labelTextColor':'#1f2840','transitionColor':'#1d8a80','transitionLabelColor':'#1f2840','stateLabelColor':'#1f2840','altBackground':'#f2ebd8'}}}%%
flowchart TB
Start["Start"]
Extract["ExtractUrls(content) -> urls"]
CheckUrls["urls.Count == 0?"]
ReturnNullNoUrls["Return null (no URLs found)"]
InitEmbeds["Create empty List#60;EmbedDto#62; embeds"]
ForEach["For each url in urls"]
CallFetch["Call FetchEmbedForUrlAsync(url)"]
ReturnNullFromFetch["Returned null -> continue"]
AddEmbed["Add EmbedDto to embeds"]
CatchLog["Catch Exception -> LogDebug and continue"]
AfterLoop["All URLs processed"]
ReturnDecision["embeds.Count > 0?"]
ReturnEmbeds["Return embeds"]
ReturnNullAll["Return null (no successful embeds)"]

subgraph FetchEmbedForUrlAsync
  F1["Try Uri.TryCreate(url, Absolute)"]
  F1_no["Return null (invalid uri)"]
  F2["Check scheme is http or https"]
  F2_no["Return null (unsupported scheme)"]
  F3["IsPrivateHost(uri)?"]
  F3_no["Return null (private host)"]
  F4["Create CTS with HubConstants.EmbedFetchTimeoutSeconds"]
  F5["Create HTTP client 'OgFetch'"]
  F6["Send GET request, get response"]
  F7["response.IsSuccessStatusCode?"]
  F7_no["Return null (unsuccessful status)"]
  F8["Content-Type starts with #quot;text/html#quot;?"]
  F8_no["Return null (non-html content)"]
  F9["Read limited HTML (HubConstants.EmbedMaxHtmlBytes)"]
  F9_empty["Return null (empty or whitespace html)"]
  F10["Parse OG tags, get title or fall back to #60;title#62;"]
  F10_no["Return null (no title)"]
  F11["Build EmbedDto and return"]
end

Start --> Extract
Extract --> CheckUrls
CheckUrls -->|"yes"| ReturnNullNoUrls
CheckUrls -->|"no"| InitEmbeds
InitEmbeds --> ForEach
ForEach --> CallFetch
CallFetch -->|"throws"| CatchLog
CallFetch -->|"null"| ReturnNullFromFetch
CallFetch -->|"EmbedDto"| AddEmbed
ReturnNullFromFetch --> ForEach
AddEmbed --> ForEach
CatchLog --> ForEach
ForEach -->|"done"| AfterLoop
AfterLoop --> ReturnDecision
ReturnDecision -->|"yes"| ReturnEmbeds
ReturnDecision -->|"no"| ReturnNullAll

CallFetch --> F1
F1 -->|"no"| F1_no
F1 -->|"yes"| F2
F2 -->|"no"| F2_no
F2 -->|"yes"| F3
F3 -->|"true"| F3_no
F3 -->|"false"| F4
F4 --> F5
F5 --> F6
F6 --> F7
F7 -->|"no"| F7_no
F7 -->|"yes"| F8
F8 -->|"no"| F8_no
F8 -->|"yes"| F9
F9 -->|"empty"| F9_empty
F9 -->|"has html"| F10
F10 -->|"no"| F10_no
F10 -->|"yes"| F11

F1_no --> ReturnNullFromFetch
F2_no --> ReturnNullFromFetch
F3_no --> ReturnNullFromFetch
F7_no --> ReturnNullFromFetch
F8_no --> ReturnNullFromFetch
F9_empty --> ReturnNullFromFetch
F10_no --> ReturnNullFromFetch
F11 --> AddEmbed
```

```csharp
public partial class LinkEmbedService
```


Scans a piece of message text for URLs and attempts to produce lightweight link preview data (EmbedDto) by fetching and parsing Open Graph and common HTML metadata. Use TryGetEmbedsAsync when you need server-side link previews for chat messages and want a defensive, timeout- and size-limited fetch that never throws (it logs failures and returns null when no usable embeds are found).

## Remarks
LinkEmbedService centralizes the logic for discovering URLs in a message and converting remote HTML metadata into EmbedDto instances suitable for display. It is intentionally defensive: only absolute http/https URLs are considered, private hosts are skipped, fetches are limited by a cancellation timeout and a maximum HTML byte count (HubConstants), and only text/html responses are parsed. Errors during individual fetches are caught and logged at debug level so the caller observes either a list of successful embeds or null (no useful embeds).

## Example
```csharp
// Given an instance of LinkEmbedService (typically from DI):
var embeds = await linkEmbedService.TryGetEmbedsAsync(messageContent);
if (embeds is null)
{
    // No embeds found or all fetch attempts failed.
}
else
{
    Console.WriteLine($"Found {embeds.Count} embeds");
    foreach (var embed in embeds)
    {
        // render embed in UI or pass to presentation layer
    }
}
```

## Notes
- TryGetEmbedsAsync returns null when no URLs are present or when all fetches fail; it does not return an empty list in those cases—check for null before iterating. 
- The service expects an IHttpClientFactory and creates a client with the name "OgFetch"; ensure your HttpClient configuration (handlers, DNS/timeout policies) is appropriate for remote HTML fetches.
- HTML metadata extraction is heuristic: it uses Open Graph tags, falls back to a <title> regex, reads only the first N bytes of HTML, and truncates long descriptions per HubConstants. Consumers should treat returned fields as untrusted display content and apply any necessary sanitization in the UI layer.