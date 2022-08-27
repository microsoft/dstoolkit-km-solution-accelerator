// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

//
// Home 
//
Microsoft.Home = Microsoft.Home || {};
Microsoft.Home = {
    LATEST_DOCUMENTS_TAG : "#documents-feed",
    LATEST_IMAGES_TAG : "#images-feed",

    initSearchHome:function() {

        // When 'Enter' clicked from Search Box, execute Search()
        $("#q").keyup(function (e) {
            if (e.keyCode === 13) {
                Microsoft.Utils.OpenView(Microsoft.View.config.path, e.target.value);
            }
        });
    
        window.document.title = Microsoft.View.config.pageTitle;
    
        $("#search-input-button").click(function (event) {
            event.preventDefault();
            var element = $(this);
            element.closest("form").each(function () {
                var form = $(this);
                form[0].searchFacetsAsString.value = JSON.stringify(selectedFacets)
                form[0].submit();
            });
        });

        // Suggestions
        Microsoft.Suggestions.init().then(() => {
            Microsoft.Suggestions.configure();
        });
    
        this.PopulateHome();
    },
    
    PopulateHome: function() {
    
        Microsoft.Search.setQueryInProgress();
    
        Microsoft.Home.RenderPageHighlights();
    
        Microsoft.Search.setQueryCompleted();
    },
    
    RenderPageHighlights:function() {
        for (var j = 0; j < Microsoft.Landing.highlights.length; j++) {
            var item = Microsoft.Landing.highlights[j];
            var items = Microsoft.Landing.highlights[j].insights;
    
            if (item.enable) {
                for (var i = 0; i < items.length; i++) {
                    var insight = items[i];
                    if (insight.method) {
                        Microsoft.Utils.executeFunctionByName(insight.method, window, insight.parameters);
                    }
                }
            }
        }
    },
  
    // IMAGES
    
    GetLatestImages:function (parameters) {
    
        $.postJSON('/api/search/getlatestimages',
            {
                queryText: Microsoft.View.currentQuery !== undefined ? Microsoft.View.currentQuery : "*",
                searchFacets: parameters?.facets,
                parameters: { RowCount: 12 },
            },
            function (data) {
                Microsoft.Search.tokens = data.tokens;
                Microsoft.View.searchId = data.searchId;
                Microsoft.Facets.facets = data.facets;
    
                if (parameters && parameters.update_method) {
                    Microsoft.Utils.executeFunctionByName(parameters.update_method, window, parameters, data);
                }
                else {
                    Microsoft.Home.HomeUpdateLatestImages(data.results, parameters?.tag_id, parameters?.tag_class);
                }
            }
        );
    },
    
    HomeUpdateLatestImages:function (results, target_tag = Microsoft.Home.LATEST_IMAGES_TAG, slick_class ='.images-feed-content',) 
    {
        var resultsHtml = '';
    
        if (results && results.length > 0) {
    
            for (var i = 0; i < results.length; i++) {
    
                var docresult = results[i].Document !== undefined ? results[i].Document : results[i];
    
                Microsoft.Search.results_keys_index.push(docresult.index_key);
                docresult.idx = Microsoft.Search.results_keys_index.length - 1;
    
                var id = docresult.index_key;
                var path = docresult.metadata_storage_path;
    
                var name = docresult.metadata_storage_name;
    
                if (docresult.title) name = docresult.title;
    
                if (path !== null) {
    
                    var pathLower = path.toLowerCase();
    
                    resultsHtml += '<div>';
                    resultsHtml += '<div class="image-carousel-item">'
    
                    var displayName = name;
    
                    if (docresult.document_embedded) {
                        displayName = Microsoft.Utils.GetImageFileTitle(docresult)
                    }
    
                    resultsHtml += '<a id="' + id + '" target="_blank" href="' + Microsoft.Search.GetSASTokenFromPath(path) + '">';
    
                    if (docresult.image.thumbnail_medium) {
                        resultsHtml += '<img class="image-carousel"  title=\'' + path + '\'  src="data:image/png;base64, ' + docresult.image.thumbnail_medium + '"/>';
                    }
                    else {
                        resultsHtml += '<img class="image-carousel" title=\'' + path + '\'  src="' + docresult.image.thumbnail_small + '"/>';
                    }
                    resultsHtml += '<div class="image-carousel-item-title">';
    
                    resultsHtml += '<h5 class="modification-time" title=\'' + path + '\'>' + displayName + '</h5>';
                    resultsHtml += Microsoft.Utils.GetModificationTime(docresult);
                    resultsHtml += '</div>';
    
                    resultsHtml += '</a>';
    
                    resultsHtml += '</div>';
    
                    resultsHtml += '</div>';
                }
            }
        }
    
        $(target_tag).html(resultsHtml);
    
        $(slick_class).not('.slick-initialized').slick({
            infinite: true,
            slidesToShow: 3,
            slidesToScroll: 2,
            nextArrow: '<span class="bi bi-chevron-right nextArrowBtn btn btn-dark"></span>',
            prevArrow: '<span class="bi bi-chevron-left prevArrowBtn btn btn-dark"></span>',
            variableWidth: true,
            adaptiveHeight: true
            //    autoplay: true,
            //    autoplaySpeed: 2000
        });
    },
    
    // DOCUMENTS
    
    GetLatestDocuments:function (parameters) {
    
        $.postJSON('/api/search/getlatestdocuments',
            {
                queryText: Microsoft.View.currentQuery !== undefined ? Microsoft.View.currentQuery : "*",
                searchFacets: parameters?.facets,
                parameters: { RowCount: 12 },
            },
            function (data) {
                Microsoft.Search.tokens = data.tokens;
                Microsoft.View.searchId = data.searchId;
                Microsoft.Facets.facets = data.facets;
    
                if (parameters && parameters.update_method) {
                    Microsoft.Utils.executeFunctionByName(parameters.update_method, window, parameters, data);
                }
                else {
                    Microsoft.Home.HomeUpdateLatestDocuments(data.results, parameters?.tag_id, parameters?.tag_class);
                }
            }
        );
    },
    
    HomeUpdateLatestDocuments:function (results, target_tag = Microsoft.Home.LATEST_DOCUMENTS_TAG, slick_class ='.documents-feed-content', slide_show=false) {
        var resultsHtml = '';
    
        if (results && results.length > 0) {
    
            resultsHtml += '<div class="row row-cols-4">';
    
            for (var i = 0; i < results.length; i++) {
    
                var docresult = results[i].Document !== undefined ? results[i].Document : results[i];
    
                Microsoft.Search.results_keys_index.push(docresult.index_key);
                docresult.idx = Microsoft.Search.results_keys_index.length - 1;
    
                var id = docresult.index_key;
                var path = docresult.metadata_storage_path;
    
                var name = docresult.metadata_storage_name;
    
                if (docresult.title) name = docresult.title;
    
                if (path !== null) {
    
                    var pathLower = path.toLowerCase();
                    var pathExtension = pathLower.split('.').pop();
                    var iconPath = Microsoft.Utils.GetIconPathFromExtension(pathExtension);
    
                    resultsHtml += '<div class="col">';
                    resultsHtml += '<div class="document-carousel-item bg-dark rounded m-2">'
    
                    var displayName = name;
    
                    if (docresult.document_embedded) {
                        displayName = Microsoft.Utils.GetImageFileTitle(docresult)
                    }
    
                    if (Microsoft.Utils.IsOfficeDocument(pathExtension)) {
                        var src = "https://view.officeapps.live.com/op/view.aspx?src=" + encodeURIComponent(Microsoft.Search.GetSASTokenFromPath(path));
                        resultsHtml += '<a target="_blank" href=\'' + src + '\'>';
                    }
                    else {
                        resultsHtml += '<a target="_blank" href=\'' + Microsoft.Search.GetSASTokenFromPath(path) + '\'>';
                    }
    
                    resultsHtml += '<img class="document-carousel-item-image" title="'+displayName+'" src="/api/search/getdocumentcoverimage?id=' + docresult.document_id + '" onError="this.onerror=null;this.src=\'' + iconPath + '\';"/>';
                    resultsHtml += '<div class="document-carousel-item-title">';
    
                    resultsHtml += '<h5 class="modification-time" title=\'' + path + '\'>' + displayName + '</h5>';
                    resultsHtml += Microsoft.Utils.GetModificationTime(docresult);
                    resultsHtml += '</div>';
    
                    resultsHtml += '</a>';
    
                    resultsHtml += '</div>';
                    resultsHtml += '</div>';
                }
    
            }
            resultsHtml += '</div>';
        }
    
        $(target_tag).html(resultsHtml);
    
        if (slide_show) {
            $(slick_class).not('.slick-initialized').slick({
                dots: true,
                infinite: true,
                slidesToShow: 3,
                slidesToScroll: 2,
                nextArrow: '<span class="bi bi-chevron-right nextArrowBtn btn btn-dark"></span>',
                prevArrow: '<span class="bi bi-chevron-left prevArrowBtn btn btn-dark"></span>',
                variableWidth: true,
                adaptiveHeight: false
            });
        }
    }
    
}

// export default Microsoft.Home;
