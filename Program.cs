using System.Text.Json;
using PyrxSynapse;
using PyrxSynapse.Models;
using PyrxSynapse.Errors;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var client = new SynapseClient(new SynapseConfig
{
    ApiKey = Environment.GetEnvironmentVariable("SYNAPSE_API_KEY") ?? "",
    WorkspaceId = Environment.GetEnvironmentVariable("SYNAPSE_WORKSPACE_ID") ?? "",
    BaseUrl = Environment.GetEnvironmentVariable("SYNAPSE_API_URL") ?? "https://synapse-api.pyrx.tech"
});

// ── Helpers ──

static string ToSnakeCase(string camelCase)
{
    var snake = "";
    for (var i = 0; i < camelCase.Length; i++)
    {
        var c = camelCase[i];
        if (c >= 'A' && c <= 'Z')
        {
            if (i > 0)
                snake += "_";
            snake += (char)(c + 32);
        }
        else
        {
            snake += c;
        }
    }
    return snake;
}

static Dictionary<string, object> SnakeKeys(Dictionary<string, object> dict)
{
    var result = new Dictionary<string, object>(dict.Count);
    foreach (var kvp in dict)
    {
        result[ToSnakeCase(kvp.Key)] = kvp.Value;
    }
    return result;
}

static Dictionary<string, object> JsonElementToDict(JsonElement el)
{
    var dict = new Dictionary<string, object>();
    if (el.ValueKind != JsonValueKind.Object) return dict;
    foreach (var prop in el.EnumerateObject())
    {
        dict[prop.Name] = JsonElementToValue(prop.Value);
    }
    return dict;
}

static object JsonElementToValue(JsonElement el)
{
    return el.ValueKind switch
    {
        JsonValueKind.String => el.GetString()!,
        JsonValueKind.Number => el.TryGetInt64(out var l) ? l : el.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Array => el.EnumerateArray().Select(JsonElementToValue).ToList(),
        JsonValueKind.Object => JsonElementToDict(el),
        _ => el.ToString()
    };
}

IResult HandleError(SynapseException ex)
{
    return Results.Json(new { error = ex.Message, status = ex.Status }, statusCode: ex.Status);
}

// ── Core ──

app.MapPost("/api/track", async (HttpContext ctx) =>
{
    try
    {
        var body = await ctx.Request.ReadFromJsonAsync<JsonElement>();
        var resp = await client.TrackAsync(new TrackParams
        {
            ExternalId = body.GetProperty("userId").GetString() ?? "",
            EventName = body.GetProperty("event").GetString() ?? "",
            Attributes = body.TryGetProperty("attributes", out var attrs)
                ? JsonElementToDict(attrs)
                : new Dictionary<string, object>()
        });
        return Results.Json(resp);
    }
    catch (SynapseException ex)
    {
        return HandleError(ex);
    }
});

app.MapPost("/api/track/batch", async (HttpContext ctx) =>
{
    try
    {
        var body = await ctx.Request.ReadFromJsonAsync<JsonElement>();
        var events = new List<TrackParams>();
        foreach (var ev in body.GetProperty("events").EnumerateArray())
        {
            events.Add(new TrackParams
            {
                ExternalId = ev.GetProperty("externalId").GetString() ?? "",
                EventName = ev.GetProperty("eventName").GetString() ?? "",
                Attributes = ev.TryGetProperty("attributes", out var attrs)
                    ? JsonElementToDict(attrs)
                    : new Dictionary<string, object>()
            });
        }
        var resp = await client.TrackBatchAsync(new TrackBatchParams { Events = events });
        return Results.Json(resp);
    }
    catch (SynapseException ex)
    {
        return HandleError(ex);
    }
});

app.MapPost("/api/identify", async (HttpContext ctx) =>
{
    try
    {
        var body = await ctx.Request.ReadFromJsonAsync<JsonElement>();
        var resp = await client.IdentifyAsync(new IdentifyParams
        {
            ExternalId = body.GetProperty("userId").GetString() ?? "",
            Email = body.TryGetProperty("email", out var email) ? email.GetString() ?? "" : "",
            Properties = body.TryGetProperty("properties", out var props)
                ? JsonElementToDict(props)
                : new Dictionary<string, object>()
        });
        return Results.Json(resp);
    }
    catch (SynapseException ex)
    {
        return HandleError(ex);
    }
});

app.MapPost("/api/identify/batch", async (HttpContext ctx) =>
{
    try
    {
        var body = await ctx.Request.ReadFromJsonAsync<JsonElement>();
        var contacts = new List<IdentifyParams>();
        foreach (var ct in body.GetProperty("contacts").EnumerateArray())
        {
            contacts.Add(new IdentifyParams
            {
                ExternalId = ct.GetProperty("externalId").GetString() ?? "",
                Email = ct.TryGetProperty("email", out var email) ? email.GetString() ?? "" : "",
                Properties = ct.TryGetProperty("properties", out var props)
                    ? JsonElementToDict(props)
                    : new Dictionary<string, object>()
            });
        }
        var resp = await client.IdentifyBatchAsync(new IdentifyBatchParams { Contacts = contacts });
        return Results.Json(resp);
    }
    catch (SynapseException ex)
    {
        return HandleError(ex);
    }
});

