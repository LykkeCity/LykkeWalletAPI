using System;
using System.Threading.Tasks;
using Common;
using Core.Repositories;
using Repositories;
using Lykke.Job.HistoryExportBuilder.Contract.Events;

namespace LykkeApi2.Cqrs.Projections
{
    public class HistoryExportProjection
    {
        private readonly IHistoryExportsRepository _repository;
        
        public HistoryExportProjection(IHistoryExportsRepository repository)
        {
            _repository = repository;
        }
        
        public Task Handle(ClientHistoryExpiredEvent evt)
        {
            return _repository.Remove(evt.ClientId, evt.Id);
        }
        
        public Task Handle(ClientHistoryExportedEvent evt)
        {
            return _repository.Add(evt.ClientId, evt.Id, evt.Uri.ToString());
        }
    }
}