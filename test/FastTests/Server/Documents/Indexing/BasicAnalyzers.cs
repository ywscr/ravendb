﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Analysis;
using Raven.Client;
using Raven.Client.Documents.Indexes;
using Raven.Server.Documents;
using Raven.Server.Documents.Includes;
using Raven.Server.Documents.Indexes;
using Raven.Server.Documents.Indexes.Persistence.Lucene;
using Raven.Server.Documents.Indexes.Persistence.Lucene.Analyzers;
using Raven.Server.Documents.Indexes.Workers;
using Raven.Server.Documents.Queries;
using Raven.Server.Documents.Queries.Results;
using Raven.Server.Documents.Queries.Timings;
using Raven.Server.ServerWide.Context;
using Sparrow.Json;
using Sparrow.Logging;
using Sparrow.Server.Json.Sync;
using Voron;
using Xunit;
using Xunit.Abstractions;
using Index = Raven.Server.Documents.Indexes.Index;

namespace FastTests.Server.Documents.Indexing
{
    public class BasicAnalyzers : NoDisposalNeeded
    {
        public BasicAnalyzers(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void CheckAnalyzers()
        {
            var operation = new TestOperation("test", null);

            var fields = new Dictionary<string, IndexField>();
            fields.Add(Constants.Documents.Indexing.Fields.AllFields, new IndexField());

            Assert.Throws<InvalidOperationException>(() => operation.GetAnalyzer(fields, forQuerying: false));

            fields.Clear();
            fields.Add(Constants.Documents.Indexing.Fields.AllFields, new IndexField { Analyzer = "StandardAnalyzer" });
            Assert.Throws<InvalidOperationException>(() => operation.GetAnalyzer(fields, forQuerying: false));

            fields.Clear();
            fields.Add("Field1", new IndexField { Analyzer = "StandardAnalyzer" }); // field must be 'NotAnalyzed' or 'Analyzed'
            var analyzer = operation.GetAnalyzer(fields, forQuerying: false);

            Assert.IsType<LowerCaseKeywordAnalyzer>(analyzer.GetAnalyzer(string.Empty));
            Assert.IsType<LowerCaseKeywordAnalyzer>(analyzer.GetAnalyzer("Field1"));

            fields.Clear();
            fields.Add("Field1", new IndexField { Name = "Field1", Analyzer = "StandardAnalyzer", Indexing = FieldIndexing.Exact }); // 'NotAnalyzed' => 'KeywordAnalyzer'
            analyzer = operation.GetAnalyzer(fields, forQuerying: false);

            Assert.IsType<LowerCaseKeywordAnalyzer>(analyzer.GetAnalyzer(string.Empty));
            Assert.IsType<KeywordAnalyzer>(analyzer.GetAnalyzer("Field1"));

            fields.Clear();
            fields.Add("Field1", new IndexField { Name = "Field1", Analyzer = null, Indexing = FieldIndexing.Search }); // 'Analyzed = null' => 'StandardAnalyzer'
            analyzer = operation.GetAnalyzer(fields, forQuerying: false);

            Assert.IsType<LowerCaseKeywordAnalyzer>(analyzer.GetAnalyzer(string.Empty));
            Assert.IsType<RavenStandardAnalyzer>(analyzer.GetAnalyzer("Field1"));

            fields.Clear();
            fields.Add("Field1", new IndexField { Name = "Field1", Analyzer = typeof(NotForQueryingAnalyzer).AssemblyQualifiedName, Indexing = FieldIndexing.Search });
            analyzer = operation.GetAnalyzer(fields, forQuerying: false);

            Assert.IsType<LowerCaseKeywordAnalyzer>(analyzer.GetAnalyzer(string.Empty));
            Assert.IsType<NotForQueryingAnalyzer>(analyzer.GetAnalyzer("Field1"));

            fields.Clear();
            fields.Add("Field1", new IndexField { Name = "Field1", Analyzer = typeof(NotForQueryingAnalyzer).AssemblyQualifiedName, Indexing = FieldIndexing.Search });
            analyzer = operation.GetAnalyzer(fields, forQuerying: true);

            Assert.IsType<LowerCaseKeywordAnalyzer>(analyzer.GetAnalyzer(string.Empty));
            Assert.IsType<RavenStandardAnalyzer>(analyzer.GetAnalyzer("Field1"));

            fields.Clear();
            fields.Add("Field1", new IndexField { Name = "Field1", Analyzer = typeof(NotForQueryingAnalyzer).AssemblyQualifiedName, Indexing = FieldIndexing.Search });
            fields.Add("Field2", new IndexField { Name = "Field2", Analyzer = "KeywordAnalyzer", Indexing = FieldIndexing.Search });
            analyzer = operation.GetAnalyzer(fields, forQuerying: false);

            Assert.IsType<LowerCaseKeywordAnalyzer>(analyzer.GetAnalyzer(string.Empty));
            Assert.IsType<NotForQueryingAnalyzer>(analyzer.GetAnalyzer("Field1"));
            Assert.IsType<KeywordAnalyzer>(analyzer.GetAnalyzer("Field2"));
        }

        private class TestOperation : IndexOperationBase
        {
            public TestOperation(string indexName, Logger logger) : base(new TestIndex(), logger)
            {
            }

            public RavenPerFieldAnalyzerWrapper GetAnalyzer(Dictionary<string, IndexField> fields, bool forQuerying)
            {
                return CreateAnalyzer(() => new LowerCaseKeywordAnalyzer(), new TestIndexDefinitions
                {
                    IndexFields = fields
                }, forQuerying);
            }

            public override void Dispose()
            {
            }
        }

        [NotForQuerying]
        private class NotForQueryingAnalyzer : Analyzer
        {
            public override TokenStream TokenStream(string fieldName, TextReader reader)
            {
                throw new System.NotImplementedException();
            }
        }
    }

