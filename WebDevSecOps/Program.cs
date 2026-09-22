using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using WebDevSecOps.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews();

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("LoginPolicy", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = builder.Configuration.GetValue<int>("RateLimiting:Login:PermitLimit", 5),
                Window = TimeSpan.FromMinutes(builder.Configuration.GetValue<int>("RateLimiting:Login:WindowMinutes", 1)),
                QueueLimit = 0
            }));
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.Name = ".Auth.BFF";
        options.Cookie.MaxAge = TimeSpan.FromHours(8);
        options.LoginPath = "/Login";
        options.LogoutPath = "/Login";
        options.AccessDeniedPath = "/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AllowAnonymousToPage("/Login");
});

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ITokenStore, TokenStore>();
builder.Services.AddHttpContextAccessor();

// Egress IPv6 roto en la maquina + DNS IPv6-first: curl hace Happy Eyeballs y cae
// a IPv4 en ~200 ms, pero SocketsHttpHandler intenta los IPv6 en serie y agota el
// AttemptTimeout. Se prefiere IPv4 con fallback a IPv6 (no solo-IPv4 estricto).
static SocketsHttpHandler CreateIpv4PreferredHandler() => new()
{
    ConnectCallback = async (context, ct) =>
    {
        // Fast-path: BaseUrl con IP literal, evita DNS.
        if (IPAddress.TryParse(context.DnsEndPoint.Host, out var literal))
        {
            var literalSocket = new Socket(literal.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            try
            {
                await literalSocket.ConnectAsync(new IPEndPoint(literal, context.DnsEndPoint.Port), ct);
                return new NetworkStream(literalSocket, ownsSocket: true);
            }
            catch
            {
                literalSocket.Dispose();
                throw;
            }
        }

        var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, ct);
        var ordered = addresses
            .OrderBy(a => a.AddressFamily == AddressFamily.InterNetwork ? 0 : 1)
            .ToArray();

        if (ordered.Length == 0)
        {
            throw new InvalidOperationException("Sin direccion IP para " + context.DnsEndPoint.Host);
        }

        Exception? lastError = null;
        foreach (var address in ordered)
        {
            var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            try
            {
                await socket.ConnectAsync(new IPEndPoint(address, context.DnsEndPoint.Port), ct);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                lastError = ex;
                socket.Dispose();
            }
        }

        throw lastError ?? new InvalidOperationException("No se pudo conectar a " + context.DnsEndPoint.Host);
    }
};

builder.Services.AddHttpClient<IAuthService, AuthService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseUrl"]
        ?? throw new InvalidOperationException("ApiSettings:BaseUrl is not configured."));
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(CreateIpv4PreferredHandler)
.AddStandardResilienceHandler();

builder.Services.AddHttpClient<IUsuarioService, UsuarioService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseUrl"]
        ?? throw new InvalidOperationException("ApiSettings:BaseUrl is not configured."));
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(CreateIpv4PreferredHandler)
.AddStandardResilienceHandler();

builder.Services.AddHttpClient<ITipoEmpleadoService, TipoEmpleadoService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseUrl"]
        ?? throw new InvalidOperationException("ApiSettings:BaseUrl is not configured."));
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(CreateIpv4PreferredHandler)
.AddStandardResilienceHandler();

builder.Services.AddHttpClient<IEmpleadoService, EmpleadoService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseUrl"]
        ?? throw new InvalidOperationException("ApiSettings:BaseUrl is not configured."));
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(CreateIpv4PreferredHandler)
.AddStandardResilienceHandler();

builder.Services.AddHttpClient<IClienteService, ClienteService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseUrl"]
        ?? throw new InvalidOperationException("ApiSettings:BaseUrl is not configured."));
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(CreateIpv4PreferredHandler)
.AddStandardResilienceHandler();

builder.Services.AddHttpClient<IProductoService, ProductoService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseUrl"]
        ?? throw new InvalidOperationException("ApiSettings:BaseUrl is not configured."));
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(CreateIpv4PreferredHandler)
.AddStandardResilienceHandler();

builder.Services.AddHttpClient<IEstadoVentaService, EstadoVentaService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseUrl"]
        ?? throw new InvalidOperationException("ApiSettings:BaseUrl is not configured."));
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(CreateIpv4PreferredHandler)
.AddStandardResilienceHandler();

builder.Services.AddHttpClient<IVentaService, VentaService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseUrl"]
        ?? throw new InvalidOperationException("ApiSettings:BaseUrl is not configured."));
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(CreateIpv4PreferredHandler)
.AddStandardResilienceHandler();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseResponseCompression();
app.UseHttpsRedirection();
app.UseStaticFiles();

app.Use(async (context, next) =>
{
    var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    context.Items["ScriptNonce"] = nonce;

    context.Response.Headers["Content-Security-Policy"] =
        $"default-src 'self'; " +
        $"script-src 'self' 'nonce-{nonce}'; " +
        $"style-src 'self' 'nonce-{nonce}'; " +
        $"img-src 'self' data:; " +
        $"font-src 'self'; " +
        $"connect-src 'self'; " +
        $"form-action 'self'; " +
        $"base-uri 'self'";
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] =
        "camera=(), display-capture=(), fullscreen=(self), geolocation=(), " +
        "microphone=(), payment=(), publickey-credentials-get=(), " +
        "screen-wake-lock=(), interest-cohort=()";
    context.Response.Headers["Cross-Origin-Embedder-Policy"] = "require-corp";
    context.Response.Headers["Cross-Origin-Opener-Policy"] = "same-origin";
    await next();
});

app.UseRouting();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Usuario}/{action=Index}/{id?}");

app.MapRazorPages()
   .WithStaticAssets();

await app.RunAsync();
