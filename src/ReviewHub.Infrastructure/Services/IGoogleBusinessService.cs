using ReviewHub.Core.Entities;

namespace ReviewHub.Infrastructure.Services;

public interface IGoogleBusinessService
{
    Task<string> GetAuthorizationUrlAsync(int userId, int businessId);
    Task<string> ExchangeCodeForTokenAsync(string code, string state);
    Task<List<Review>> FetchReviewsAsync(int platformConnectionId);
    Task RefreshAccessTokenAsync(int platformConnectionId);
}
