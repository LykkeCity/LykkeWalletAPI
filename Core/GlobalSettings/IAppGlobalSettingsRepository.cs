using System.Threading.Tasks;

namespace Core.GlobalSettings
{
    public interface IAppGlobalSettingsRepository
    {
        Task<IAppGlobalSettings> GetFromDbOrDefault();
    }
}