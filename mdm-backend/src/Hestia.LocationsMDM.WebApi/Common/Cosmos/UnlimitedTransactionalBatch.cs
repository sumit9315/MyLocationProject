using Hestia.LocationsMDM.WebApi.Exceptions;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Hestia.LocationsMDM.WebApi.Common
{
    public class UnlimitedTransactionalBatch
    {
        const int MaxOperationCount = 100;

        private readonly Func<TransactionalBatch> _batchCreator;

        private readonly IList<TransactionalBatchCounter> _batches = new List<TransactionalBatchCounter>();

        public UnlimitedTransactionalBatch(Func<TransactionalBatch> batchCreator)
        {
            _batchCreator = batchCreator;
        }

        public void CreateItem(JObject obj)
        {
            const int operationCount = 1;

            var batchCounter = GetBatchCounter(operationCount);

            batchCounter.OperationCount += operationCount;
            batchCounter.Batch
                .CreateItem(obj);
        }

        public void UpdateItem(JObject oldObj, JObject newObj)
        {
            const int operationCount = 2;

            var batchCounter = GetBatchCounter(operationCount);

            batchCounter.OperationCount += operationCount;
            batchCounter.Batch
                .ReplaceItemWithConcurrency(oldObj)
                .CreateItem(newObj);
        }

        public void UpsertItem(JObject obj)
        {
            const int operationCount = 1;

            var batchCounter = GetBatchCounter(operationCount);

            batchCounter.OperationCount += operationCount;
            batchCounter.Batch
                .UpsertItem(obj);
        }

        public async Task ExecuteAsync()
        {
            foreach (var batch in _batches)
            {
                var result = await batch.Batch.ExecuteAsync();
                if (result.StatusCode == HttpStatusCode.PreconditionFailed)
                {
                    throw new DataConflictException("Resource was updated by someone else, please reload resource and try again.");
                }
                else if (!result.IsSuccessStatusCode)
                {
                    throw new ServiceException($"Couldn't update resource. {result.ErrorMessage}");
                }
            }
        }

        private TransactionalBatchCounter GetBatchCounter(int operationCount)
        {
            TransactionalBatchCounter batchCounter;
            if (_batches.Count == 0 || _batches[_batches.Count - 1].OperationCount + operationCount > MaxOperationCount)
            {
                var newBatch = _batchCreator();
                batchCounter = new TransactionalBatchCounter(newBatch);
                _batches.Add(batchCounter);
            }

            return _batches[_batches.Count - 1];
        }
    }
}
