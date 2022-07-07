// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Knowledge.Services.Configuration;
using Knowledge.Services.Helpers;
using Knowledge.Services.Models;
using Knowledge.Services.Models.Ingress;
using Knowledge.Services.QnA;
using Knowledge.Services.SemanticSearch;
using Knowledge.Services.SpellChecking;
using Knowledge.Services.Translation;
using Knowledge.Services.WebSearch;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Knowledge.Services
{
    public class QueryService : IQueryService
    {
        protected const string QUERY_ALL = QueryHelper.QUERY_ALL;

        private IDistributedCache memCache;
        private ISpellCheckingService spellcheckService;
        private ITranslationService translationService;
        private IQnAService qnaService;
        private ISemanticSearchService semanticService;
        private IWebSearchService webSearchService;
        private IAzureSearchService searchService;

        SearchServiceConfig config; 

        public QueryService(SearchServiceConfig configuration,
            IDistributedCache memoryCache,
            IAzureSearchService searchSvc,
            IQnAService qnaService,
            ISpellCheckingService spellcheckService,
            ITranslationService translationService,
            ISemanticSearchService semanticService,
            IWebSearchService websvc)
        {
            try
            {
                this.memCache = memoryCache;
                this.config = configuration; 

                this.searchService = searchSvc;
                this.spellcheckService = spellcheckService;
                this.translationService = translationService;
                this.qnaService = qnaService;
                this.semanticService = semanticService;
                this.webSearchService = websvc; 
            }
            catch (Exception e)
            {
                // If you get an exception here, most likely you have not set your
                // credentials correctly in appsettings.json
                throw new ArgumentException(e.Message.ToString());
            }
        }

        public async Task TransformQuery(IngressSearchRequest request)
        {
            QueryHelper.EnsureDefaultValues(request);

            request.AddQueryTransformation("0-Original", request.queryText);

            if (request.queryText != QUERY_ALL)
            {
                // Bing Query SpellChecking
                // TODO Support the new ACS built-in spellchecker
                if (request.options.isQuerySpellCheck)
                {
                    request.queryText = await spellcheckService.SpellCheckAsync(request.queryText);
                    request.AddQueryTransformation("1-SpellCheck", request.queryText);
                }

                // Query Translation
                if (request.options.isQueryTranslation)
                {
                    request.queryText = translationService.TranslateSearchText(request.queryText);
                    request.AddQueryTransformation("2-Translation", request.queryText);
                }
            }
        }

        public async Task<SearchResponse> GetFederatedDocumentsAsync(IngressSearchRequest request)
        {
            await TransformQuery(request);

            var searchTask = Task.Run(async () => await this.searchService.GetDocumentsAsync(request));
            //var searchTask = Task.Run(async () => await this.searchService.GetDocumentsAsync(q, searchFacets, currentPage, polygonString));
            //var answersTask = Task.Run(async () => await this.mrcService.GetAnswerAsync(q, searchId));
            //var qnaTask = Task.Run(async () => await this.qnaService.GetQnaAnswersAsync(q, searchId));

            SearchResponse result;

            Task.WaitAll(searchTask);

            result = searchTask.Result;

            result.QueryTransformations = request.GetQueryTransformation();

            return result;
        }

        public async Task<SearchResponse> GetDocumentsAsync(IngressSearchRequest request)
        {
            await TransformQuery(request);

            SearchResponse result = await this.searchService.GetDocumentsAsync(request);

            result.QueryTransformations = request.GetQueryTransformation();

            return result;
        }

        public string GetSearchId()
        {
            return this.searchService.GetSearchId();
        }

        /// <summary>
        /// Initiates a run of the search indexer.
        /// </summary>
        public void RunIndexers()
        {
            this.searchService.RunIndexers();
        }

        public async Task RunIndexer(string indexerName)
        {
            await this.searchService.RunIndexer(indexerName);
        }

        public async Task<SearchResponse> GetDocumentByIndexKey(IngressSearchRequest request)
        {
            QueryHelper.EnsureDefaultValues(request);

            return await this.searchService.GetDocumentByIndexKey(request);
        }

        public async Task<SearchResponse> GetDocumentById(IngressSearchRequest request)
        {
            QueryHelper.EnsureDefaultValues(request);

            return await this.searchService.GetDocumentById(request);
        }

        public async Task<SearchResponse> GetDocumentEmbedded(IngressSearchRequest request)
        {
            QueryHelper.EnsureDefaultValues(request);

            return await this.searchService.GetDocumentEmbedded(request);
        }

        public async Task<SearchResponse> GetDocumentCoverImage(IngressSearchRequest request)
        {
            QueryHelper.EnsureDefaultValues(request);

            return await this.searchService.GetDocumentCoverImage(request);
        }

        public async Task<SearchResponse> GetImagesAsync(IngressSearchRequest request)
        {
            await TransformQuery(request);

            SearchResponse result = await this.searchService.GetImagesAsync(request);

            result.QueryTransformations = request.GetQueryTransformation();

            return result;
        }
        public async Task<SearchResponse> GetLatestImagesAsync(IngressSearchRequest request)
        {
            QueryHelper.EnsureDefaultValues(request);

            SearchResponse result = await this.searchService.GetLatestImagesAsync(request);

            return result;
        }

        public async Task<SearchResponse> GetVideosAsync(IngressSearchRequest request)
        {
            await TransformQuery(request);

            SearchResponse result = await this.searchService.GetVideosAsync(request);

            result.QueryTransformations = request.GetQueryTransformation();

            return result;
        }

        public async Task<List<string>> AutocompleteAsync(IngressSuggestionRequest request)
        {
            return await this.searchService.AutocompleteAsync(request);
        }

        public async Task<List<string>> SuggestAsync(IngressSuggestionRequest request)
        {
            return await this.searchService.SuggestAsync(request);
        }

        public async Task<SearchResponse> GetLatestDocumentsAsync(IngressSearchRequest request)
        {
            QueryHelper.EnsureDefaultValues(request);

            SearchResponse result = await this.searchService.GetLatestDocumentsAsync(request);

            return result;
        }

        public async Task<SearchResponse> GetDocumentSiblings(IngressSearchRequest request)
        {
            QueryHelper.EnsureDefaultValues(request);

            SearchResponse result = await this.searchService.GetDocumentSiblings(request);

            return result;
        }
    }
}
