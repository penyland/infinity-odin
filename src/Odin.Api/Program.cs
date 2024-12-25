var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddFeatureModules();

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    //app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapFeatureModules();

app.Run();

public partial class  Program { }
