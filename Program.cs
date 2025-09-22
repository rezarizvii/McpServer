using System.Net.Http;
using System.Web;                // for HttpUtility
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.StaticFiles;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

var provider = new FileExtensionContentTypeProvider();
provider.Mappings[".json"] = "application/json";

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, ".well-known")),
    RequestPath = "/.well-known",
    ContentTypeProvider = provider
});

app.MapEstimatesEndpoints();
app.MapDocketsEndpoints();

app.Run();
