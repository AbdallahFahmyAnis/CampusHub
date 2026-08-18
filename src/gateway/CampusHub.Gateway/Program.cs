using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.RateLimiting;
using CampusHub.BuildingBlocks.Security;
using CampusHub.Gateway;
using CampusHub.Gateway.Infrastructure;
using CampusHub.ServiceDefaults;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<AccessTokenRefresher>();
builder.Services.AddScoped<DownstreamApi>();
builder.Services.AddScoped<HealthProbe>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("authenticated", policy => policy.RequireAuthenticatedUser());
    options.AddPolicy("admin", policy => policy.RequireRole(Roles.Administrator));
});

builder.Services.AddHttpClient("ops-health", client => client.Timeout = TimeSpan.FromSeconds(3));
builder.Services.AddHttpClient("oidc-token", client => client.Timeout = TimeSpan.FromSeconds(10));
builder.Services.AddHttpClient("catalog", client => client.BaseAddress = new Uri(builder.Configuration["Services:Catalog"] ?? "http://localhost:5102"));
builder.Services.AddHttpClient("enrollment", client => client.BaseAddress = new Uri(builder.Configuration["Services:Enrollment"] ?? "http://localhost:5103"));
builder.Services.AddHttpClient("access", client => client.BaseAddress = new Uri(builder.Configuration["Services:Access"] ?? "http://localhost:5106"));
builder.Services.AddHttpClient("identity", client => client.BaseAddress = new Uri(builder.Configuration["Services:Identity"] ?? "http://localhost:5101"));

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(http =>
    {
        if (!IsBrowserApiCall(http.Request.Path))
        {
            return RateLimitPartition.GetNoLimiter("browser");
        }

        var key = http.User.Identity?.Name ?? http.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 120,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        });
    });
});

var identityAuthority = builder.Configuration["Identity:Authority"] ?? "http://localhost:5101";

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        options.DefaultSignOutScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Cookie.Name = "campushub.bff";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.SlidingExpiration = true;
        options.AccessDeniedPath = "/ops/denied";
        options.Events.OnRedirectToLogin = context =>
        {
            if (IsBrowserApiCall(context.Request.Path))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            }

            return Task.CompletedTask;
        };
        options.Events.OnValidatePrincipal = async context =>
        {
            var refresher = context.HttpContext.RequestServices.GetRequiredService<AccessTokenRefresher>();
            await refresher.RefreshIfNeededAsync(context);
        };
    })
    .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
    {
        options.Authority = identityAuthority;
        var metadata = builder.Configuration["Identity:MetadataAddress"];
        if (!string.IsNullOrWhiteSpace(metadata))
        {
            options.MetadataAddress = metadata;
        }
        options.RequireHttpsMetadata = false;
        options.ClientId = builder.Configuration["Identity:ClientId"] ?? Clients.Gateway;
        options.ClientSecret = builder.Configuration["Identity:ClientSecret"] ?? "gateway-dev-secret";
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.ResponseMode = OpenIdConnectResponseMode.Query;
        options.UsePkce = true;
        options.SaveTokens = true;
        options.GetClaimsFromUserInfoEndpoint = false;
        options.MapInboundClaims = false;
        options.CorrelationCookie.SameSite = SameSiteMode.Lax;
        options.NonceCookie.SameSite = SameSiteMode.Lax;
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.NonceCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = "name",
            RoleClaimType = "role"
        };
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.Scope.Add("roles");
        options.Scope.Add("offline_access");
        options.Scope.Add(Scopes.CatalogApi);
        options.Scope.Add(Scopes.EnrollmentApi);
        options.Scope.Add(Scopes.NotificationApi);
        options.Scope.Add(Scopes.AccessApi);
        options.Scope.Add(Scopes.ChatApi);
        options.CallbackPath = "/signin-oidc";
        options.SignedOutCallbackPath = "/signout-callback-oidc";
        options.SignedOutRedirectUri = "/";
        options.Events.OnRedirectToIdentityProvider = context =>
        {
            if (IsBrowserApiCall(context.Request.Path))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.HandleResponse();
            }

            return Task.CompletedTask;
        };
        options.Events.OnRemoteFailure = context =>
        {
            var returnUrl = context.Properties?.RedirectUri;
            if (string.IsNullOrWhiteSpace(returnUrl) || !returnUrl.StartsWith('/'))
            {
                returnUrl = "/catalog";
            }

            context.Response.Redirect($"/login?returnUrl={Uri.EscapeDataString(returnUrl)}");
            context.HandleResponse();
            return Task.CompletedTask;
        };
    });

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(builderContext =>
    {
        builderContext.AddRequestTransform(async transformContext =>
        {
            var accessToken = await transformContext.HttpContext.GetTokenAsync("access_token");
            if (!string.IsNullOrEmpty(accessToken))
            {
                transformContext.ProxyRequest.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", accessToken);
            }

            if (transformContext.HttpContext.Items[CampusHub.BuildingBlocks.Diagnostics.CorrelationId.ItemKey] is string correlationId)
            {
                transformContext.ProxyRequest.Headers.Remove(CampusHub.BuildingBlocks.Diagnostics.CorrelationId.HeaderName);
                transformContext.ProxyRequest.Headers.Add(
                    CampusHub.BuildingBlocks.Diagnostics.CorrelationId.HeaderName,
                    correlationId);
            }
        });
    });

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseServiceDefaults();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseWebSockets();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapGet("/login", (string? returnUrl) =>
        Results.Challenge(new AuthenticationProperties
        {
            RedirectUri = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl
        }))
    .AllowAnonymous();

app.MapMethods("/logout", ["GET", "POST"], () =>
        Results.SignOut(
            new AuthenticationProperties { RedirectUri = "/" },
            [
                CookieAuthenticationDefaults.AuthenticationScheme,
                OpenIdConnectDefaults.AuthenticationScheme
            ]))
    .AllowAnonymous()
    .DisableAntiforgery();

app.MapGet("/whoami", (HttpContext http) =>
    {
        var user = http.User;
        return Results.Ok(new
        {
            authenticated = user.Identity?.IsAuthenticated ?? false,
            name = user.Identity?.Name,
            email = user.FindFirstValue("email") ?? user.FindFirstValue("preferred_username"),
            sub = user.FindFirstValue("sub"),
            roles = user.FindAll("role").Select(c => c.Value).ToArray(),
            tenantId = user.FindFirstValue(Tenancy.TenantIdClaim),
            tenantName = user.FindFirstValue(Tenancy.TenantNameClaim),
            plan = user.FindFirstValue(Tenancy.PlanClaim),
            claims = user.Claims.Select(c => new { c.Type, c.Value })
        });
    })
    .RequireAuthorization();

app.MapAccountEndpoints();
app.MapCampusGatewayEndpoints();
app.MapPost("/api/tenants/register", async (RegisterCampusRequest body, DownstreamApi api, CancellationToken ct) =>
    {
        var (ok, error) = await api.PostJsonAsync("identity", "/api/identity/tenants/register", body, ct, internalKey: true);
        return ok
            ? Results.Ok(new { created = true })
            : Results.BadRequest(new { error = error ?? "Could not create the campus." });
    })
    .AllowAnonymous()
    .DisableAntiforgery();
app.MapRazorPages();
app.MapReverseProxy();
app.MapDefaultEndpoints();

app.Run();

static bool IsBrowserApiCall(PathString path) =>
    path.StartsWithSegments("/api") || path.StartsWithSegments("/socket.io");

public sealed record RegisterCampusRequest(string CampusName, string Email, string DisplayName, string Password);
