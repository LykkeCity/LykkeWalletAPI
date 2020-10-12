using Autofac;
using Repositories;
using AzureStorage.Tables;
using Core.Repositories;
using Lykke.Common.Log;
using Lykke.SettingsReader;

namespace LykkeApi2.Modules
{
    public class RepositoriesModule : Module
    {
        private readonly IReloadingManager<DbSettings> _dbSettings;

        public RepositoriesModule(
            IReloadingManager<DbSettings> dbSettings)
        {
            _dbSettings = dbSettings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(ctx =>
                new HistoryExportsRepository(AzureTableStorage<HistoryExportEntry>.Create(
                    _dbSettings.ConnectionString(x => x.DataConnString),
                    HistoryExportsRepository.TableName,
                    ctx.Resolve<ILogFactory>())))
                .As<IHistoryExportsRepository>()
                .SingleInstance();
        }
    }
}
