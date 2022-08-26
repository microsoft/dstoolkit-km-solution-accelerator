// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

Microsoft.Web = Microsoft.Web || {};
Microsoft.Web = {

    MAX_NUMBER_WEB_RESULTS_PER_PAGE: 20,
    view_tag: "#web-results-content",
    view_tag_col1: "#web-results-content-col1",

    WebSearch: function (query) {

        Microsoft.Search.setQueryInProgress();

        if (query !== undefined && query !== null) {
            $("#q").val(query)
        }

        if (Microsoft.Search.currentPage > 0) {
            if (Microsoft.View.currentQuery !== $("#q").val()) {
                Microsoft.Search.ResetSearch();
            }
        }
        Microsoft.View.currentQuery = $("#q").val();

        // Set a default web filter for your vertical if necessary
        var filter = Microsoft.View.config.filter ? Microsoft.View.config.filter : '';

        $.postJSON('/api/web/webresults',
            {
                queryText: Microsoft.View.currentQuery !== undefined ? Microsoft.View.currentQuery : "*",
                searchFacets: Microsoft.Facets.selectedFacets,
                count: Microsoft.Web.MAX_NUMBER_WEB_RESULTS_PER_PAGE,
                incomingFilter: filter,
                parameters: Microsoft.Search.Parameters,
                currentPage: ++Microsoft.Search.currentPage
            },
            function (data) {
                Microsoft.Search.setQueryCompleted();
                Microsoft.Web.WebUpdate(data, Microsoft.Search.currentPage);
            });
    },

    WebUpdate: function (data, currentPage) {

        Microsoft.Search.results = JSON.parse(data);

        if (Microsoft.Search.results.webPages) {
            Microsoft.Search.TotalCount = Microsoft.Search.results.webPages.totalEstimatedMatches;
        }
        else {
            Microsoft.Search.TotalCount = 0;
        }
        Microsoft.Search.MaxPageCount = Math.ceil(Microsoft.Search.TotalCount / Microsoft.Web.MAX_NUMBER_WEB_RESULTS_PER_PAGE);

        Microsoft.Search.UpdateDocCount(Microsoft.Search.TotalCount);

        Microsoft.Web.UpdateWebResults(Microsoft.Search.results, currentPage);

        //// Log Search Events
        //Microsoft.Telemetry.LogSearchAnalytics(data.count);

        //Filters
        Microsoft.Facets.UpdateFilterReset();

        Microsoft.Search.setQueryCompleted();
    },

    UpdateEmbeddedWebResults: function (results, currentPage) {
        var resultsHtml = '';

        if (Object.keys(results).length === 0) {
            return resultsHtml;
        }

        var has_carousel_data = false;

        if (results.images) {

            resultsHtml += '<div class="row border border-1 rounded ms-2">';

            if (currentPage === 1) {
                resultsHtml += '<h6 class="mt-2"><span class="bi bi-image text-secondary"></span> Web Images</h6>';
            }

            resultsHtml += '<div class="web-results-carousel">';

            for (var i = 0; i < results.images.value.length; ++i) {
                has_carousel_data = true;

                var imageresult = results.images.value[i];

                resultsHtml += '<div>';
                resultsHtml += '<div class="web-carousel-item-image">'

                resultsHtml += '<a target="_blank" href=\'' + imageresult.contentUrl + '\'>';
                resultsHtml += '<img src="' + imageresult.thumbnailUrl + '"/>';
                resultsHtml += '</a>';

                resultsHtml += '<a target="_blank" href=\'' + imageresult.hostPageUrl + '\'>';
                resultsHtml += '<div class="web-carousel-item-title">';
                resultsHtml += '    <h5 class="" title=\'' + imageresult.contentUrl + '\'>' + imageresult.name + '</h5>';

                resultsHtml += '    <h6 class=""> ';
                if (imageresult.datePublished) {
                    var d = new Date(imageresult.datePublished);
                    resultsHtml += d.toLocaleString()
                }
                resultsHtml += '    </h6>';

                resultsHtml += '</div>';
                resultsHtml += '</a>';

                resultsHtml += '</div>';
                resultsHtml += '</div>';
            }

            resultsHtml += '</div>';

            resultsHtml += '</div>';
        }

        if (results.webPages) {

            if (has_carousel_data) {
                resultsHtml += '<div class="row border border-1 rounded ms-2 mt-2">';
            }
            else {
                resultsHtml += '<div class="row border border-1 rounded ms-2">';
            }

            if (currentPage === 1) {
                resultsHtml += '<h6 class="mt-2"><span class="bi bi-globe text-secondary"> Web Results</h6>';
            }

            resultsHtml += '<ul class="list-group list-group-flush">';
            //for (var i = 0; i < results.webPages.value.length; ++i) {
            var pagesLength = results.webPages.value.length;

            //if (pagesLength > 2) {
            //    pagesLength = 2
            //}

            for (var i = 0; i < pagesLength; ++i) {

                var webPage = results.webPages.value[i];

                resultsHtml += '<li class="list-group-item flex-even">'
                resultsHtml += '<div>';
                resultsHtml += '    <h5 title=\'' + webPage.url + '\'><span class="bi bi-stars"><a target="_blank" href=\'' + webPage.url + '\'>' + webPage.name + '</a></span></h5>';

                var startTag = '<h6 class="modification-line"> ';
                var endTag = '</h6>';
                var d = new Date(webPage.dateLastCrawled);
                resultsHtml += startTag + 'Last Modified ' + d.toDateString() + endTag;

                resultsHtml += '<span class="text-break" title="' + webPage.snippet + '">';
                resultsHtml += webPage.snippet;
                resultsHtml += '</span>';

                resultsHtml += '</div>';
                resultsHtml += '</li>';
            }
            resultsHtml += '</ul>';
            resultsHtml += '</div>';
        }

        if (currentPage > 1) {
            $(Microsoft.Web.view_tag).append(resultsHtml);
        }
        else {
            $(Microsoft.Web.view_tag).html(resultsHtml);
        }

        if (has_carousel_data) {

            // Slick the images carousel
            $('.web-results-carousel').not('.slick-initialized').slick({
                //dots: true,
                infinite: true,
                slidesToShow: 1,
                slidesToScroll: 1,
                nextArrow: '<button class="slick-next slick-arrow btn btn-warning btn-sm nopadding" aria-label="Next" title="Next" type="button"></button>',
                //prevArrow: '<button class="slick-prev slick-arrow btn btn-danger btn-sm nopadding" aria-label="Previous" title="Previous" type="button"></button>',
                variableWidth: true,
                adaptiveHeight: false
            });

        }
    },

    UpdateWebResults: function (results, currentPage, pagination = true, quickactions = true, full_preview = true) {
        var resultsHtml = '';

        if (Object.keys(results).length === 0) {
            return resultsHtml;
        }

        // https://docs.microsoft.com/en-us/bing/search-apis/bing-web-search/search-responses

        var has_images_data = false;

        // Bing Images 
        if (results.images) {
            if (results.images.value.length > 0) {

                resultsHtml += '<div class="row border border-1 rounded mb-2">';

                if (currentPage === 1 && results.images.value.length > 0) {
                    resultsHtml += '<h6 class="mt-2"><span class="bi bi-image text-secondary"></span> Images</h6>';
                }

                resultsHtml += '<div class="row web-results-images-carousel">';

                for (var i = 0; i < results.images.value.length; ++i) {

                    has_images_data = true;

                    var imageresult = results.images.value[i];

                    resultsHtml += '<div class="col">';
                    resultsHtml += '<div class="web-carousel-item-image">'

                    resultsHtml += '<a target="_blank" href=\'' + imageresult.contentUrl + '\'>';
                    resultsHtml += '<img title="' + imageresult.name + '" class="web-carousel-item-image" src="' + imageresult.thumbnailUrl + '"/>';
                    resultsHtml += '</a>';

                    resultsHtml += '<a target="_blank" href=\'' + imageresult.hostPageUrl + '\'>';
                    resultsHtml += '<div class="web-carousel-item-title">';
                    resultsHtml += '<h5 class="" title=\'' + imageresult.contentUrl + '\'>' + imageresult.name + '</h5>';

                    resultsHtml += '<h6 class=""> ';
                    if (imageresult.datePublished) {
                        var d = new Date(imageresult.datePublished);
                        resultsHtml += d.toLocaleString()
                    }
                    resultsHtml += '</h6>';

                    resultsHtml += '</div>';
                    resultsHtml += '</a>';
                    resultsHtml += Microsoft.Web.renderDownloadAction(i, imageresult.contentUrl);
                    resultsHtml += '</div>';
                    resultsHtml += '</div>';
                }

                resultsHtml += '</div>';

                resultsHtml += '</div>';
            }
        }


        var has_videos_data = false;

        // Bing Videos 
        if (results.videos) {
            if (results.videos.value.length > 0) {

                resultsHtml += '<div class="row border border-1 rounded mb-2">';

                if (currentPage === 1 && results.videos.value.length > 0) {
                    resultsHtml += '<h6 class="mt-2"><span class="bi bi-camera-video text-secondary"> Videos</h6>';
                }

                resultsHtml += '<div class="row web-results-videos-carousel">';

                for (var i = 0; i < results.videos.value.length; ++i) {

                    has_videos_data = true;

                    var imageresult = results.videos.value[i];

                    resultsHtml += '<div class="col">';
                    resultsHtml += '<div class="web-carousel-item-image">'

                    resultsHtml += '<a target="_blank" href=\'' + imageresult.contentUrl + '\'>';
                    resultsHtml += '<img title="' + imageresult.name + '" class="web-carousel-item-image" src="' + imageresult.thumbnailUrl + '"/>';
                    resultsHtml += '</a>';

                    resultsHtml += '<a target="_blank" href=\'' + imageresult.hostPageUrl + '\'>';
                    resultsHtml += '<div class="web-carousel-item-title">';
                    resultsHtml += '<h5 class="" title=\'' + imageresult.contentUrl + '\'>' + imageresult.name + '</h5>';

                    resultsHtml += '<h6 class=""> ';
                    if (imageresult.datePublished) {
                        var d = new Date(imageresult.datePublished);
                        resultsHtml += d.toLocaleString()
                    }
                    resultsHtml += '</h6>';

                    resultsHtml += '</div>';
                    resultsHtml += '</a>';

                    resultsHtml += '</div>';
                    resultsHtml += '</div>';
                }

                resultsHtml += '</div>';

                resultsHtml += '</div>';
            }
        }

        var has_webPages_data = false;

        // Bing Webpages
        if (results.webPages) {
            if (results.webPages.value.length > 0) {

                resultsHtml += '<div class="row">';

                if (currentPage === 1 && results.webPages.value.length > 0) {
                    resultsHtml += '<h6><span class="bi bi-stars"> Web Pages</span></h6>';
                }

                resultsHtml += '<div class="web-content row">';

                if (Microsoft.Search.results_rendering > -1) {

                    has_webPages_data = true;

                    var rendering = Microsoft.View.config.resultsRenderings[Microsoft.Search.results_rendering];
                    resultsHtml += Microsoft.Utils.executeFunctionByName(rendering.method, window, results, currentPage);
                }

                resultsHtml += '</div>';

                resultsHtml += '</div>';
            }
            else {
                // No results message
            }
        }

        var has_news_data = false;

        // Bing News
        if (results.news) {
            if (results.news.value.length > 0) {

                has_news_data = true;

                resultsHtml += '<div class="row">';

                if (currentPage === 1 && results.news.value.length > 0) {
                    resultsHtml += '<h6><span class="bi bi-stars"> News</span></h6>';
                }

                resultsHtml += '<div class="web-content row">';

                if (Microsoft.Search.results_rendering > -1) {

                    var rendering = Microsoft.View.config.resultsRenderings[Microsoft.Search.results_rendering];
                    resultsHtml += Microsoft.Utils.executeFunctionByName(rendering.method, window, results, currentPage);
                }

                resultsHtml += '</div>';

                resultsHtml += '</div>';
            }
        }


        var has_column1_data = false;

        // Related Searches - Facets Style
        if (results.relatedSearches) {

            if (results.relatedSearches.value.length > 0) {

                has_column1_data = true;

                $("#related-search-nav").empty();

                relatedSearchHtml = '';
                var relatedSearchHtml = '<div class="accordion accordion-flush" id="related-searches-accordion">';

                relatedSearchHtml += '<div class="accordion-item">';
                relatedSearchHtml += '<h2 class="accordion-header accordion-item-related-searches" id="web-related-searches-id">';
                relatedSearchHtml += '<button class="accordion-button" type="button" data-bs-toggle="collapse" data-bs-target="#web-related-search" aria-expanded="false" aria-controls="web-related-search">';
                relatedSearchHtml += 'Related Searches';
                relatedSearchHtml += '</button>';
                relatedSearchHtml += '</h2>';

                relatedSearchHtml += '<div id="web-related-search" class="accordion-collapse" aria-labelledby="web-related-searches-id">';

                relatedSearchHtml += '<div class="accordion-body">';
                relatedSearchHtml += '<ul class="list-group list-group-flush">';

                for (var i = 0; i < results.relatedSearches.value.length; i++) {
                    var relatedSearch = results.relatedSearches.value[i];
                    relatedSearchHtml += '<li><a href="javascript:void(0);" onclick="Microsoft.Web.WebSearch(\'' + relatedSearch.text + '\')" >' + relatedSearch.displayText + '</a></li>';
                }
                relatedSearchHtml += '</ul>';

                relatedSearchHtml += '        </div>';
                relatedSearchHtml += '    </div>';
                relatedSearchHtml += '</div>';

                $("#related-search-nav").html(relatedSearchHtml);
            }

        }

        // Bing Entities - Facets Style
        if (results.entities) {

            if (results.entities.value.length > 0) {

                has_column1_data = true;

                $("#entities-nav").empty();

                var entitiesHtml = '<div class="accordion accordion-flush" id="bing-entities-accordion">';

                entitiesHtml += '<div class="accordion-item">';
                entitiesHtml += '<h2 class="accordion-header accordion-item-bing-entities" id="web-bing-entities-id">';
                entitiesHtml += '<button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#web-bing-entity" aria-expanded="true" aria-controls="web-bing-entity">';
                entitiesHtml += 'Bing Entities';
                entitiesHtml += '</button>';
                entitiesHtml += '</h2>';

                entitiesHtml += '<div id="web-bing-entity" class="accordion-collapse collapse" role="group" aria-labelledby="web-bing-entities-id">';

                entitiesHtml += '<div class="accordion-body">';
                entitiesHtml += '<ul>';

                for (var i = 0; i < results.entities.value.length; i++) {
                    var entity = results.entities.value[i];
                    var targetURL = entity.url ? entity.url : entity.webSearchUrl
                    entitiesHtml += '<li><a target="_blank" href="' + targetURL + '" title="' + entity.description + '" >' + entity.name + '</a></li>';
                }
                entitiesHtml += '</ul>';

                entitiesHtml += '        </div>';
                entitiesHtml += '    </div>';
                entitiesHtml += '</div>';

                $("#entities-nav").html(entitiesHtml);
            }
        }

        if (currentPage > 1) {
            $(Microsoft.Web.view_tag).append(resultsHtml);
        }
        else {
            $(Microsoft.Web.view_tag).html(resultsHtml);

            $(Microsoft.Web.view_tag_col1).removeClass();
            $(Microsoft.Web.view_tag).removeClass();

            if (has_column1_data) {
                $(Microsoft.Web.view_tag_col1).addClass('col-md-2');
                $(Microsoft.Web.view_tag).addClass('col-md-10 reset-view');
            }
            else {
                $(Microsoft.Web.view_tag_col1).addClass('d-none');
                $(Microsoft.Web.view_tag).addClass('col-md-12 reset-view');
            }
        }

        if (has_images_data && has_webPages_data) {
            // Slick the images carousel
            $('.web-results-images-carousel').not('.slick-initialized').slick({
                dots: true,
                infinite: true,
                slidesToShow: 5,
                slidesToScroll: 3,
                nextArrow: '<button class="col-1 slick-next slick-arrow btn btn-warning btn-sm nopadding" aria-label="Next" title="Next" type="button"></button>',
                //prevArrow: '<button class="slick-prev slick-arrow btn btn-danger btn-sm nopadding" aria-label="Previous" title="Previous" type="button"></button>',
                variableWidth: true,
                adaptiveHeight: false
            });
        }

        if (has_videos_data && has_webPages_data) {
            // Slick the images carousel
            $('.web-results-videos-carousel').not('.slick-initialized').slick({
                dots: true,
                infinite: true,
                slidesToShow: 5,
                slidesToScroll: 3,
                nextArrow: '<button class="col-1 slick-next slick-arrow btn btn-warning btn-sm nopadding" aria-label="Next" title="Next" type="button"></button>',
                //prevArrow: '<button class="slick-prev slick-arrow btn btn-danger btn-sm nopadding" aria-label="Previous" title="Previous" type="button"></button>',
                variableWidth: true,
                adaptiveHeight: false
            });
        }
    },

    //
    // Web Results as List 
    //
    UpdateWebResultsAsList: function (results, currentPage, quickactions = true, full_preview = true) {

        var resultsHtml = '';

        for (var i = 0; i < results.webPages.value.length; ++i) {

            var documentHtml = '';

            var classList = "row web-list-item rounded ms-2 pb-1 border-start border-danger border-2";

            if (i % 2 !== 0) {
                classList = "row web-list-item rounded ms-2 pb-1 border-start border-secondary border-2";
            }

            var webPage = results.webPages.value[i];

            documentHtml += '<div class="' + classList + '" >';
            documentHtml += '        <div class="row results-header">';
            documentHtml += '        <div class="col-md-11">';
            documentHtml += '            <h5 class="m-0" title=\'' + webPage.url + '\'>';
            documentHtml += '               <a target="_blank" class="text-success" href=\'' + webPage.url + '\'>' + webPage.name + '</a>';
            documentHtml += '            </h5>';
            documentHtml += '            <h6 title=\'' + webPage.url + '\'>';
            documentHtml += webPage.url;
            documentHtml += '            </h6>';
            documentHtml += '        </div>';
            documentHtml += '        <div class="col-md-1">';
            if (quickactions) {
                if (webPage && webPage.url.endsWith('.pdf')) {
                    documentHtml += '    <button type="button" class="btn btn-outline-danger btn-sm" onclick="Microsoft.Web.WebUpload(\'' + Base64.encode(JSON.stringify(webPage)) + '\');">'
                    documentHtml += '        <span class="bi bi-cloud-download"/>'
                    documentHtml += '    </button>'
                }
            }
            documentHtml += '        </div>';
            documentHtml += '        </div>';

            // Item rendering
            documentHtml += '        <div class="web-result-item">';

            var startTag = '<h6 class="modification-line"> ';
            var endTag = '</h6>';
            var d = new Date(webPage.dateLastCrawled);

            documentHtml += startTag + 'Last Modified ' + d.toDateString() + endTag

            if (full_preview) {
                documentHtml += '<div onclick="Microsoft.Web.previewWebPage(\'' + webPage.url + '\');">';
            }
            else {
                documentHtml += '<div onclick="Microsoft.Web.quickPreview(\'' + webPage.url + '\');">';
            }
            documentHtml += webPage.snippet;
            documentHtml += '</div>';

            // deeplinks
            if (webPage.deepLinks) {

                if (webPage.deepLinks.length > 0) {
                    documentHtml += '<div class="container mt-2">'
                    documentHtml += '<div class="row">'
                    for (var j = 0; j < webPage.deepLinks.length; ++j) {
                        var link = webPage.deepLinks[j];
                        documentHtml += '<div class="col"><a target="_blank" href=\'' + link.url + '\' class="">' + link.name + '</a></div>'
                    }
                    documentHtml += '</div>'
                    documentHtml += '</div>'
                }
            }
            documentHtml += '        </div>';
            documentHtml += '</div>';

            resultsHtml += documentHtml;
        }

        return resultsHtml;
    },

    UpdateWebResultsAsCard: function (results, currentPage, quickactions = true, full_preview = true) {

        var resultsHtml = '';

        var classList = "col results-div web-results-div col-md-3";

        for (var i = 0; i < results.webPages.value.length; ++i) {

            var webPage = results.webPages.value[i];

            resultsHtml += '<div class="' + classList + '" >';
            resultsHtml += '    <div class="web-search-result">';
            resultsHtml += '        <div class="results-header">';
            resultsHtml += '            <h5 title=\'' + webPage.url + '\'><a target="_blank" href=\'' + webPage.url + '\'>' + webPage.name + '</a></h5>';
            if (quickactions) {
                resultsHtml += Microsoft.Web.renderWebResultActions(webPage.id, webPage.url, webPage);
            }
            resultsHtml += '        </div>';
            resultsHtml += '         <div class="web-result-item">';

            var startTag = '<h6 class="modification-line"> ';
            var endTag = '</h6>';
            var d = new Date(webPage.dateLastCrawled);

            resultsHtml += startTag + 'Last Modified ' + d.toDateString() + endTag

            if (full_preview) {
                resultsHtml += '<div onclick="Microsoft.Web.previewWebPage(\'' + webPage.url + '\');">';
            }
            else {
                resultsHtml += '<div onclick="Microsoft.Web.quickPreview(\'' + webPage.url + '\');">';
            }
            resultsHtml += webPage.snippet;
            resultsHtml += '</div>';

            // deeplinks
            if (webPage.deepLinks) {
                for (var j = 0; j < webPage.deepLinks.count; ++j) {
                    var link = webPage.deepLinks[j];
                    resultsHtml += '<h7 title=\'' + link.url + '\'><a target="_blank" href=\'' + link.url + '\'>' + link.name + '</a></h7>';

                }
            }
            resultsHtml += '        </div>';
            resultsHtml += '    </div>';
            resultsHtml += '</div>';
        }

        return resultsHtml;
    },

    ClearWebResults: function () {
        $(Microsoft.Web.view_tag).empty();
    },

    renderWebResultActions: function (id, url, webpage = null, staticActions = false, initialStyle = "flex") {
        var htmlDiv = ''

        // Actions
        htmlDiv += '<div class="row">';
        if (staticActions) {
            htmlDiv += '<div class="search-result-actions">';
        }
        else {
            htmlDiv += '<div class="search-result-actions" id="actions-' + id + '" style="display:' + initialStyle + ' !important;">';

        }
        htmlDiv += '    <div class="col-md-12" style="padding: 5px;">';
        htmlDiv += '            <div class="d-grid gap-2 d-md-flex" >';

        if (url && url.endsWith('.pdf')) {
            htmlDiv += '                <button type="button" title="Upload to your data lake" class="btn btn-outline-danger btn-sm" onclick="Microsoft.Web.UrlUpload(\'' + Base64.encode(url) + '\');">';
            htmlDiv += '                    <span class="bi bi-cloud-download"/>';
            htmlDiv += '                </button>';
        }
        htmlDiv += '            </div>';
        htmlDiv += '    </div>';
        htmlDiv += '</div>';
        htmlDiv += '</div>';

        return htmlDiv;
    },

    renderDownloadAction: function (id, url, staticActions = false, initialStyle = "flex") {
        var htmlDiv = '';

        // Actions
        htmlDiv += '<div class="row">';
        if (staticActions) {
            htmlDiv += '<div class="search-result-actions">';
        }
        else {
            htmlDiv += '<div class="search-result-actions" id="actions-' + id + '" style="display:' + initialStyle + ' !important;">';

        }
        htmlDiv += '    <div class="col-md-12" style="padding: 5px;">';
        htmlDiv += '            <div class="d-grid gap-2 d-md-flex" >';
        htmlDiv += '                <button type="button" title="Upload to your data lake" class="btn btn-outline-secondary btn-sm" onclick="Microsoft.Web.UrlUpload(\'' + Base64.encode(url) + '\');">';
        htmlDiv += '                    <span class="bi bi-cloud-download"/>';
        htmlDiv += '                </button>';
        htmlDiv += '            </div>';
        htmlDiv += '    </div>';
        htmlDiv += '</div>';
        htmlDiv += '</div>';

        return htmlDiv;
    },

    previewWebPage: function (url) {

        $('#quick-actions-header').html(Microsoft.Web.renderWebResultActions(1, url));

        $('#details-modal-body').html('<embed src="' + url + '" />');

        $('#preview-modal').modal('show');
    },

    quickPreview: function (link) {

        //    let div = document.createElement('div');
        //    div.classList.add('preview_frame');

        //    let frame = document.createElement('iframe');
        //    frame.src = link;

        //    let close = document.createElement('a');
        //    close.classList.add('close-btn');
        //    close.innerHTML = "Click here to close the preview";
        //    close.addEventListener('click', function (e) {
        //        div.remove();
        //    })

        //    div.appendChild(frame);
        //    div.appendChild(close);

        //    document.getElementById(view_tag).appendChild(div);
    },

    WebUpload: function(base64webpage) {
        $.postJSON('/api/storage/webupload',{base64obj: base64webpage},
            function (data) {
                //TODO send notification
        });
    },

    UrlUpload: function(base64url) {
        $.postJSON('/api/storage/urlupload',{base64obj: base64url},
            function (data) {
                //TODO send notification
            });
    },
}

// export default Microsoft.Web;