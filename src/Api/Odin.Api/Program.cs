using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Odin.Api.ExceptionHandlers;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.AddFeatureModules();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddMicrosoftIdentityWebApi(builder.Configuration, subscribeToJwtBearerMiddlewareDiagnosticsEvents: true);

builder.Services.AddAuthorization();

builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = ctx =>
    {
        ctx.ProblemDetails.Instance = $"{ctx.HttpContext.Request.Method} {ctx.HttpContext.Request.Path}";
        ctx.ProblemDetails.Extensions.TryAdd("requestId", ctx.HttpContext.TraceIdentifier);
    });
builder.Services.AddExceptionHandler<ExceptionToProblemDetailsHandler>();

var app = builder.Build();

app.UseStatusCodePages();
app.UseExceptionHandler(new ExceptionHandlerOptions
{
    StatusCodeSelector = ex => ex switch
    {
        InvalidOperationException _ => StatusCodes.Status400BadRequest,
        _ => StatusCodes.Status500InternalServerError,
    },
});

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapFeatureModules();

app.Run();

public partial class Program { }
