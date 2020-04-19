using Greenwell.Models;
using IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

//Profile service extends the existing ProfileService method to add roles and role claims to profile data.
public class ProfileService : IProfileService
{
    protected UserManager<ApplicationUser> mUserManager;

    public ProfileService(UserManager<ApplicationUser> userManager)
    {
        mUserManager = userManager;
    }

    public async Task GetProfileDataAsync(ProfileDataRequestContext context)
    {
        //We get the user
        ApplicationUser user = await mUserManager.GetUserAsync(context.Subject);

        //We get a list of the user's roles
        IList<string> roles = await mUserManager.GetRolesAsync(user);

        //We add a claim to every role on the lost.
        IList<Claim> roleClaims = new List<Claim>();
        foreach (string role in roles)
        {
            roleClaims.Add(new Claim(JwtClaimTypes.Role, role));
        }
        context.IssuedClaims.Add(new Claim(JwtClaimTypes.Name, user.UserName));
        context.IssuedClaims.AddRange(roleClaims);
        //Add more claims as you need
    }

    public Task IsActiveAsync(IsActiveContext context)
    {
        return Task.CompletedTask;
    }
}