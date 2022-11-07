// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Home 
//
Microsoft.All = Microsoft.All || {};
Microsoft.All = {
    MAX_NUMBER_ITEMS_PER_PAGE: 10,
    view_result_tag: "#search-results-content",

    Search: function (query) {

        Microsoft.Search.setQueryInProgress();

        if (query !== undefined && query !== null) {
            $("#q").val(query)
        }

        // New query ? 
        if (Microsoft.Search.currentPage > 0) {
            if (Microsoft.View.currentQuery !== $("#q").val()) {
                Microsoft.Search.ResetSearch();
            }
            else {
                // This is the pagination case although Semantic search has no pagination capability (50 results max). 
                if (Microsoft.Search.Options.isSemanticSearch) {
                    Microsoft.Search.setQueryCompleted();
                    return;
                }
            }
        }

        Microsoft.View.currentQuery = $("#q").val();

        var rendering_filter = Microsoft.View.config.filter ? Microsoft.View.config.filter : '';

        if (Microsoft.Search.results_rendering > -1) {
            if (Microsoft.View.config.resultsRenderings[Microsoft.Search.results_rendering].filter) {
                if (rendering_filter.length > 0) {
                    rendering_filter += ' and ';
                }
                rendering_filter += Microsoft.View.config.resultsRenderings[Microsoft.Search.results_rendering].filter;
            }
        }

        $.postAPIJSON('/api/search/getdocuments',
            {
                queryText: Microsoft.View.currentQuery !== undefined ? Microsoft.View.currentQuery : "*",
                searchFacets: Microsoft.Facets.selectedFacets,
                currentPage: ++Microsoft.Search.currentPage,
                incomingFilter: rendering_filter,
                parameters: Microsoft.Search.Parameters,
                options: Microsoft.Search.Options
            },
            function (data) {
                Microsoft.All.Update(data, Microsoft.Search.currentPage);
            });
    },

    Update: function (data, currentPage) {

        Microsoft.Search.ProcessSearchResponse(data,Microsoft.All.MAX_NUMBER_ITEMS_PER_PAGE);

        // RESULTS
        Microsoft.All.UpdateResults(Microsoft.Search.results, currentPage);
    },

    UpdateResults: function (results, currentPage, detailedView) {
        var resultsHtml = '';

        if (results && results.length > 0) {

            if (Microsoft.Search.results_rendering > -1) {
                var rendering = Microsoft.View.config.resultsRenderings[Microsoft.Search.results_rendering];
                resultsHtml += Microsoft.Utils.executeFunctionByName(rendering.method, window, results, currentPage, detailedView);
            }

            $(this.view_result_tag).append(resultsHtml);

            Microsoft.Search.ProcessCoverImage(); 
        }
        else {
            //No search result found
            if (!Microsoft.Search.hasMoreResults()) {
                if (results && results.length === 0) {
                    resultsHtml = '<div class="row results-div" >';
                    resultsHtml += '<div class="rounded">';
                    resultsHtml += '<div class="row">';
                    resultsHtml += '<center><h4>We couldn\'t find any results for this view...</h4></center>';
                    resultsHtml += '</div>';
                    resultsHtml += '</div>';
                    resultsHtml += '</div>';

                    $(this.view_result_tag).append(resultsHtml);
                }
            }
            //ResetSearch();
            Microsoft.Search.currentPage = 0;
            Microsoft.Search.results_keys_index = []

            //$(view_result_tag).empty();
        }
    },

    UpdateResultsAsList: function (results, currentPage, detailedView) {

        var resultsHtml = '';

        for (var i = 0; i < results.length; i++) {
            resultsHtml += Microsoft.Search.Results.RenderResultAsListItem(results[i]);
        }

        return resultsHtml;
    },

    UpdateResultsAsCard: function (results, currentPage, detailedView) {
        var resultsHtml = '';

        resultsHtml += '<div class="row">';

        var classList = "col results-div ";

        for (var i = 0; i < results.length; i++) {

            var docresult = results[i].Document !== undefined ? results[i].Document : results[i];

            Microsoft.Search.results_keys_index.push(docresult.index_key);
            docresult.idx = Microsoft.Search.results_keys_index.length - 1;

            var id = docresult.index_key;
            var path = docresult.metadata_storage_path;

            var tags = Microsoft.Tags.renderTagsAsTable(docresult, true, results[i].Highlights);

            var name = docresult.metadata_storage_name;

            if (docresult.title) name = docresult.title;

            if (path !== null) {

                var highlights = Microsoft.Search.ProcessHighlights(results[i]);

                var pathLower = path.toLowerCase();

                var pathExtension = pathLower.split('.').pop();

                //
                // Rendering the search result div 
                // 
                if (Microsoft.Utils.IsImageExtension(pathExtension)) {
                    var iconPath = Microsoft.Utils.GetIconPathFromExtension(pathExtension);

                    resultsHtml += '<div class="' + classList + '" onmouseover="Microsoft.Search.Actions.hideShowActions(\'' + id + '\');" onmouseout="Microsoft.Search.Actions.hideShowActions(\'' + id + '\');">';
                    resultsHtml += '    <div class="search-result">';
                    resultsHtml += '        <div class="results-body">';
                    // Header Row
                    resultsHtml += '            <div class="row">';
                    resultsHtml += '            <div class="col-md-2">';
                    resultsHtml += '            <div>';
                    resultsHtml += '                <span>';
                    resultsHtml += '                <a id="' + id + '" onclick="Microsoft.Utils.toggleCollapsable(\'' + id + '\')" >';

                    if (docresult.image.thumbnail_small) {
                        resultsHtml += '                        <img alt="' + name + '" style="width: 32px;height: 32px;" src="data:image/png;base64, ' + docresult.image.thumbnail_small + '"/>';
                    }
                    else {
                        resultsHtml += '                        <img alt="' + name + '" style="width: 32px;height: 32px;" src="' + iconPath + '"/>';
                    }

                    resultsHtml += '                </a>';
                    resultsHtml += '                </span>';
                    resultsHtml += '            </div>';
                    resultsHtml += '            </div>';

                    if (docresult.document_embedded) {

                        var imagename = docresult.metadata_storage_name;
                        var containerPath = Microsoft.Utils.GetParentPathFromImage(docresult);

                        resultsHtml += '<div class="col-md-10">';
                        resultsHtml += '<div class="results-header">';

                        resultsHtml += '<h5 title=\'' + path + '\'>';

                        resultsHtml += '<a target="_blank" href=\'' + Microsoft.Search.GetSASTokenFromPath(path) + '\'>' + Microsoft.Utils.GetImageFileTitle(docresult) + '</a>';
                        resultsHtml += ' - <a target="_blank" href=\'' + Microsoft.Search.GetSASTokenFromPath(containerPath) + '\'>' + Base64.decode(docresult.parent.filename) + '</a>';
                        resultsHtml += '</h5>';
                        resultsHtml += '</div>';
                        resultsHtml += '</div>';

                    }
                    else {
                        resultsHtml += '        <div class="results-header">';
                        resultsHtml += '            <h5 title=\'' + path + '\'><a target="_blank" href=\'' + Microsoft.Search.GetSASTokenFromPath(path) + '\'>' + name + '</a></h5>';
                        // resultsHtml += Microsoft.Utils.GetModificationLine(docresult);
                        resultsHtml += '        </div>';
                    }

                    resultsHtml += '        </div>';

                    // Image Row
                    resultsHtml += '        <div class="collapse show" id="result-' + id + '">';
                    resultsHtml += '            <div class="row col-md-12 " onclick="Microsoft.Results.Details.ShowDocument(\'' + id + '\',' + docresult.idx + ');">';
                    resultsHtml += Microsoft.Utils.GetModificationLine(docresult);

                    var display_title = docresult.document_embedded ? Base64.decode(docresult.parent.filename) : docresult.metadata_storage_name;

                    if (docresult.image.thumbnail_medium) {
                        resultsHtml += '<img alt="' + name + '" class="image-result" src="data:image/png;base64, ' + docresult.image.thumbnail_medium + '" title="' + display_title + '" />';
                    }
                    else {
                        resultsHtml += '<img alt="' + name + '" class="image-result" src="' + Microsoft.Search.GetSASTokenFromPath(path) + '" title="' + display_title + '" />';
                    }


                    resultsHtml += '            <div class="results-body mt-2">' + highlights + '</div>';

                    resultsHtml += '            </div>';
                    resultsHtml += '        </div>';

                    // Tags
                    resultsHtml += Microsoft.Tags.renderTags(id, tags);
                    // Actions
                    resultsHtml += Microsoft.Search.Actions.renderActions(docresult);

                    resultsHtml += '        </div>';
                    resultsHtml += '    </div>';
                    resultsHtml += '</div>';

                }
                else if (pathExtension === "mp3") {

                    resultsHtml += '<div class="' + classList + '" onmouseover="Microsoft.Search.Actions.hideShowActions(\'' + id + '\');" onmouseout="Microsoft.Search.Actions.hideShowActions(\'' + id + '\');">';
                    //resultsHtml += '<div class="' + classList + '" onclick="Microsoft.Results.Details.ShowDocument(\'' + id + '\',' + docresult.idx + ');">';
                    resultsHtml += '    <div class="search-result">';
                    resultsHtml += '        <div class="audio-result-div">';
                    resultsHtml += '            <audio controls>';
                    resultsHtml += '                <source src="' + Microsoft.Search.GetSASTokenFromPath(path) + '" type="audio/mp3">';
                    resultsHtml += '                    Your browser does not support the audio tag.';
                    resultsHtml += '            </audio>';
                    resultsHtml += '        </div>';
                    resultsHtml += '        <div class="results-header">';
                    resultsHtml += '            <h4 title="' + path + '"><a href=\'' + Microsoft.Search.GetSASTokenFromPath(path) + '\'>' + name + '</a></h4>';
                    resultsHtml += Microsoft.Utils.GetModificationLine(docresult);
                    resultsHtml += '        </div>';

                    // Tags
                    resultsHtml += Microsoft.Tags.renderTags(id, tags);
                    // Actions
                    resultsHtml += Microsoft.Search.Actions.renderActions(docresult);

                    resultsHtml += '    </div>';
                    resultsHtml += '</div>';
                }
                else if (pathExtension === "mp4") {

                    resultsHtml += '<div class="' + classList + '" onmouseover="Microsoft.Search.Actions.hideShowActions(\'' + id + '\');" onmouseout="Microsoft.Search.Actions.hideShowActions(\'' + id + '\');">';
                    //resultsHtml += '<div class="' + classList + '" onclick="Microsoft.Results.Details.ShowDocument(\'' + id + '\',' + docresult.idx + ');">';
                    resultsHtml += '    <div class="search-result">';
                    resultsHtml += '         <div class="video-result-div">';
                    resultsHtml += '            <video controls class="video-result">';
                    resultsHtml += '                <source src="' + Microsoft.Search.GetSASTokenFromPath(path) + '" type="video/mp4">';
                    resultsHtml += '                    Your browser does not support the video tag.';
                    resultsHtml += '            </video>';
                    resultsHtml += '        </div>';
                    resultsHtml += '        <hr />';
                    resultsHtml += '        <div class="results-header">';
                    resultsHtml += '            <h4 title=\'' + path + '\'><a href=\'' + Microsoft.Search.GetSASTokenFromPath(path) + '\'>' + name + '</a></h4>';
                    resultsHtml += Microsoft.Utils.GetModificationLine(docresult);
                    resultsHtml += '        </div>';

                    // Tags
                    resultsHtml += Microsoft.Tags.renderTags(id, tags);
                    // Actions
                    resultsHtml += Microsoft.Search.Actions.renderActions(docresult);

                    resultsHtml += '        </div>';
                    resultsHtml += '    </div>';
                    resultsHtml += '</div>';
                }
                else {
                    var iconPath = Microsoft.Utils.GetIconPathFromExtension(pathExtension);

                    resultsHtml += '<div class="' + classList + '" onmouseover="Microsoft.Search.Actions.hideShowActions(\'' + id + '\');" onmouseout="Microsoft.Search.Actions.hideShowActions(\'' + id + '\');">';

                    resultsHtml += '    <div class="search-result">';

                    resultsHtml += '        <div class="row">';

                    resultsHtml += '        <div class="col-md-2">';
                    resultsHtml += '            <div class="results-icon">';
                    resultsHtml += '            <div>';
                    resultsHtml += '                <i class="html-icon">';
                    resultsHtml += '                <a id="' + id + '" onclick = "Microsoft.Utils.toggleCollapsable(\'' + id + '\')">';
                    resultsHtml += '                    <img alt="' + name + '" style="width: 32px;height: 32px;" src="' + iconPath + '"/>';
                    resultsHtml += '                </a>';
                    resultsHtml += '                </span>';
                    resultsHtml += '            </div>';
                    resultsHtml += '            </div>';
                    resultsHtml += '        </div>';

                    resultsHtml += '        <div class="col-md-10">';

                    resultsHtml += '            <div class="results-body">';

                    if (Microsoft.Utils.IsOfficeDocument(pathExtension)) {
                        var src = "https://view.officeapps.live.com/op/view.aspx?src=" + encodeURIComponent(Microsoft.Search.GetSASTokenFromPath(path));
                        resultsHtml += '<h4 title=\'' + path + '\'><a target="_blank" href=\'' + src + '\'>' + name + '</a></h4>';
                    }
                    else {
                        resultsHtml += '<h4 title=\'' + path + '\'><a target="_blank" href=\'' + Microsoft.Search.GetSASTokenFromPath(path) + '\'>' + name + '</a></h4>';
                    }

                    resultsHtml += '            </div>';
                    resultsHtml += '        </div>';

                    // Closing row
                    resultsHtml += '    </div>';

                    resultsHtml += '    <div class="collapse show" id="result-' + id + '">';
                    resultsHtml += '        <div class="row col-md-12" onclick="Microsoft.Results.Details.ShowDocument(\'' + id + '\',' + docresult.idx + ');">';
                    resultsHtml += Microsoft.Utils.GetModificationLine(docresult);

                    resultsHtml += Microsoft.Search.RenderCoverImage(docresult,name,iconPath);

                    resultsHtml += '            <div class="results-body mt-2" >' + highlights + '</div>';

                    resultsHtml += '        </div>';
                    resultsHtml += '    </div>';

                    // Tags
                    resultsHtml += Microsoft.Tags.renderTags(id, tags);
                    // Actions
                    resultsHtml += Microsoft.Search.Actions.renderActions(docresult);

                    resultsHtml += '            </div>';
                    resultsHtml += '        </div>';


                    //resultsHtml += '    </div>';
                    //resultsHtml += '    </div>';
                }
            }
            else {
                resultsHtml += '<div class="' + classList + '" > Error while rendering result for document ' + docresult.index_key;
                resultsHtml += '</div>';
            }
        }

        // PLACEHOLDER Add a blank result to avoid images to be stretched too much
        if (results.length === 1) {
            resultsHtml += '<div class="' + classList + '" ></div>';
        }

        resultsHtml += '</div>';
        return resultsHtml;
    }
}

// export default Microsoft.All;