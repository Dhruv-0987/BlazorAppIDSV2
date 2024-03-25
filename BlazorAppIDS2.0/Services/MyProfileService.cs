using System.Security.Claims;
using BlazorAppIDS2._0.Data;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Microsoft.EntityFrameworkCore;

namespace BlazorAppIDS.Services;

public class MyProfileService : IProfileService
{
    private readonly ApplicationDbContext _dbContext;

    public MyProfileService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task GetProfileDataAsync(ProfileDataRequestContext context)
    {
        if (context.RequestedClaimTypes.Any())
        {
            context.AddRequestedClaims(context.Subject.Claims);
        }

        var requestedClaimTypes = context.RequestedClaimTypes;

        var userId = context.Subject.GetSubjectId();

        var userClaims = await GetUserClaims(userId, requestedClaimTypes);
        
        context.IssuedClaims.AddRange(userClaims);
    }

    public async Task<List<Claim>> GetUserClaims(string userId, IEnumerable<string> requestedClaimTypes)
    {
        var userClaims = new List<Claim>();

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user is not null)
        {
            foreach (var claimType in requestedClaimTypes)
            {
                var fieldName = GetClaimToFieldMapping(claimType);
                
                var userFieldValue = user.GetType().GetProperty(fieldName)?.GetValue(user, null) as string;
                
                userClaims.Add(new Claim(claimType, userFieldValue));
            }
        }

        return userClaims;
    }
    
    private string GetClaimToFieldMapping(string claim)
    {
        var field = ClaimToFieldMappings.FirstOrDefault(kvp => kvp.Key == claim);
        return field.Value;
    }
    
    private Dictionary<string, string> ClaimToFieldMappings = new()
    {
        { "given_name", "FirstName" },
        { "family_name", "LastName" }
    };
    
    public Task IsActiveAsync(IsActiveContext context)
    {
        return Task.CompletedTask;
    }
}