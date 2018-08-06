using Autofac;
using Common.Log;
using Repositories;
using Core.Settings;
using AzureStorage.Tables;
using Lykke.SettingsReader;

namespace LykkeApi2.Modules
{
    public class RepositoriesModule : Module
    {
        private IReloadingManager<DbSettings> _dbSettings;
        private ILog _log;
        
        public RepositoriesModule(
            IReloadingManager<DbSettings> dbSettings,
            ILog log)
        {
            _dbSettings = dbSettings;
            _log = log;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(ctx =>
                new HistoryExportsRepository(AzureTableStorage<HistoryExportEntry>.Create(
                    _dbSettings.ConnectionString(x => x.LogsConnString),
                    HistoryExportsRepository.TableName,
                    _log)));
        }
    }
}