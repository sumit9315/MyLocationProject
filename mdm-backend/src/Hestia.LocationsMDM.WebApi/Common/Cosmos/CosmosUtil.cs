/*
 * Copyright (c) 2020, TopCoder, Inc. All rights reserved.
 */
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json.Linq;

namespace Hestia.LocationsMDM.WebApi.Common
{
    /// <summary>
    /// This class contains Cosmos DB client helper methods.
    /// </summary>
    internal static class CosmosUtil
    {
        /// <summary>
        /// Replaces the item using optimistic concurrency settings.
        /// </summary>
        /// <param name="batch">The transactional batch.</param>
        /// <param name="jObj">The source object that will be updated.</param>
        /// <returns>The updated transactional batch.</returns>
        public static TransactionalBatch ReplaceItemWithConcurrency(this TransactionalBatch batch,  JToken jObj)
        {
            // get object Id
            string id = jObj["id"].ToString();

            // set Concurrency handling
            var requestOptions = new TransactionalBatchItemRequestOptions
            {
                IfMatchEtag = jObj["_etag"].ToString()
            };

            batch.ReplaceItem(id, jObj, requestOptions);
            return batch;
        }

        public static string EscapeSingleQuotes(this string paramValue)
        {
            return paramValue?.Replace("\'", "\\'");
        }
    }
}
