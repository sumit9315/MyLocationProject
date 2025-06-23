using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hestia.LocationsMDM.WebApi.Common
{
    public class TransactionalBatchCounter
    {
        public TransactionalBatch Batch { get; set; }

        public int OperationCount { get; set; }

        public TransactionalBatchCounter(TransactionalBatch batch)
        {
            Batch = batch;
        }
    }
}
