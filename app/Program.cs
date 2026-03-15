using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.AspNetCore.HttpOverrides;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | 
                               ForwardedHeaders.XForwardedProto;
});


builder.Services.AddMicrosoftIdentityWebAppAuthentication(builder.Configuration, "AzureAd");
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseForwardedHeaders();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", [Authorize] (HttpContext context, IConfiguration config) =>
{
    var user = context.User;
    var name = user.FindFirst("name")?.Value ?? user.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
    var email = user.FindFirst("preferred_username")?.Value ?? user.FindFirst(ClaimTypes.Email)?.Value ?? "Unknown";
    var groups = user.Claims
        .Where(c => c.Type == "groups" || c.Type == ClaimTypes.Role || c.Type == "roles")
        .Select(c => c.Value)
        .ToList();

    var dbConn = config["ConnectionStrings:DefaultConnection"] ?? "not set";
    var apiUrl = config["AppSettings:ApiBaseUrl"] ?? "not set";
    var retries = config["AppSettings:MaxRetries"] ?? "not set";
    var featureX = config["AppSettings:EnableFeatureX"] ?? "not set";

    var groupRows = groups.Count > 0
        ? string.Join("", groups.Select(g => "<tr><td>" + g + "</td></tr>"))
        : "<tr><td>No groups returned — check Entra token configuration</td></tr>";

    var html = "<!DOCTYPE html>"
        + "<html><head><title>.NET Entra Auth Demo</title>"
        + "<style>"
        + "body { font-family: Arial, sans-serif; max-width: 800px; margin: 40px auto; padding: 0 20px; background: #1a1a2e; color: #e0e0e0; }"
        + "h1 { color: #0fbcf9; }"
        + ".card { background: #16213e; border-radius: 8px; padding: 20px; margin: 20px 0; border-left: 4px solid #0fbcf9; }"
        + ".card h2 { color: #0fbcf9; margin-top: 0; }"
        + "table { width: 100%; border-collapse: collapse; }"
        + "td { padding: 8px; border-bottom: 1px solid #2a2a4a; }"
        + "td:first-child { color: #888; width: 40%; }"
        + "td:last-child { color: #0fbcf9; font-family: monospace; }"
        + ".tag { display: inline-block; background: #0a3d62; color: #0fbcf9; padding: 4px 12px; border-radius: 4px; margin: 4px; font-family: monospace; }"
        + ".logout { display: inline-block; margin-top: 20px; color: #ff6b6b; text-decoration: none; }"
        + ".warning { color: #ff6b6b; font-size: 0.85em; margin-top: 20px; }"
        + "</style></head><body>"
        + "<h1>.NET Entra ID Auth Demo</h1>"
        + "<a class='logout' href='/signout'>Sign Out</a>"
        + "<div class='card'><h2>Authenticated User</h2><table>"
        + "<tr><td>Name</td><td>" + name + "</td></tr>"
        + "<tr><td>Email</td><td>" + email + "</td></tr>"
        + "</table></div>"
        + "<div class='card'><h2>AD Group Memberships</h2>"
        + "<p>These are the groups returned in the Entra ID token — the same groups your app uses for role-based access.</p>"
        + "<table>" + groupRows + "</table></div>"
        + "<div class='card'><h2>Configuration (from env / appsettings)</h2><table>"
        + "<tr><td>DefaultConnection</td><td>" + dbConn + "</td></tr>"
        + "<tr><td>ApiBaseUrl</td><td>" + apiUrl + "</td></tr>"
        + "<tr><td>MaxRetries</td><td>" + retries + "</td></tr>"
        + "<tr><td>EnableFeatureX</td><td>" + featureX + "</td></tr>"
        + "</table></div>"
        + "<div class='card'><h2>Runtime</h2><table>"
        + "<tr><td>Framework</td><td>" + RuntimeInformation.FrameworkDescription + "</td></tr>"
        + "<tr><td>OS</td><td>" + RuntimeInformation.OSDescription + "</td></tr>"
        + "<tr><td>Hostname</td><td>" + Environment.MachineName + "</td></tr>"
        + "<tr><td>Time (UTC)</td><td>" + DateTime.UtcNow + "</td></tr>"
        + "</table></div>"
        + "<p class='warning'>Demo only — never expose config or group memberships in production.</p>"
        + "</body></html>";

    return Results.Content(html, "text/html");
});

app.MapGet("/signout", async (HttpContext context) =>
{
    await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Content("<!DOCTYPE html><html><body style='background:#1a1a2e;color:#e0e0e0;font-family:Arial;text-align:center;padding-top:100px;'>"
        + "<h1>Signed Out</h1><p><a href='/' style='color:#0fbcf9;'>Sign back in</a></p></body></html>", "text/html");
});

app.MapGet("/health", () => Results.Ok(new { Status = "healthy", Time = DateTime.UtcNow }));

app.Run();