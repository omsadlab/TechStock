using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace TechStock.Blazor.Services;

public class AppAuthStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _storage;

    public AppAuthStateProvider(ILocalStorageService storage) => _storage = storage;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _storage.GetItemAsync<string>("authToken");

        if (string.IsNullOrWhiteSpace(token))
            return Unauthenticated();

        var claims = ParseClaimsFromJwt(token);
        var expiryClaim = claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Exp);

        if (expiryClaim != null && long.TryParse(expiryClaim.Value, out var exp))
        {
            var expiry = DateTimeOffset.FromUnixTimeSeconds(exp);
            if (expiry < DateTimeOffset.UtcNow)
                return Unauthenticated();
        }

        var identity = new ClaimsIdentity(claims, "jwt");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public void NotifyAuthChanged() => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

    private static AuthenticationState Unauthenticated() =>
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var handler = new JwtSecurityTokenHandler();
        try
        {
            var token = handler.ReadJwtToken(jwt);
            return token.Claims;
        }
        catch
        {
            return Enumerable.Empty<Claim>();
        }
    }
}
