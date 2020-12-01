using System.Threading.Tasks;

namespace Core.Repositories
{
    public interface ILkkInvestmentRequestRepository
    {
        Task Add(string clientId, string requestId, string amount, string purchaseOption);
    }
}
