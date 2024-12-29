var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddFeatureModules();

builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthentication().AddJwtBearer();
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

app.MapFeatureModules();

app.Run();

public partial class  Program { }
