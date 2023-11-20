export interface SearchRequest {
    queryText: string;
    searchFacets: any[];
    currentPage: number;
    incomingFilter: string;
    parameters: {
        scoringProfile: string;
        inOrderBy: string[];
    };
    options: {
        isSemanticSearch: boolean;
        isQueryTranslation: boolean;
        isQuerySpellCheck: boolean;
        suggestionsAsFilter: boolean;
        orMVRefinerOperator: boolean;
    };
}
