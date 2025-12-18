using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using TostonApp.Security;

public class PermisosPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider;

    public PermisosPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        => _fallbackPolicyProvider.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
        => _fallbackPolicyProvider.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(PermissionConstants.PolicyPrefix))
        {
            var codigoPermiso = policyName.Substring(PermissionConstants.PolicyPrefix.Length);

            var policy = new AuthorizationPolicyBuilder();
            policy.AddRequirements(new PermisoRequirement(codigoPermiso));
            return Task.FromResult<AuthorizationPolicy?>(policy.Build());
        }

        return _fallbackPolicyProvider.GetPolicyAsync(policyName);
    }
}
