using System.Threading.Tasks;

namespace Core.Messages
{
    public interface IVerifiedEmailsRepository
    {
        Task AddOrReplaceAsync(string email, string partnerId);
        Task<bool> IsEmailVerified(string email, string partnerId);
    }
}
