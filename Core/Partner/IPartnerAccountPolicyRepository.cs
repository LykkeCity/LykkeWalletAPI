using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Partner
{
    public interface IPartnerAccountPolicyRepository
    {
        Task CreateAsync(IPartnerAccountPolicy partner);
        Task CreateOrUpdateAsync(IPartnerAccountPolicy partner);
        Task UpdateAsync(IPartnerAccountPolicy partner);
        Task<IEnumerable<IPartnerAccountPolicy>> GetPoliciesAsync();
        Task<IPartnerAccountPolicy> GetAsync(string publicId);
        Task RemoveAsync(string publicId);
    }
}
