using Autofac;
using Common.Log;
using Repositories;
using Core.Settings;
using AzureStorage.Tables;
using Core.Repositories;
using Lykke.SettingsReader;

namespace LykkeApi2.Modules
{
    public class RepositoriesModule : Module
    {
        private readonly IReloadingManager<DbSettings> _dbSettings;
        private readonly ILog _log;
        
        public RepositoriesModule(
            IReloadingManager<DbSettings> dbSettings,
            ILog log)
        {
            _dbSettings = dbSettings;
            _log = log;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance<IHistoryExportsRepository>(
                new HistoryExportsRepository(AzureTableStorage<HistoryExportEntry>.Create(
                    _dbSettings.ConnectionString(x => x.DataConnString),
                    HistoryExportsRepository.TableName,
                    _log)));
        }
    }
}