app.MapPost("/api/send", async (HttpContext ctx) =>
{
    try
    {
        var body = await ctx.Request.ReadFromJsonAsync<JsonElement>();
        var to = body.TryGetProperty("to", out var toEl)
            ? SnakeKeys(JsonElementToDict(toEl))
            : new Dictionary<string, object>();
        var resp = await client.SendAsync(new SendParams
        {
            TemplateSlug = body.GetProperty("templateSlug").GetString() ?? "",
            To = to,
            Attributes = body.TryGetProperty("attributes", out var attrs)
                ? JsonElementToDict(attrs)
                : new Dictionary<string, object>()
        });
        return Results.Json(resp);
    }
    catch (SynapseException ex)
    {
        return HandleError(ex);
    }
});

// ── Contacts ──

app.MapGet("/api/contacts", async (HttpContext ctx) =>
{
    try
    {
        var page = int.TryParse(ctx.Request.Query["page"], out var p) ? p : 1;
        var perPage = int.TryParse(ctx.Request.Query["limit"], out var pp) ? pp : 20;
        var resp = await client.Contacts.ListAsync(new ContactListParams { Page = page, PerPage = perPage });
        return Results.Json(resp);
    }
    catch (SynapseException ex)
    {
        return HandleError(ex);
    }
});

app.MapGet("/api/contacts/{id}", async (string id) =>
{
    try
    {
        var resp = await client.Contacts.GetAsync(id);
        return Results.Json(resp);
    }
    catch (SynapseException ex)
    {
        return HandleError(ex);
    }
});

app.MapPut("/api/contacts/{externalId}", async (string externalId, HttpContext ctx) =>
{
    try
    {
        var body = await ctx.Request.ReadFromJsonAsync<JsonElement>();
        var properties = body.TryGetProperty("properties", out var props)
            ? JsonElementToDict(props)
            : new Dictionary<string, object>();
        var resp = await client.Contacts.UpdateAsync(externalId, new ContactUpdateParams
        {
            Properties = properties
        });
        return Results.Json(resp);
    }
    catch (SynapseException ex)
    {
        return HandleError(ex);
    }
});

app.MapDelete("/api/contacts/{externalId}", async (string externalId) =>
{
    try
    {
        await client.Contacts.DeleteAsync(externalId);
        return Results.Json(new { success = true });
    }
    catch (SynapseException ex)
    {
        return HandleError(ex);
    }
});

// ── Templates ──

app.MapGet("/api/templates", async () =>
{
    try
    {
        var resp = await client.Templates.ListAsync();
        return Results.Json(resp);
    }
    catch (SynapseException ex)
    {
        return HandleError(ex);
    }
});

app.MapPost("/api/templates", async (HttpContext ctx) =>
{
    try
    {
        var body = await ctx.Request.ReadFromJsonAsync<JsonElement>();
        var resp = await client.Templates.CreateAsync(new TemplateCreateParams
        {
            Slug = body.GetProperty("slug").GetString() ?? "",
            Name = body.GetProperty("name").GetString() ?? "",
            Subject = body.GetProperty("subject").GetString() ?? "",
            BodyHtml = body.GetProperty("body_html").GetString() ?? "",
            SenderName = body.GetProperty("sender_name").GetString() ?? "",
            FromEmail = body.GetProperty("from_email").GetString() ?? ""
        });
        return Results.Json(resp);
    }
    catch (SynapseException ex)
    {
        return HandleError(ex);
    }
});

app.MapGet("/api/templates/{slug}", async (string slug) =>
{
    try
    {
        var resp = await client.Templates.GetAsync(slug);
        return Results.Json(resp);
    }
    catch (SynapseException ex)
    {
        return HandleError(ex);
    }
});

app.MapPut("/api/templates/{slug}", async (string slug, HttpContext ctx) =>
{
    try
    {
        var body = await ctx.Request.ReadFromJsonAsync<JsonElement>();
        var updateParams = new TemplateUpdateParams();
        if (body.TryGetProperty("subject", out var subj))
            updateParams.Subject = subj.GetString() ?? "";
        if (body.TryGetProperty("name", out var name))
            updateParams.Name = name.GetString() ?? "";
        if (body.TryGetProperty("body_html", out var html))
            updateParams.BodyHtml = html.GetString() ?? "";
        if (body.TryGetProperty("sender_name", out var sn))
            updateParams.SenderName = sn.GetString() ?? "";
        if (body.TryGetProperty("from_email", out var fe))
            updateParams.FromEmail = fe.GetString() ?? "";

        var resp = await client.Templates.UpdateAsync(slug, updateParams);
        return Results.Json(resp);
    }
    catch (SynapseException ex)
    {
        return HandleError(ex);
    }
});

app.MapDelete("/api/templates/{slug}", async (string slug) =>
{
    try
    {
        await client.Templates.DeleteAsync(slug);
        return Results.Json(new { success = true });
    }
    catch (SynapseException ex)
    {
        return HandleError(ex);
    }
});

app.MapPost("/api/templates/{slug}/preview", async (string slug, HttpContext ctx) =>
{
    try
    {
        var body = await ctx.Request.ReadFromJsonAsync<JsonElement>();
        var contact = body.TryGetProperty("contact", out var ct)
            ? JsonElementToDict(ct)
            : new Dictionary<string, object>();
        var resp = await client.Templates.PreviewAsync(slug, new TemplatePreviewParams
        {
            Contact = contact
        });
        return Results.Json(resp);
    }
    catch (SynapseException ex)
    {
        return HandleError(ex);
    }
});

var port = Environment.GetEnvironmentVariable("PORT") ?? "4014";
app.Run($"http://localhost:{port}");