    internal class TestIndex : Index
    {
        public TestIndex() : base(IndexType.None, IndexSourceType.None, new TestIndexDefinitions())
        {
        }

        protected override IIndexingWork[] CreateIndexWorkExecutors()
        {
            throw new NotImplementedException();
        }

        public override IIndexedItemEnumerator GetMapEnumerator(IEnumerable<IndexItem> documents, string collection, TransactionOperationContext indexContext, IndexingStatsScope stats, IndexType type)
        {
            throw new NotImplementedException();
        }

        public override void HandleDelete(Tombstone tombstone, string collection, Lazy<IndexWriteOperation> writer, TransactionOperationContext indexContext, IndexingStatsScope stats)
        {
            throw new NotImplementedException();
        }

        public override int HandleMap(IndexItem indexItem, IEnumerable mapResults, Lazy<IndexWriteOperation> writer, TransactionOperationContext indexContext, IndexingStatsScope stats)
        {
            throw new NotImplementedException();
        }

        public override (ICollection<string> Static, ICollection<string> Dynamic) GetEntriesFields()
        {
            throw new NotImplementedException();
        }

        public override IQueryResultRetriever GetQueryResultRetriever(IndexQueryServerSide query, QueryTimingsScope queryTimings, DocumentsOperationContext documentsContext, FieldsToFetch fieldsToFetch,
            IncludeDocumentsCommand includeDocumentsCommand, IncludeCompareExchangeValuesCommand includeCompareExchangeValuesCommand)
        {
            throw new NotImplementedException();
        }

        public override void SaveLastState()
        {
            throw new NotImplementedException();
        }
    }

    internal class TestIndexDefinitions : IndexDefinitionBase
    {
        public override long Version => IndexVersion.CurrentVersion;

        public TestIndexDefinitions()
        {
            Collections = new HashSet<string> { Constants.Documents.Collections.AllDocumentsCollection };
        }

        public override void Persist(TransactionOperationContext context, StorageEnvironmentOptions options)
        {
            throw new NotImplementedException();
        }

        protected override void PersistMapFields(JsonOperationContext context, AbstractBlittableJsonTextWriter writer)
        {
            throw new NotImplementedException();
        }

        protected override void PersistFields(JsonOperationContext context, AbstractBlittableJsonTextWriter writer)
        {
            throw new NotImplementedException();
        }

        protected internal override IndexDefinition GetOrCreateIndexDefinitionInternal()
        {
            throw new NotImplementedException();
        }

        public override IndexDefinitionCompareDifferences Compare(IndexDefinitionBase indexDefinition)
        {
            throw new NotImplementedException();
        }

        public override IndexDefinitionCompareDifferences Compare(IndexDefinition indexDefinition)
        {
            throw new NotImplementedException();
        }

        internal override void Reset()
        {
            throw new NotImplementedException();
        }
    }
}
