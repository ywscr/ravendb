﻿using System;
using System.Collections.Generic;
using Raven.Client.Extensions;
using Raven.Client.ServerWide;
using Sparrow.Json.Parsing;

namespace Raven.Client.Documents.Operations.ETL
{
    public class EtlProcessState : IDatabaseTaskStatus
    {
        public EtlProcessState()
        {
            LastProcessedEtagPerDbId = new Dictionary<string, long>();
            ChangeVector = null;
        }

        public string ConfigurationName { get; set; }

        public string TransformationName { get; set; }

        public Dictionary<string, long> LastProcessedEtagPerDbId { get; set; }

        [Obsolete("Was used in older versions, replaced by 'LastProcessedEtagPerDbId'")]
        public Dictionary<string, long> LastProcessedEtagPerNode { get; set; }

        public string ChangeVector { get; set; }

        public string NodeTag { get; set; }

        // In a regular case we load time-series only when the time-series change.
        // When there is time-series without document (that can happen if the time-series was replicated but its document didn't yet)
        // we mark the time-series of the document as skipped so when we load the document we will load all its time-series with it
        public HashSet<string> SkippedTimeSeriesDocs { get; set; }
        
        public long GetLastProcessedEtag(string dbId, string nodeTag)
        {
            if (LastProcessedEtagPerDbId.TryGetValue(dbId, out var etag))
                return etag;

#pragma warning disable 618
            if (LastProcessedEtagPerNode != null && LastProcessedEtagPerNode.TryGetValue(nodeTag, out etag))
#pragma warning restore 618
                // legacy EtlProcessState
                return etag;
            
            return 0;
        }

        public DynamicJsonValue ToJson()
        {
            var json = new DynamicJsonValue
            {
                [nameof(ConfigurationName)] = ConfigurationName,
                [nameof(TransformationName)] = TransformationName,
                [nameof(LastProcessedEtagPerDbId)] = LastProcessedEtagPerDbId.ToJson(),
#pragma warning disable 618
                [nameof(LastProcessedEtagPerNode)] = LastProcessedEtagPerNode?.ToJson(),
#pragma warning restore 618
                [nameof(ChangeVector)] = ChangeVector,
                [nameof(NodeTag)] = NodeTag,
                [nameof(SkippedTimeSeriesDocs)] = SkippedTimeSeriesDocs,
            };

            return json;
        }

        public static string GenerateItemName(string databaseName, string configurationName, string transformationName)
        {
            return $"values/{databaseName}/etl/{configurationName.ToLowerInvariant()}/{transformationName.ToLowerInvariant()}";
        }
    }
}
