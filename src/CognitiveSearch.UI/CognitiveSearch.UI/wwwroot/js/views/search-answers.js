// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

//
// ANSWERS 
//
Microsoft = Microsoft || {};
Microsoft.Answers = Microsoft.Answers || {};
Microsoft.Answers = {

    UpdateAnswers: function (data) {
        
        Microsoft.Search.mrcAnswers = data.mrcAnswers;
        Microsoft.Search.qnaAnswers = data.qnaAnswers;
        Microsoft.Search.semantic_answers = data.semanticAnswers;

        this.UpdateQnAAnswers();
        this.UpdateSemanticAnswers();
    },

    UpdateSemanticAnswers: function () {
        $("#semantic-answer-content").empty();
        var answersHtml = this.renderSemanticAnswers(Microsoft.Search.semantic_answers);
        $("#semantic-answer-content").append(answersHtml);
        if (answersHtml.length>0)
        {
            $("#semantic-answer-content").removeClass("d-none");
        }
    },

    renderSemanticAnswers: function (semantic_answers) {
        var answersHtml = '';

        // Extractive Answers
        if (semantic_answers) {
            if (semantic_answers.length > 0) {

                answersHtml += '<h6 class="mt-2 text-success"><span class="bi bi-chat"></span> Semantic Answers</h6>';

                answersHtml += '<div class="">';
                answersHtml += '<div class="list-group list-group-horizontal mb-3">'

                var iconPath = Microsoft.Utils.GetIconPathFromExtension();

                for (var i = 0; i < semantic_answers.length; i++) {
                    var item = semantic_answers[i];

                    var index_key = item.key;

                    if (item.highlights) {
                        answersHtml += ' <a title="'+item.text+'" href="javascript:void(0)" onclick="Microsoft.Results.Details.ShowDocument(\'' + index_key + '\',0);" class="list-group-item list-group-item-action d-flex" style="color:inherit !important">' ;
                        answersHtml += '   <img title="'+item.text+'" class="image-result img-thumbnail" src="/api/search/getdocumentcoverimagebyindexkey?key=' + index_key + '" " onError="this.onerror=null;this.src=\'' + iconPath + '\';"/>';
                        answersHtml += ' <span class="ms-1">' + item.highlights + '</span>';
                        answersHtml += ' </a>';
                    }
                    else {
                        if (item.text) {
                            answersHtml += ' <a title="'+item.text+'" href="javascript:void(0)" onclick="Microsoft.Results.Details.ShowDocument(\'' + index_key + '\',0);" class="list-group-item list-group-item-action d-flex" style="color:inherit !important">' ;
                            answersHtml += '   <img title="'+item.text+'" class="image-result img-thumbnail" src="/api/search/getdocumentcoverimagebyindexkey?key=' + index_key + '" onError="this.onerror=null;this.src=\'' + iconPath + '\';"/>';
                            answersHtml += ' <span class="ms-1">' + item.text + '</span>';
                            answersHtml += ' </a>';
                        }
                    }
                }
                answersHtml += '</div>';
                answersHtml += '</div>';
            }
        }

        return answersHtml;
    },

    UpdateQnAAnswers: function () {

        // QnA Answers
        $("#qna-answer-content").empty();

        var answersHtml = '';

        if (Microsoft.Search.qnaAnswers) {
            if (Microsoft.Search.qnaAnswers.length > 0) {

                answersHtml += '<h6 style="color:darkgreen;"><span class="bi bi-chat-left-text-fill"></span> QnA Answers</h6>';
                answersHtml += '<div class="row">';

                for (var i = 0; i < Microsoft.Search.qnaAnswers.length; i++) {
                    var item = Microsoft.Search.qnaAnswers[i];
                    if (Microsoft.Search.qnaAnswers.length > 1) {
                        answersHtml += '<div class="card col-md-4" style="flex-direction:row; border:none !important">';
                    }
                    else {
                        answersHtml += '<div class="card col-md-8" style="flex-direction:row; border:none !important">';
                    }
                    answersHtml += '<div class="card-heading-answer">';

                    var title = item.source;

                    answersHtml += '<i class="oi oi-question-mark" style="width: 24px;height: 24px;"></span>';
                    answersHtml += '</div>';

                    answersHtml += '<div class="card-body" style="border-top: solid;border-top-color: darkgreen;">';
                    answersHtml += '<span class="bi bi-blockquote-left"></span>';
                    answersHtml += '    <a target="_blank" href="' + item.source + '" class="card-title" style="color:inherit !important" title="' + title + '">...' + item.answer + ' </a>';
                    answersHtml += '<span class="bi bi-blockquote-right"></span>';
                    answersHtml += '<hr class="hr-answer">';
                    answersHtml += '<h7 class="card-subtitle mb-2 text-muted">Source: ';
                    answersHtml += '    <a target="_blank" href="' + item.source + '" >' + item.source + '</a>';
                    answersHtml += '</h7>';
                    answersHtml += '</div>';

                    answersHtml += '</div>';
                }

                answersHtml += '</div>';
                answersHtml += '<hr>';
            }
        }

        $("#qna-answer-content").append(answersHtml);

        if (answersHtml.length>0)
        {
            $("#qna-answer-content").removeClass("d-none");
        }
    },

    AnswersSearch: function(query) {
    
        Microsoft.Search.setQueryInProgress();
    
        if (query !== undefined && query !== null) {
            $("#q").val(query)
        }
        // if (query !== undefined) {
        //     $("#q").val(query)
        // }
    
        if (Microsoft.Search.currentPage > 0) {
            if (Microsoft.View.currentQuery !== $("#q").val()) {
                Microsoft.Search.ResetSearch();
            }
        }
        Microsoft.View.currentQuery = $("#q").val();
    
        // Get center of map to use to score the search results
        $.postJSON('/api/answers/getanswers',
            {
                queryText: Microsoft.View.currentQuery !== undefined ? Microsoft.View.currentQuery : "*",
                searchFacets: Microsoft.Facets.selectedFacets,
                currentPage: ++Microsoft.Search.currentPage,
                parameters: Microsoft.Search.Parameters,
                options: Microsoft.Search.Options
            },
            function (data) {
                Microsoft.Answers.AnswersUpdate(data, Microsoft.Search.currentPage);
            });
    },
    
    AnswersUpdate: function(data, currentPage) {
        Microsoft.Search.results = data.value;
        Microsoft.Search.semantic_answers = data["@search.answers"];
        //facets = data.facets;
        //tags = data.tags;
        Microsoft.Search.tokens = data.tokens;
        Microsoft.View.searchId = data.searchId;
    
        Microsoft.Search.TotalCount = data["@odata.count"];
        Microsoft.Search.MaxPageCount = 50;
    
        ////Facets
        //UpdateFacets();
    
        Microsoft.Search.UpdateDocCount(Microsoft.Search.TotalCount);
        //Results List
        Microsoft.Answers.UpdateAnswersResults(Microsoft.Search.results, currentPage);
    
        // Log Search Events
        Microsoft.Telemetry.LogSearchAnalytics(Microsoft.Search.TotalCount);
    
        //Filters
        Microsoft.Facets.UpdateFilterReset();
    
        Microsoft.Search.setQueryCompleted();
    },
    
    UpdateAnswersResults: function(results, currentPage) {
    
        var resultsHtml = '';
        var classList = "answer-result-div";
    
        resultsHtml += Microsoft.Answers.renderSemanticAnswers(Microsoft.Search.semantic_answers);
    
        if (results && results.length > 0) {
    
            var answersHtml = '';
            answersHtml += '<h6 class="text-success"><span class="bi bi-card-text"></span> Semantic Ranked results</h6>';
            answersHtml += '<div class="row">';
    
            for (var i = 0; i < results.length; i++) {
    
                var docresult = results[i];
    
                //docresult.idx = i;
                Microsoft.Search.results_keys_index.push(docresult.index_key);
                docresult.idx = Microsoft.Search.results_keys_index.length - 1;
    
                var id = docresult.index_key;
                var name = docresult.document_filename;
                var path = docresult.metadata_storage_path;
    
                var captions = docresult["@search.captions"];
    
                if (path !== null) {
                    var pathLower = path.toLowerCase();
                    var pathExtension = pathLower.split('.').pop();
    
                    answersHtml += '<div class="card col-md-4" style="flex-direction:row; border:none !important">';
    
                    // Card Header Answer 
                    answersHtml += '<div class="card-heading-answer">';
    
                    var iconPath = Microsoft.Utils.GetIconPathFromExtension(pathExtension);
    
                    answersHtml += '<img style="width: 24px;height: 24px;" src="' + iconPath + '"/>';
                    answersHtml += '</div>';
    
                    // Card Body
                    answersHtml += '<div class="card-body" style="border-top: solid;border-top-color: darkgreen;">';
    
                    if (captions) {
                        for (var j = 0; j < captions.length; j++) {
                            var answer = captions[j];
                            if (answer.highlights){
                                answersHtml += '<span>' + answer.highlights + ' </span>';
                            }
                            else{
                                answersHtml += '<span>' + answer.text + ' </span>';
                            }
                        }
                    }
    
                    answersHtml += '<br>';
    
                    var src = Microsoft.Search.GetSASTokenFromPath(path); 
    
                    if (Microsoft.Utils.images_extensions.includes(pathExtension)) {
                        answersHtml += '<a target="_blank" href="' + src + '" >';
                        answersHtml += '<img class="image-result" src="data:image/png;base64, ' + docresult.image.thumbnail_medium + '" title="' + Base64.decode(docresult.image_parentfilename) + '" />';
                        answersHtml += '</a>';
                    }
                    else {
                        answersHtml += '<h5 class="card-subtitle mb-2 text-muted">';
                        if (Microsoft.Utils.IsOfficeDocument(pathExtension)) {
                            src = "https://view.officeapps.live.com/op/view.aspx?src=" + encodeURIComponent(Microsoft.Search.GetSASTokenFromPath(path));
                        }
                        answersHtml += '<a target="_blank" href="' + src + '" >' + name;
                        answersHtml += '    <img class="image-result" src="/api/search/getdocumentcoverimage?id=' + docresult.document_id + '"/>';
                        answersHtml += '</a>';
                        answersHtml += '</h5>';
                    }
    
                    // Actions
                    answersHtml += Microsoft.Search.Actions.renderActions(docresult, "flex");
    
                    answersHtml += '</div>';
                    answersHtml += '</div>';
                }
            }
    
            answersHtml += '</div>';
    
            resultsHtml += '<hr>';
            resultsHtml += answersHtml;
        }
    
        $("#semantic-answer-content").html(resultsHtml);
    }

}
