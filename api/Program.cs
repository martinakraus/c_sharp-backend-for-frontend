using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// CORS: Entweder AllowAnyOrigin ODER AllowCredentials
builder.Services.AddCors(options =>
{
    options.AddPolicy("CORS", policy =>
    {
        policy
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowAnyOrigin();
    });
});

var domain = builder.Configuration["Authentication:Authority"];
var audience = builder.Configuration["Authentication:Audience"];
var requireHttpsMetadata = builder.Configuration.GetValue<bool>("Authentication:RequireHttpsMetadata", true);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = domain;
        options.Audience = audience;
        options.RequireHttpsMetadata = requireHttpsMetadata;
        // Akzeptiere beide Issuer (localhost und keycloak im Docker-Netzwerk)
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuers =
            [
                "http://keycloak:8080/realms/bff-realm",
                "http://localhost:8080/realms/bff-realm"
            ],
            ValidAudience = audience,
            ClockSkew = TimeSpan.Zero // Keine Zeittoleranz - Token läuft exakt zur angegebenen Zeit ab (default ist 5 Minuten)
        };
        
        // Map realm_access.roles to role claims
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var claimsIdentity = context.Principal?.Identity as ClaimsIdentity;
                if (claimsIdentity != null)
                {
                    var realmAccessClaim = claimsIdentity.FindFirst("realm_access");
                    if (realmAccessClaim != null)
                    {
                        var realmAccess = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(realmAccessClaim.Value);
                        if (realmAccess.TryGetProperty("roles", out var rolesElement))
                        {
                            foreach (var role in rolesElement.EnumerateArray())
                            {
                                claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, role.GetString()!));
                            }
                        }
                    }
                }
                return Task.CompletedTask;
            }
        };
    });

// Role-based Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("users:read", policy => policy.RequireRole("users:read"));
    options.AddPolicy("users:write", policy => policy.RequireRole("users:write"));
});

builder.Services.AddControllers();

var app = builder.Build();

// Environment-Check korrigiert
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseHsts();
}

app.UseCors("CORS");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers(); // Moderner Ansatz statt UseRouting/UseEndpoints

app.Run();