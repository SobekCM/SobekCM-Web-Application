using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SobekCM.Engine_Library;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddResponseCompression();

var app = builder.Build();

app.UseResponseCompression();

// Replaces MicroserviceRewriter: if urlrelative is absent from the query string,
// populate it from the request path so MicroserviceHandler can dispatch correctly.
// Requests may arrive either as /engine/aggregation/list  (clean URL)
// or as the legacy sobekcm.svc?urlrelative=/engine/aggregation/list form.
app.Use(async (context, next) =>
{
    if (!context.Request.Query.ContainsKey("urlrelative"))
    {
        string path = context.Request.Path.Value ?? "/";
        context.Request.QueryString = context.Request.QueryString.Add("urlrelative", path);
    }
    await next();
});

// Route every request through MicroserviceHandler (replaces the *.svc IHttpHandler).
app.Run(context =>
{
    var handler = new MicroserviceHandler();
    handler.ProcessRequest(context);
    return Task.CompletedTask;
});

app.Run();
