using BackendForFrontend.Models;
using BackendForFrontend.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();

// Configure session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// Configure OAuth options
builder.Services.Configure<OAuthOptions>(builder.Configuration.GetSection("OAuth"));

// Register services
builder.Services.AddScoped<IPkceService, PkceService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IOAuthService, OAuthService>();

// Configure YARP Reverse Proxy with custom transform provider
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms<TokenTransformProvider>();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseSession();
app.UseCors();
app.UseHttpsRedirection();
app.MapControllers();

// Map YARP reverse proxy routes
app.MapReverseProxy();

app.Run();
