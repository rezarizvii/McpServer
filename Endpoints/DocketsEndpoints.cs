using System.Net.Http;
using System.Web;                // for HttpUtility
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using System.Text.Json;
using System.Text.Json.Nodes;

public static class DocketsEndpoints
{
    public static void MapDocketsEndpoints(this WebApplication app)
    {
        app.MapPost("/tools/get-dockets", async (
            [FromBody] FilterQuery query,
            IHttpClientFactory clientFactory,
            IConfiguration config) =>
        {
            string apiBase = config["FunctionPoint:BaseUrl"] ?? "https://api-platform.functionpoint.com";
            string apiToken = config["FunctionPoint:Token"] ?? "";

            var queryParams = HttpUtility.ParseQueryString(string.Empty);
            queryParams["page"] = query.page?.ToString() ?? "1";
            queryParams["itemsPerPage"] = query.itemsPerPage?.ToString() ?? "30";

            foreach (var kv in query.Filters)
                if (!string.IsNullOrWhiteSpace(kv.Value))
                    queryParams[kv.Key] = kv.Value;

            var http = clientFactory.CreateClient();
            http.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiToken}");
            http.DefaultRequestHeaders.Add("Accept", "application/ld+json");

            var allItems = new List<JsonNode>();
            string? nextUrl = $"{apiBase}/dockets?{queryParams}";
            string? lastUrl = null;

            while (!string.IsNullOrEmpty(nextUrl))
            {
                var resp = await http.GetAsync(nextUrl);
                if (!resp.IsSuccessStatusCode)
                    return Results.Json(new { success = false, status = (int)resp.StatusCode, error = $"Failed at {nextUrl}" });

                var body = await resp.Content.ReadAsStringAsync();
                var json = JsonNode.Parse(body)!;

                // Grab items and append
                var members = json["hydra:member"]?.AsArray();
                if (members is not null)
                    allItems.AddRange(members);

                // Find next + last links
                var view = json["hydra:view"];
                var next = view?["hydra:next"]?.GetValue<string>();
                lastUrl ??= view?["hydra:last"]?.GetValue<string>();

                if (string.IsNullOrEmpty(next))
                    break;

                // FunctionPoint gives relative URLs like "/dockets?page=2"
                // Prepend base if needed
                nextUrl = next.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                            ? next
                            : $"{apiBase}{next}";
            }

            return Results.Json(new
            {
                success = true,
                totalItems = allItems.Count,
                data = allItems
            });
        })
        .WithName("GetDockets")
        .WithOpenApi(op =>
        {
            op.Summary = "Fetch dockets (jobs) from FunctionPoint with optional filters.";
            return op;
        });
    }
}