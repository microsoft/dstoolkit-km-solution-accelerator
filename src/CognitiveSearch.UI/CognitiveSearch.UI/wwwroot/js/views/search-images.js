// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

//
// IMAGES 
//
Microsoft.Images = Microsoft.Images || {};
Microsoft.Images = {
    MAX_NUMBER_ITEMS_PER_PAGE: 50,
    view_tag_id: "#search-results-content",

    render_image_result: function (search_result) {

        var resultsHtml = '';
        var docresult = search_result.Document !== undefined ? search_result.Document : search_result;

        //docresult.idx = i;
        Microsoft.Search.results_keys_index.push(docresult.index_key);
        docresult.idx = Microsoft.Search.results_keys_index.length - 1;

        var id = docresult.index_key;
        var name = docresult.document_filename;
        var path = docresult.metadata_storage_path;

        if (path !== null) {
            if (docresult.document_embedded) {
                var containerPath = Microsoft.Utils.GetParentPathFromImage(docresult);

                resultsHtml += '<div class="image-result-div pt-2 pb-2 pr-2 pl-2 d-inline-flex flex-column justify-content-center">';
                resultsHtml += '        <div class="image-result-img" onclick="Microsoft.Results.Details.ShowDocument(\'' + id + '\',' + docresult.idx + ');">';

                resultsHtml += '<img class="image-result" src="data:image/png;base64, ' + docresult.image.thumbnail_medium + '" title="' + Base64.decode(docresult.image_parentfilename) + '" />';
                resultsHtml += '        </div>';

                resultsHtml += '    <div class="image-result-path" >';
                resultsHtml += '        <a target="_blank" href="' + Microsoft.Search.GetSASTokenFromPath(containerPath) + '">';
                resultsHtml += '            <span class="text-break">' + Microsoft.Utils.GetImageFileTitle(docresult) + '</span>';
                resultsHtml += '        </a>';
                resultsHtml += '    </div>';

                resultsHtml += '</div>';
            }
            else {
                resultsHtml += '<div class="image-result-div pt-2 pb-2 pr-2 pl-2 d-inline-flex flex-column justify-content-center">';
                resultsHtml += '        <div class="image-result-img" onclick="Microsoft.Results.Details.ShowDocument(\'' + id + '\',' + docresult.idx + ');">';

                resultsHtml += '<img class="image-result" src="data:image/png;base64, ' + docresult.image.thumbnail_medium + '" title="' + docresult.metadata_storage_name + '" />';
                resultsHtml += '        </div>';

                resultsHtml += '    <div class="image-result-path" >';
                resultsHtml += '        <a target="_blank" href="' + Microsoft.Search.GetSASTokenFromPath(path) + '">';
                resultsHtml += '            <span class="text-break">' + name + '</span>';
                resultsHtml += '        </a>';
                resultsHtml += '    </div>';
                resultsHtml += '</div>';
            }
        }

        return resultsHtml;
    },

    ImagesSearch: function(query) {

        Microsoft.Search.setQueryInProgress();
    
        if (query !== undefined && query !== null) {
            $("#q").val(query)
        }
    
        if (Microsoft.Search.currentPage > 0) {
            if (Microsoft.View.currentQuery !== $("#q").val()) {
                //currentPage = 0;
                Microsoft.Search.ResetSearch();
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
    
        // Get center of map to use to score the search results
        $.postAPIJSON('/api/search/getimages',
            {
                queryText: Microsoft.View.currentQuery !== undefined ? Microsoft.View.currentQuery : "*",
                searchFacets: Microsoft.Facets.selectedFacets,
                currentPage: ++Microsoft.Search.currentPage,
                incomingFilter: rendering_filter,
                parameters: Microsoft.Search.Parameters,
                options: Microsoft.Search.Options
            },
            function (data) {
                Microsoft.Images.ImagesUpdate(data, Microsoft.Search.currentPage);
            });
    },

    ImagesUpdate: function(data, currentPage) {
        
        Microsoft.Search.ProcessSearchResponse(data,Microsoft.Images.MAX_NUMBER_ITEMS_PER_PAGE);

        //Results List
        Microsoft.Images.UpdateImagesResults(Microsoft.Search.results, currentPage);
    },

    UpdateImagesResults: function(results, currentPage) {

        if (! currentPage > 1) {
            $(Microsoft.Images.view_tag_id).empty();
        }
    
        if (results && results.length > 0) {
    
            for (var i = 0; i < results.length; i++) {
    
                var image_result_html = Microsoft.Images.render_image_result(results[i]);
    
                $(Microsoft.Images.view_tag_id).append(image_result_html);
            }
        }

        return '';
    }
}

// export default Microsoft.Images;