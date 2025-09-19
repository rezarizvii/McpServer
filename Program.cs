using System.Net.Http;
using System.Web;                // for HttpUtility
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

string apiBase = builder.Configuration["FunctionPoint:BaseUrl"]
    ?? "https://api-platform.functionpoint.com";
string apiToken = builder.Configuration["FunctionPoint:Token"]
    ?? "";

app.MapPost("/tools/get-estimates", async (
    [FromBody] EstimateQuery query,
    IHttpClientFactory clientFactory) =>
{
    // Build the query string dynamically
    var queryParams = HttpUtility.ParseQueryString(string.Empty);

    if (query.page.HasValue) queryParams["page"] = query.page.Value.ToString();
    if (query.itemsPerPage.HasValue) queryParams["itemsPerPage"] = query.itemsPerPage.Value.ToString();

    // Add any arbitrary filters (order[], etc.)
    foreach (var kv in query.Filters)
    {
        if (!string.IsNullOrWhiteSpace(kv.Value))
            queryParams[kv.Key] = kv.Value;
    }

    string url = $"{apiBase}/estimates";
    if (queryParams.Count > 0)
        url += "?" + queryParams.ToString();

    var http = clientFactory.CreateClient();
    http.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiToken}");
    http.DefaultRequestHeaders.Add("Accept", $"application/ld+json");

    var resp = await http.GetAsync(url);
    var body = await resp.Content.ReadAsStringAsync();

    return Results.Json(new
    {
        success = resp.IsSuccessStatusCode,
        status = (int)resp.StatusCode,
        forwardedUrl = url,
        data = body
    });
})
.WithName("GetEstimates")
.WithOpenApi(op =>
{
    op.Summary = "Fetch estimates from FunctionPoint with optional filters.";
    op.Description = "Pass any supported FunctionPoint query keys inside Filters dictionary.";
    return op;
});

app.Run();
