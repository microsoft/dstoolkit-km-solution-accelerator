// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

Microsoft.Search = Microsoft.Search || {};
Microsoft.Search = {
    isQueryInProgress: false,
    setQueryInProgress: function () {
        this.isQueryInProgress = true;
        $(".tt-menu").hide();
        $('#doc-count').addClass("d-none");
        $('#loading-indicator').removeClass('d-none');
        $('#loading-indicator').addClass('d-flex');
    },
    setQueryCompleted: function () {
        this.isQueryInProgress = false;
        $(".tt-menu").hide();
        $('#doc-count').removeClass('d-none');
        $('#loading-indicator').removeClass('d-flex');
        $('#loading-indicator').addClass("d-none");

    },
    disableSearchBox: function () {

        $("#navbar-row-1").addClass("d-none");
        $("#filter-navbar").addClass("d-none");

        if (Microsoft.View && Microsoft.View.config) {
            $('#navitem-' + Microsoft.View.config.id).addClass('navbar-highlight');
        }

        Microsoft.Search.setQueryCompleted();

    },

    indexName: '',
    results: [],
    results_keys_index: [],

    results_rendering: -1,

    selected_results_filter: {},

    // Azure Storage SAS token
    tokens: [],

    TotalCount: 0,
    currentPage: 0,
    MaxPageCount: 0,

    MAX_NUMBER_ITEMS_PER_PAGE: 10,

    mrcAnswers: [],
    qnaAnswers: [],
    semantic_answers: [],

    ProcessSearchResponse: function (data, numberOfItemPerPage) {

        Microsoft.Search.results = data.results;
        Microsoft.Facets.facets = data.facets;
        Microsoft.Tags.tags = data.tags;
        Microsoft.Search.tokens = data.tokens;
        Microsoft.View.searchId = data.searchId;

        this.indexName = data.indexName;

        Microsoft.Search.TotalCount = data.count;

        if (data.isSemanticSearch) {
            Microsoft.Search.MaxPageCount = 1;
        }
        else {
            if (numberOfItemPerPage === undefined || numberOfItemPerPage === null) {
                Microsoft.Search.MaxPageCount = 1;
            }
            else {
                Microsoft.Search.MaxPageCount = Math.ceil(Microsoft.Search.TotalCount / numberOfItemPerPage);
            }
        }

        Microsoft.Search.UpdateDocCount(data.count);

        // Facets
        Microsoft.Facets.RenderFacets();
        Microsoft.Facets.UpdateFilterReset();

        // ANSWERS
        Microsoft.Answers.UpdateAnswers(data);

        Microsoft.Telemetry.LogSearchAnalytics(data.count);

        Microsoft.Search.setQueryCompleted();

    },

    hasMoreResults: function () {
        return Microsoft.Search.currentPage < Microsoft.Search.MaxPageCount;
    },
    
    UpdateDocCount: function (resultsCount) {
        if (resultsCount === 0) {
            $("#doc-count").addClass('bg-danger');
            $("#doc-count").html('No result !');
        }
        else {
            $("#doc-count").removeClass('bg-danger');
            $("#doc-count").html('About ' + resultsCount.toLocaleString('en-US') + ' result(s)');
        }
    },

    initSearchVertical: function (vertical) {

        if (vertical.infiniteScroll) {
            $(window).scroll(function () {
                if (!Microsoft.Search.isQueryInProgress) {
                    // console.log('scroll '+ document.body.scrollHeight + ' - ' + document.body.offsetHeight);
                    // Trigger reload when the user reaches 85% of the height of the window
                    var thresholdHeight = document.body.scrollHeight * 0.95;
                    var scrollYPosition = typeof window.scrollY === "undefined" ? window.pageYOffset : window.scrollY;
                    if ((window.innerHeight + scrollYPosition) >= thresholdHeight) {
                        //Has more results
                        if (Microsoft.Search.hasMoreResults()) {
                            // console.log('has more results');
                            Microsoft.Utils.executeFunctionByName(Microsoft.View.config.searchMethod, window);
                        }
                    }
                }
            });
        }

        // Suggestions
        Microsoft.Suggestions.init().then(() => {
            Microsoft.Suggestions.configure(vertical);
        });

        //Static Facets
        Microsoft.Facets.init().then(() => {
            Microsoft.Facets.RenderStaticFacets();
        });

        Microsoft.Facets.selectedFacets = Microsoft.View.selectedFacets;

        if (Microsoft.Facets.selectedFacets && Microsoft.Facets.selectedFacets.length > 0) {
            Microsoft.Facets.UpdateFilterReset();
        }

        window.document.title = vertical.pageTitle;

        $('#navitem-' + vertical.id).addClass('navbar-highlight');

        $('#q').attr('placeholder', vertical.placeHolder);

        if (vertical.isSemanticCapable) {
            $('#semanticSwitch').prop('checked', Microsoft.Search.Options.isSemanticSearch);
        }

        if (vertical.enableOffcanvasNavigation) {
            $('#navigation-btn').removeClass('d-none');
        }
        if (vertical.enableDynamicFacets) {
            $('.dynamic-facet-navigation').removeClass('d-none');
        }
        if (vertical.enableExcelExport) {
            $('#export-excel-btn').removeClass('d-none');
        }

        if (vertical.enableDateRange) {
            $('#daterange-form-id').removeClass('d-none');

            Microsoft.Facets.initDateRangeFilter();
        }

        if(vertical.title) {
            $('#search-button').attr('title', vertical.title);
            $('#search-button-image').attr('title', vertical.title);
            $('#search-button-image').attr('alt', vertical.title);    
        }

        if (vertical.svgicon) {
            $('#search-button-image').attr('src', '/icons/'+vertical.svgicon);
        }

        // Assign the default results rendering of that vertical when relevant
        if (Microsoft.View.config.resultsRenderings) {
            for (var i = 0; i < Microsoft.View.config.resultsRenderings.length; i++) {
                var rendering = Microsoft.View.config.resultsRenderings[i];
                if (rendering.isDefault)
                    this.results_rendering = i;
            }
        }

        Microsoft.Search.Results.renderResultsView();

        // When 'Enter' clicked from Search Box, execute Search()
        $("#q").keyup(function (e) {
            if (e.keyCode === 13) {
                Microsoft.Search.ResetSearch();
                Microsoft.Utils.executeFunctionByName(Microsoft.View.config.searchMethod, window, e.target.value);
            }
        });

    },

    initSearch: function () {

        Microsoft.Search.ResetSearch();

        //Do the initial search query when the document is ready. 
        if (Microsoft.View.currentQuery) {
            document.getElementById('q').value = Microsoft.View.currentQuery;
        }

        $(".tt-menu").hide();

        Microsoft.Utils.executeFunctionByName(Microsoft.View.config.searchMethod, window);
    },

    ProcessHighlights: function (result) {
        var highlights = "";

        var max_higlights = 3;
        var highlightsCounter = 0;
        var max_total_chars_size = 250;

        var fields = ["content", "translated_text"];
        // Hit Hightlighting
        if (result.Highlights && result.Highlights !== undefined) {

            var highlightdata = result.Highlights;
            var has_content_highlight = false;

            for (var i = 0; i < fields.length; i++) {
                var field = fields[i];
                if (highlightdata.hasOwnProperty(field)) {
                    if (highlightdata[field] !== null && highlightdata[field] !== undefined) {

                        if ((field === "content" || field === "translated_text") && has_content_highlight) {
                            break;
                        }

                        if (highlightsCounter < max_higlights) {

                            if (field === "content" || field === "translated_text") {
                                has_content_highlight = true;
                            }

                            highlights += "<div>";
                            for (var j = 0; j < highlightdata[field].length; j++) {
                                if (j > 5) {
                                    break;
                                }

                                highlights += "<strong> ... </strong>" + highlightdata[field][j];
                                //// TODO
                                //if (highlightdata[field][j].length <= 250) {
                                //    highlights += "<strong>...</strong>" + highlightdata[field][j];
                                //}
                                //else {
                                //    console.log(highlightdata[field][j]);
                                //}

                                highlightsCounter += 1
                            }
                            highlights += "</div>";
                        }
                    }
                }
            }
        }

        if ("@search.captions" in result) {
            var captions = result["@search.captions"];

            if (captions) {
                for (var j = 0; j < captions.length; j++) {
                    var answer = captions[j];
                    if ("highlights" in answer && answer.highlights !== null) {
                        highlights += '<span>' + answer.highlights + ' </span>';
                    }
                    else {
                        if ("text" in answer) {
                            highlights += '<span>' + answer.text + ' </span>';
                        }
                    }
                }
            }
        }

        //// Fallback to the description or the first paragraphs when no highlights
        if (highlights.length == 0) {

            var docresult = result.Document !== undefined ? result.Document : result;

            if (docresult.summary && docresult.summary.length > 0) {
                highlights = docresult.summary.join(' ... ');
            }
            else if (docresult.description && docresult.description.length > 0) {
                highlights = docresult.description
            }
            else if (docresult.paragraphs && docresult.paragraphs.length > 0) {
                highlights = docresult.paragraphs[0]
            }
        }

        return highlights;
    },

    ResetSearch: function () {
        Microsoft.Search.currentPage = 0;
        Microsoft.Search.results_keys_index = []

        $('.parallax-search').show();
        $('.parallax').hide();
        $(".tt-menu").hide();

        // Reset all data views
        var reset_views = [].slice.call(document.querySelectorAll('.reset-view'));
        reset_views.map(function (elt) {
            elt.innerHTML = '';
        });
    },

    ReSearch: function (query, filter) {
        this.ResetSearch();
        if (Microsoft.View.config.searchMethod) {
            Microsoft.Utils.ScrollTopFunction();
            Microsoft.Utils.executeFunctionByName(Microsoft.View.config.searchMethod, window, query, filter);
        } else if (Microsoft.View.config.path) {
            Microsoft.Utils.OpenView(Microsoft.View.config.path, query, filter)
        }
    },

    GetSASTokenFromPath: function (path) {
        if (path != "undefined") {
            var keys = Object.keys(Microsoft.Search.tokens);
            for (var i = 0; i < keys.length; i++) {
                if (path.indexOf(keys[i]) > -1) {
                    return path + Microsoft.Search.tokens[keys[i]];
                }
            }
            return path;
        }
    },

    //TODO
    RenderCoverImage: function (docresult) {
        var documentHtml = '';
        if (hasCoverImage) {
            documentHtml += '   <img alt="' + name + '" class="image-result cover-image" src="data:image/png;base64,R0lGODlhAQABAAD/ACwAAAAAAQABAAACADs=" data-src="/api/document/getcoverimage?document_id=' + document_key + '" title="' + docresult.metadata_storage_name + '"onError="this.onerror=null;this.src=\'' + iconPath + '\';"/>';
        }
        else {
            documentHtml += '   <img alt="' + name + '" class="image-result-nocover" src="' + iconPath + '" title="' + docresult.title + '"/>';
        }
        return documentHtml;
    },

    ProcessCoverImage: function () {
        var imgDefer = document.getElementsByClassName('cover-image');
        for (var i = 0; i < imgDefer.length; i++) {
            this.RequestCoverImage(imgDefer[i]);
        }
    },
    RequestCoverImage: function (coverImage) {
        if (coverImage.getAttribute('data-src')) {
            var url = coverImage.getAttribute('data-src');

            if (Microsoft.Config.data.webAPIBackend.isEnabled) {
                // Append the API Backend host here
                url = Microsoft.Config.data.webAPIBackend.endpoint + url;
            }

            // Call backend API with a promise
            return new Promise((resolve, reject) => {
                jQuery.ajax({
                    cache: true,
                    url: url,
                    type: "POST",
                    contentType: "application/text; charset=utf-8",
                    dataType: "text",
                    success: function (data) {
                        coverImage.setAttribute('src', 'data:image/png;base64,' + data);
                        coverImage.classList.remove('cover-image');
                    }
                });
            });
        }
    }
}

//
// Search Parameters
//
Microsoft.Search.Parameters = Microsoft.Search.Parameters || {};
Microsoft.Search.Parameters = {

    scoringProfile: "",

    inOrderBy: [],

}

//
// User Options (backend)
//
Microsoft.Search.Options = Microsoft.Search.Options || {};
Microsoft.Search.Options = {
    isSemanticSearch: false,
    isQueryTranslation: true,
    isQuerySpellCheck: true,
    suggestionsAsFilter: true,

    switchSemanticSearch: function (obj) {
        if (obj !== undefined) {
            this.isSemanticSearch = obj.checked;
            Microsoft.Search.ReSearch();
            this.save();
        }
    },
    switchQueryTranslation: function (obj) {
        if (obj !== undefined) {
            this.isQueryTranslation = obj.checked;
            Microsoft.Search.ReSearch();
            this.save();
        }
    },
    switchQuerySpellCheck: function (obj) {
        if (obj !== undefined) {
            this.isQuerySpellCheck = obj.checked;
            Microsoft.Search.ReSearch();
            this.save();
        }
    },
    switchSuggestionsAsFilter: function (obj) {
        if (obj !== undefined) {
            this.suggestionsAsFilter = obj.checked;
            Microsoft.Search.ReSearch();
            this.save();
        }
    },
    init: function () {
        // this is ugly. TODO 
        $("#semanticSwitch").prop("checked", this.isSemanticSearch);
        $("#translation-switch").prop("checked", this.isQueryTranslation);
        $("#spellcheck-switch").prop("checked", this.isQuerySpellCheck);
        $("#suggestions-switch").prop("checked", this.suggestionsAsFilter);
    },
    save: function () {
        // Save the Search Options
        localStorage.setItem("Microsoft.Search.Options", JSON.stringify(Microsoft.Search.Options));
    }
}

if (localStorage.getItem("Microsoft.Search.Options")) {
    Microsoft.Search.Options = Object.assign(Microsoft.Search.Options, JSON.parse(localStorage.getItem("Microsoft.Search.Options")));

    Microsoft.Search.Options.init();
}
else {
    // Save the Search Options
    Microsoft.Search.Options.save();
}

//
// Search Utilities
//
Microsoft.Search.Utils = Microsoft.Search.Utils || {};
Microsoft.Search.Utils.Excel = Microsoft.Search.Utils.Excel || {};

Microsoft.Search.Utils.Excel = {

    columnsExclusionList: ["index_key", "image_parentid", "image_parentfilename", "image_parenturl", "image_data", "thumbnail_small", "thumbnail_medium", "document_id"],
    //Credits Search Team 
    jsonToCSVConvertor: function (jsonData, ReportTitle, ShowLabel) {
        //If JSONData is not an object then JSON.parse will parse the JSON string in an Object
        var arrData = typeof jsonData !== 'object' ? JSON.parse(jsonData) : jsonData;
        var emptyValue = '';
        var delimiter = '\t';
        try {
            var CSV = '';
            //This condition will generate the Label/Header
            if (ShowLabel) {
                var row = "";
                for (var header in arrData[0]) {
                    row += header + delimiter;
                }

                row = row.slice(0, -1);
                CSV += row + '\r\n';
            }

            for (var i = 0; i < arrData.length; i++) {
                var r = "";
                for (var ind in arrData[i]) {
                    var columnValue = emptyValue;
                    if (arrData[i][ind]) {
                        columnValue = jQuery("<div>" + arrData[i][ind].toString().replace(/[\r\n]+/gm, " ").replace(/\"/g, "\"\"") + "</div>").text();
                    }
                    r += "\"" + columnValue + "\"" + delimiter;
                }

                r.slice(0, row.length - 1);
                CSV += r + '\r\n';
            }

            if (CSV === '') {
                alert("Invalid data");
                return;
            }

            //Generate a file name
            var fileName = "Report_";
            fileName += ReportTitle.replace(/ /g, "_");

            //Initialize file format you want csv or xls
            var uri = 'data:text/csv;charset=utf-8,' + escape(CSV);
            var link = document.createElement("a");

            if (navigator.msSaveBlob) { // IE 10+ 
                return navigator.msSaveBlob(new Blob([CSV], { type: "text/csv;charset=utf-8;" }), fileName + ".csv");
            }
            link.href = uri;
            link.style.cssText = "visibility:hidden";
            link.download = fileName + ".csv";
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
        } catch (e) {
            console.error(e);
        }
    },
    dataToExport: [],
    exportToExcel: function () {
        var query = {
            q: Microsoft.View.currentQuery !== undefined ? Microsoft.View.currentQuery : "*",
            searchFacets: Microsoft.Facets.selectedFacets,
            currentPage: 1,
            semanticSearch: Microsoft.Search.Options.isSemanticSearch
        };

        if (window.location.pathname) {
            if (window.location.pathname.toLowerCase() === '/home/search') {
                this.getExportToExcelResults('/api/search/getdocuments', query, 'All Search Results Export');
            } else if (window.location.pathname.toLowerCase() === '/images/images') {
                this.getExportToExcelResults('/api/search/getimages', query, 'Images Search Results Export');
            } else if (window.location.pathname.toLowerCase() === '/videos/videos') {
                this.getExportToExcelResults('/api/search/getvideos', query, 'Videos Search Results Export');
            }
        }
    },
    maxNumberOfItems: 100,
    formatDate: function (date) {
        try {
            if (date) {
                date = date.toJSON().slice(0, 10);
                var nDate = date.slice(5, 7) + '-'
                    + date.slice(8, 10) + '-'
                    + date.slice(0, 4);
                return nDate;
            }

        } catch (e) {
            console.error(e);
        }
    },
    isDate: function (val) {
        var d = new Date(val);
        return !isNaN(d.valueOf());
    },
    getExportToExcelResults: function (url, query, ReportTitle) {
        Microsoft.Search.setQueryInProgress();

        $.postAPIJSON(url, query,
            function (data) {
                try {
                    if (data && data.results) {
                        if (data.results.length > 0) {
                            for (var i = 0; i < data.results.length; i++) {
                                var obj = data.results[i].Document ? data.results[i].Document : data.results[i];
                                for (var prop in obj) {
                                    if (Microsoft.Search.Utils.Excel.columnsExclusionList.indexOf(prop) > -1) {
                                        delete obj[prop];
                                    }
                                }
                                Microsoft.Search.Utils.Excel.dataToExport.push(obj);
                            }
                            //Microsoft.Search.Utils.Excel.dataToExport = $.merge(Microsoft.Search.Utils.Excel.dataToExport, data.results);
                            //if (Microsoft.Search.Utils.Excel.dataToExport.length === data.count || Microsoft.Search.Utils.Excel.dataToExport.length >= Microsoft.Search.Utils.Excel.maxNumberOfItems) {
                            if (Microsoft.Search.Utils.Excel.dataToExport.length === data.count || Microsoft.Search.Utils.Excel.dataToExport.length >= Microsoft.Search.Utils.Excel.maxNumberOfItems) {
                                Microsoft.Search.Utils.Excel.jsonToCSVConvertor(Microsoft.Search.Utils.Excel.dataToExport, ReportTitle, true);
                                Microsoft.Search.setQueryCompleted();
                                Microsoft.Search.Utils.Excel.dataToExport = [];

                            } else {
                                query.currentPage += 1;
                                Microsoft.Search.Utils.Excel.getExportToExcelResults(url, query, ReportTitle);
                            }
                        }
                        else {
                            if (Microsoft.Search.Utils.Excel.dataToExport.length > 0) {
                                Microsoft.Search.Utils.Excel.jsonToCSVConvertor(Microsoft.Search.Utils.Excel.dataToExport, ReportTitle, true);
                            }

                            Microsoft.Search.setQueryCompleted();
                        }
                    }
                } catch (e) {
                    console.error(e);
                    Microsoft.Search.setQueryCompleted();
                }
            }
        );
    }
}

//
// Search Result
//
Microsoft.Search = Microsoft.Search || {};
Microsoft.Search.Results = Microsoft.Search.Results || {};
Microsoft.Search.Results = {

    RenderSearchResultPreview: function (result) {
        return this.render_file_container(result.document_embedded, result.metadata_storage_path, result.image ? result.image.image_data : null, result.page_number, result.image_parentid, result.image_parentfilename);
    },
    render_file_container: function (document_embedded, path, image_data, pagenumber, parentid, parentfilename) {
        var fileContainerHTML = '';
        if (path !== null) {

            var pathLower = path.toLowerCase();

            var pathExtension = pathLower.split('.').pop();

            if (Microsoft.Utils.IsPDF(pathLower)) {
                if (pagenumber) {
                    fileContainerHTML = '<embed class="file-container" src="' + Microsoft.Search.GetSASTokenFromPath(path) + '#page=' + pagenumber + '" type="application/pdf"/>';
                }
                else {
                    fileContainerHTML = '<embed class="file-container" src="' + Microsoft.Search.GetSASTokenFromPath(path) + '" type="application/pdf"/>';
                }
            }
            else if (Microsoft.Utils.IsImageExtension(pathExtension)) {
                fileContainerHTML = '<div class="file-container">';

                fileContainerHTML += '  <div class="image-container">';

                fileContainerHTML += '<div id="extendable-image-before-container">';
                fileContainerHTML += '</div>';

                if (Microsoft.Utils.IsTIFFImage(pathExtension)) {
                    fileContainerHTML += '<img id="image-rotate" rotate="0" class="image-viewport img-fluid" title="File Viewer" src="data:image/png;base64, ' + image_data + '"/>';
                }
                else {
                    fileContainerHTML += '<img id="image-rotate" rotate="0" class="image-viewport img-fluid" title="File Viewer" src="' + Microsoft.Search.GetSASTokenFromPath(path) + '"/>';
                }

                fileContainerHTML += '<div id="extendable-image-after-container">';

                //Next Page Support for PDF embedded page
                if (document_embedded && Microsoft.Utils.IsPDF(parentfilename)) {
                    fileContainerHTML += '<button type="button" class="btn btn-light" onclick="Microsoft.Search.Results.get_next_page(\'' + parentid + '\',' + pagenumber + ');" >Next Page</button>'
                }

                fileContainerHTML += '</div>';

                fileContainerHTML += '  </div>';
                fileContainerHTML += '</div>';
            }
            else if (pathExtension === "xml") {
                fileContainerHTML = '<iframe class="file-container" src="' + Microsoft.Search.GetSASTokenFromPath(path) + '" type="text/xml">';
                fileContainerHTML += '  This browser does not support XMLs. Please download the XML to view it: <a href="' + Microsoft.Search.GetSASTokenFromPath(path) + '">Download XML</a>"';
                fileContainerHTML += '</iframe>';
            }
            else if (pathExtension === "htm" || pathExtension === "html") {
                fileContainerHTML = '<iframe class="file-container" src="' + Microsoft.Search.GetSASTokenFromPath(path) + '" type="text/html">';
                fileContainerHTML += '  Error viewing. Please download the HTML to view it: <a href="' + Microsoft.Search.GetSASTokenFromPath(path) + '">Download HTML</a>"';
                fileContainerHTML += '</iframe>';
            }
            else if (pathExtension === "mp3") {
                fileContainerHTML = '<audio controls>';
                fileContainerHTML += '<source src="' + Microsoft.Search.GetSASTokenFromPath(path) + '" type="audio/mp3">';
                fileContainerHTML += '                 Your browser does not support the audio tag.';
                fileContainerHTML += '</audio>';
            }
            else if (pathExtension === "mp4") {
                fileContainerHTML = '<video controls class="video-result">';
                fileContainerHTML += '  <source src="' + Microsoft.Search.GetSASTokenFromPath(path) + '" type="video/mp4">';
                fileContainerHTML += '      Your browser does not support the video tag.';
                fileContainerHTML += '</video>';
            }
            // SECURITY WARNING - Office Viewer requires public access to the data !! 
            else if (Microsoft.Utils.IsOfficeDocument(pathExtension)) {

                var src = "https://view.officeapps.live.com/op/view.aspx?src=" + encodeURIComponent(Microsoft.Search.GetSASTokenFromPath(path));

                fileContainerHTML =
                    '<iframe class="file-container" src="' + src + '"></iframe>';
            }
            else {
                //    fileContainerHTML =
                //        '<div>This file cannot be previewed. Download it here to view: <a href="' + Microsoft.Search.GetSASTokenFromPath(path) + '">Download</a></div>';
            }
        }
        else {
            //    fileContainerHTML =
            //        '<div>This file cannot be previewed or downloaded.';
        }

        return fileContainerHTML;
    },

    get_next_page: function (document_id, pagenumber) {
        $.postAPIJSON('/api/document/getsiblings',
            {
                document_id: document_id,
                incomingFilter: "image_parentid eq '" + document_id + "' and (page_number ge " + (pagenumber + 1) + ")",
                parameters: {
                    RowCount: 1,
                    inOrderBy: ["page_number asc"]
                }
            },
            function (data) {
                Microsoft.Search.Results.append_next_page(data.results)
            });
    },
    append_next_page: function (results) {

        if (results && results.length > 0) {

            var fileContainerHTML = '';

            for (var i = 0; i < results.length; i++) {

                var docresult = results[i].Document !== undefined ? results[i].Document : results[i];

                var path = docresult.metadata_storage_path;
                var pathLower = path.toLowerCase();
                var pathExtension = pathLower.split('.').pop();

                fileContainerHTML += '  <div class="image-container">';
                if (Microsoft.Utils.IsTIFFImage(pathExtension)) {
                    fileContainerHTML += '<img id="image-rotate" rotate="0" class="image-viewport img-fluid" title="Next Page" src="data:image/png;base64, ' + docresult.image.image_data + '"/>';
                }
                else {
                    fileContainerHTML += '<img id="image-rotate" rotate="0" class="image-viewport img-fluid" title="Next Page" src="' + Microsoft.Search.GetSASTokenFromPath(path) + '"/>';
                }
                fileContainerHTML += '  </div>';

                // Next Page
                if (docresult.document_embedded) {
                    fileContainerHTML += '<button id="next_page_button" type="button" class="btn btn-success" onclick="Microsoft.Search.Results.get_next_page(\'' + docresult.image_parentid + '\',' + docresult.page_number + ');" >Next Page</button>'
                }
            }
            //else if (Microsoft.Utils.IsImageExtension(pathExtension)) {

            $('#extendable-image-after-container').append(fileContainerHTML);
        }
    },

    // Search Results as list item 
    RenderResultAsListItem: function (result, hasCoverImage = true, showMethod = "Microsoft.Results.Details.ShowDocumentById") {
        var documentHtml = '';
        var classList = "row results-list-item pb-1";

        var docresult = result.Document !== undefined ? result.Document : result;
        var document_key = docresult.image_parentid !== null ? docresult.image_parentid : docresult.document_id;

        Microsoft.Search.results_keys_index.push(docresult.index_key);
        docresult.idx = Microsoft.Search.results_keys_index.length - 1;

        var path = docresult.metadata_storage_path;
        var name = docresult.metadata_storage_name;

        if (docresult.title) name = docresult.title;

        if (path !== null) {

            documentHtml += '<div class="' + classList + '">';

            //
            // Rendering the search result div as list
            //

            var highlights = Microsoft.Search.ProcessHighlights(result);

            var pathLower = path.toLowerCase();
            var pathExtension = pathLower.split('.').pop();

            // First Column
            if (hasCoverImage) {
                documentHtml += '<div class="col-md-2">'
            }
            else {
                documentHtml += '<div class="col-md-1">'
            }

            var iconPath = Microsoft.Utils.GetIconPathFromExtension(pathExtension);
            documentHtml += '<a href="javascript:void(0)" onclick="' + showMethod + '(\'' + docresult.document_id + '\');" >';

            if (Microsoft.Utils.images_extensions.includes(pathExtension)) {
                if (docresult.document_embedded) {
                    name = Microsoft.Utils.GetImageFileTitle(docresult);
                    documentHtml += '<img alt="' + name + '" class="image-result" src="data:image/png;base64, ' + docresult.image.thumbnail_medium + '" title="' + Base64.decode(docresult.image_parentfilename) + '" />';
                }
                else {
                    documentHtml += '<img alt="' + name + '" class="image-result" src="data:image/png;base64, ' + docresult.image.thumbnail_medium + '" title="' + docresult.metadata_storage_name + '" />';
                }
            }
            else {
                if (hasCoverImage) {
                    documentHtml += '   <img alt="' + name + '" class="image-result cover-image" src="data:image/png;base64,R0lGODlhAQABAAD/ACwAAAAAAQABAAACADs=" data-src="/api/document/getcoverimage?document_id=' + document_key + '" title="' + docresult.metadata_storage_name + '"onError="this.onerror=null;this.src=\'' + iconPath + '\';"/>';
                }
                else {
                    documentHtml += '   <img alt="' + name + '" class="image-result-nocover" src="' + iconPath + '" title="' + docresult.title + '"/>';
                }
            }
            documentHtml += '</a>';
            documentHtml += '</div>';

            // Second column
            if (hasCoverImage) {
                documentHtml += '<div class="col-md-10">'
            }
            else {
                documentHtml += '<div class="col-md-11">'
            }

            documentHtml += '<div class="d-flex align-items-center">'
            documentHtml += '<div class="col-md-11">'
            documentHtml += Microsoft.Utils.GetDocumentTitle(docresult);
            documentHtml += '</div>';
            documentHtml += '<div class="col-md-1">'
            documentHtml += Microsoft.Search.Actions.renderActionsAsMenu(docresult);
            documentHtml += '</div>';
            documentHtml += '</div>'

            documentHtml += Microsoft.Utils.GetModificationLine(docresult);

            documentHtml += '   <div class="results-body mt-2">';
            if (highlights.length > 0) {
                documentHtml += highlights;
            }
            documentHtml += Microsoft.Tags.renderTagsAsList(docresult, true, false, ['organizations', 'key_phrases']);
            documentHtml += '</div>';

            documentHtml += '</div>';

            //// Third column
            //documentHtml += '<div class="col-md-3">'
            //documentHtml += '   <div class="results-body mt-2">' + Microsoft.Tags.renderTags(docresult.index_key, tags, false); + '</div>';
            //documentHtml += '</div>';

            documentHtml += '</div>';
            documentHtml += '</div>';
        }
        else {
            documentHtml += '<div class="' + classList + '" > Error while rendering result for document ' + docresult.index_key;
            documentHtml += '</div>';
        }

        return documentHtml;
    },

    renderResultsView: function () {
        if (Microsoft.View.config.resultsRenderings && Microsoft.View.config.resultsRenderings.length > 0) {
            var renderingHtml = '';

            var switchClassList = "view-switch-button btn btn-outline-secondary btn-sm";
            var switchClassListActive = "view-switch-button btn btn-outline-secondary btn-sm active";

            // For each rendering of the search vertical
            for (var i = 0; i < Microsoft.View.config.resultsRenderings.length; i++) {
                var rendering = Microsoft.View.config.resultsRenderings[i];

                if (rendering.name !== "blank")
                {
                    renderingHtml += '        <label id="switch-' + rendering.name + '" title="' + rendering.title + '"  class="' + (Microsoft.Search.results_rendering === i ? switchClassListActive : switchClassList) + '" onclick="Microsoft.Search.Results.switchResultsView(' + i + ');">';
                    renderingHtml += '             <span class="' + rendering.fonticon + '"/>';
                    renderingHtml += '        </label>';    
                }
                else {
                    renderingHtml += '<label class="m-1"></label>';
                }    
            }

            $("#filter-results-rendering").html(renderingHtml);
        }
    },
    switchResultsView: function (viewidx) {

        Microsoft.Search.results_rendering = viewidx;

        this.renderResultsView();

        // ReSearch with new filter
        Microsoft.Search.ReSearch();
    },

    isSubset: function(array1, [groupidx,viewidx]) {
        if ( groupidx in array1 ) {
            return array1[groupidx].findIndex((obj) => {
                // if the current object name key matches the string
                // return boolean value true
                if (obj===viewidx) {
                    return true;
                }
                // else return boolean value false
                return false;
            }) > -1;
        }
        else {
            return false;
        }
    },

    isSubsetIdx: function(array1, [groupidx,viewidx]) {
        if ( groupidx in array1 ) {
            return array1[groupidx].findIndex((obj) => {
                // if the current object name key matches the string
                // return boolean value true
                if (obj===viewidx) {
                    return true;
                }
                // else return boolean value false
                return false;
              });
        }
        else {
            array1[groupidx] = [];
            return -1;
        }
    },
}

// METADATA

Microsoft.Search = Microsoft.Search || {};
Microsoft.Search.Results.Metadata = Microsoft.Search.Results.Metadata || {};
Microsoft.Search.Results.Metadata = {
    render_tab: function (result) {

        var metadataContainerHTML = $("#metadata-viewer").html();

        metadataContainerHTML = '';
        metadataContainerHTML += '<table class="table metadata-table table-hover table-striped"><thead><tr><th data-field="key" class="key">Key</th><th data-field="value">Value</th></tr></thead>';
        metadataContainerHTML += '<tbody>';

        var excluding_fields = ["content", "merged_content", "translated_text", "tables", "paragraphs", "image_data", "thumbnail_small", "thumbnail_medium", "tokens_html"];

        for (var key in result) {
            if (result.hasOwnProperty(key)) {
                if (!excluding_fields.includes(key)) {
                    if (result[key] !== null) {
                        var value = result[key];
                        if (value.length && value.length > 0) {
                            if (key === "image_parentfilename" || key === "image_parenturl") {
                                value = Base64.decode(value);
                            }
                            metadataContainerHTML += '<tr><td class="key">' + key + '</td><td class="wrapword text-break">' + value + '</td></tr>';
                        }
                        else {
                            if (Number.isInteger(value) || (typeof value === 'boolean')) {
                                metadataContainerHTML += '<tr><td class="key">' + key + '</td><td class="wrapword text-break">' + value + '</td></tr>';
                            }
                            else {
                                if (Object.keys(value).length > 0) {
                                    for (var subkey in value) {
                                        if (!excluding_fields.includes(subkey)) {
                                            if (value[subkey]) {
                                                metadataContainerHTML += '<tr><td class="key">' + key + '/' + subkey + '</td><td class="wrapword text-break">' + value[subkey] + '</td></tr>';
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        metadataContainerHTML += '</tbody>';
        metadataContainerHTML += '</table></div><br/>';

        $.postAPIJSON('/api/document/getmetadata',
            {
                path: result.metadata_storage_path
            },
            function (data) {
                if (data && data.length > 0) {
                    try {
                        result = JSON.parse(data)
                        var extraMetadataContainerHTML = $("#metadata-viewer").html();

                        extraMetadataContainerHTML += '<h4 id="available_metadata">File Metadata</h4><div style="overflow-x:auto;">';
                        extraMetadataContainerHTML += '<table class="table metadata-table table-hover table-striped">';
                        extraMetadataContainerHTML += '<thead><tr><th data-field="key" class="key">Key</th><th data-field="key" class="key">Normalized</th><th data-field="value">Value</th></tr></thead>';
                        extraMetadataContainerHTML += '<tbody>';

                        var keys = Object.keys(result).sort();

                        for (var k = 0; k < keys.length; k++) {
                            var key = keys[k];
                            if (result.hasOwnProperty(key)) {
                                if (!key.startsWith("X-TIKA")) {
                                    extraMetadataContainerHTML += '<tr><td class="key">' + key + '</td><td class="key">' + Microsoft.Utils.replaceAll(Microsoft.Utils.replaceAll(key," ","-"),":","-") + '</td><td class="wrapword text-break">' + result[key] + '</td></tr>';                                    
                                }
                            }
                        };

                        extraMetadataContainerHTML += '</tbody>';
                        extraMetadataContainerHTML += '</table></div><br/>';

                        $("#metadata-viewer").html(extraMetadataContainerHTML);
                    }
                    catch (exception) {
                    }
                }
            });

        return metadataContainerHTML;
    }
}

// Entities Tokens HTML

Microsoft.Search = Microsoft.Search || {};
Microsoft.Search.Results.Tokens = Microsoft.Search.Results.Tokens || {};
Microsoft.Search.Results.Tokens = {
    render_tab: function (result) {

        var tokensContainerHTML = $("#tokens-viewer").html();

        if (result.tokens_html) {
            if (result.tokens_html.length > 0) {
                tokensContainerHTML += '<div class="entities-visualization">'
                for (var i = 0; i < result.tokens_html.length; i++) {
                    tokensContainerHTML += result.tokens_html[i]
                }
                tokensContainerHTML += '</div>'
            }
        }
        return tokensContainerHTML;
    }
}


// EMBEDDED Images

Microsoft.Search = Microsoft.Search || {};
Microsoft.Search.Results.Embedded = Microsoft.Search.Results.Embedded || {};
Microsoft.Search.Results.Embedded = {
    render_tab: function (result) {
        // Embedded Images Tab content
        var embeddedContainerHTML = '';
        var pathExtension = result.metadata_storage_path.toLowerCase().split('.').pop();

        if (!result.document_embedded) {
            // Show the Embedded Images if relevant
            if (!Microsoft.Utils.IsImageExtension(pathExtension)) {
                embeddedContainerHTML = this.render_embedded_results(result);
            }
        }
        return embeddedContainerHTML;
    },
    render_embedded_results: function (result) {

        var containerHTML = '<div class="progress"><div class="progress-bar progress-bar-striped bg-danger" role = "progressbar" style = "width: 100%" aria - valuenow="100" aria - valuemin="0" aria - valuemax="100"></div></div>';

        $.postAPIJSON('/api/document/getembedded',
            {
                document_id: result.document_id
            },
            function (data) {

                var containerHTML = '';

                // List of embedded images 
                if (data && data.count > 0) {
                    var results = data.results;

                    containerHTML += '<div class="imagesResults">';

                    for (var i = 0; i < results.length; i++) {

                        containerHTML += Microsoft.Images.render_image_result(results[i]);
                    }

                    containerHTML += '</div>';

                    $('#images-pivot-link').append(' (' + data.count + ')');

                }
                else {
                    $('#images-pivot-link').hide();
                }

                $('#images-viewer').html(containerHTML);
            });

        return containerHTML;
    }
}

// SIBLINGS 

Microsoft.Search = Microsoft.Search || {};
Microsoft.Search.Results.Siblings = Microsoft.Search.Results.Siblings || {};
Microsoft.Search.Results.Siblings = {
    render_tab: function (result, tabular) {
        var containerHTML = '';
        var pathExtension = result.metadata_storage_path.toLowerCase().split('.').pop();

        if (result.document_embedded) {
            if (Microsoft.Utils.IsImageExtension(pathExtension)) {
                containerHTML = this.render_siblings_results(result, tabular);
            }
        }
        return containerHTML;
    },
    render_siblings_results: function (result, tabular) {

        var containerHTML = '<div class="progress"><div class="progress-bar progress-bar-striped bg-danger" role = "progressbar" style = "width: 100%" aria - valuenow="100" aria - valuemin="0" aria - valuemax="100"></div></div>';

        $.postAPIJSON('/api/document/getsiblings',
            {
                document_id: result.image_parentid,
                incomingFilter: "image_parentid eq '" + result.image_parentid + "' ",
                parameters: {
                    RowCount: 50,
                    inOrderBy: ["page_number asc"]
                }
            },
            function (data) {
                Microsoft.Search.Results.Siblings.append_siblings(data.results);
            });

        return containerHTML;
    },
    append_siblings: function (results) {

        var fileContainerHTML = '';
        $('#siblings-viewer').html(fileContainerHTML);

        if (results && results.length > 0) {

            for (var i = 0; i < results.length; i++) {

                var docresult = results[i].Document !== undefined ? results[i].Document : results[i];

                var path = docresult.metadata_storage_path;
                var pathLower = path.toLowerCase();
                var pathExtension = pathLower.split('.').pop();

                fileContainerHTML += '  <div class="image-container border border-1 mb-2">';

                if (Microsoft.Utils.IsTIFFImage(pathExtension)) {
                    fileContainerHTML += '<img id="image-rotate" rotate="0" class="image-viewport img-fluid" title="Sibling Viewer" src="data:image/png;base64, ' + docresult.image.image_data + '"/>';
                }
                else {
                    fileContainerHTML += '<img id="image-rotate" rotate="0" class="image-viewport img-fluid" title="Sibling Viewer" src="' + Microsoft.Search.GetSASTokenFromPath(path) + '"/>';
                }

                fileContainerHTML += '  </div>';
            }
        }

        $('#siblings-viewer').append(fileContainerHTML);
    }
}

// PARENT (Source) document

Microsoft.Search = Microsoft.Search || {};
Microsoft.Search.Results.Parent = Microsoft.Search.Results.Parent || {};
Microsoft.Search.Results.Parent = {
    render_tab_with_fallback: function (result, tabular) {
        return this.render_tab(result, tabular, true);
    },
    render_tab: function (result, tabular, fallback = false) {
        // Embedded Images Tab content
        var embeddedContainerHTML = '';

        if (result.document_embedded) {
            embeddedContainerHTML = Microsoft.Search.Results.render_file_container(result.document_embedded, Microsoft.Utils.GetParentPathFromImage(result), result.image_data, result.page_number);
        }
        else {
            if (fallback && Microsoft.Utils.IsPDF(result.metadata_storage_path)) {
                embeddedContainerHTML = Microsoft.Search.Results.RenderSearchResultPreview(result)
            }
        }

        return embeddedContainerHTML;
    }
}


// TRANSCRIPT

Microsoft.Search = Microsoft.Search || {};
Microsoft.Search.Results.Transcript = Microsoft.Search.Results.Transcript || {};
Microsoft.Search.Results.Transcript = {

    // All functions about Transcript 

    RenderTranscriptHTML: function (result) {

        var transcriptContainerHTML = '';

        var full_content = "";

        // If we have merged content, let's use it.
        if (result.merged_content) {
            if (result.merged_content.length > 0) {
                full_content = this.RenderTranscript(result.merged_content);
            }
        }
        else {
            if (result.content) {
                if (result.content.length > 0) {
                    full_content = this.RenderTranscript(result.content);
                }
            }
        }

        if (full_content === null || full_content === "") {
            // not much to display
            return '';
        }

        var full_translated_text = "";
        if (!!result.translated_text && result.translated_text !== null && result.language !== "en") {
            if (result.translated_text.length > 0) {
                full_translated_text = this.RenderTranscript(result.translated_text);
            }
        }

        if (full_translated_text.length > 0) {
            transcriptContainerHTML += '<div style="overflow-x:initial;"><table class="table"><thead><tr><th>Original Content</th><th>Translated (En)</th></tr></thead>';
            transcriptContainerHTML += '<tbody>';
            transcriptContainerHTML += '<tr><td class="wrapword text-break" style="width:50%"><div id="transcript-viewer-pre">' + full_content + '</div></td><td class="wrapword text-break"><div id="translated-transcript-viewer-pre">' + full_translated_text + '</div></td></tr>';
            transcriptContainerHTML += '</tbody>';
            transcriptContainerHTML += '</table></div>';
        }
        else {
            transcriptContainerHTML += '<div style="overflow-x:initial;"><table class="table"><thead><tr><th>Original Content</th></tr></thead>';
            transcriptContainerHTML += '<tbody>';
            transcriptContainerHTML += '<tr><td class="wrapword text-break"><div id="transcript-viewer-pre">' + full_content + '</div></td></tr>';
            transcriptContainerHTML += '</tbody>';
            transcriptContainerHTML += '</table></div>';
        }

        return transcriptContainerHTML;
    },

    RenderTranscript: function (content) {
        var full_content = '';
        var lines = content.trim().split('\n');
        lines.forEach((line, idx) => {
            var classname = "p-highlight-" + idx;
            full_content += '<p class="p-highlight ' + classname + ' text-break" onmouseover="Microsoft.Results.Details.pMouseOver(\'' + classname + '\');" onmouseout="Microsoft.Results.Details.pMouseOut(\'' + classname + '\');" >' + Microsoft.Utils.htmlDecode(line).trim() + '</p>';
        });

        return full_content;
    },

    FindMatches: function (regex, transcriptText) {
        var i = -1;
        var response = transcriptText.replace(regex, function (str) {
            i++;
            var shortname = str.slice(0, 20).replace(/[^a-zA-Z ]/g, " ").replace(new RegExp(" ", 'g'), "_");
            return '<span id=\'' + i + '_' + shortname + '\' class="highlight">' + str + '</span>';
        });

        return { html: response, count: i + 1 };
    },

    GetReferences: function (searchText, targetTranslatedText) {

        if (searchText === '*')
            return;

        var targetTag = '#transcript-viewer-pre';

        var transcriptText;
        var lines = [];
        var ptags = $(targetTag).find('p');

        for (var i = 0; i < ptags.length; i++) {
            lines.push(ptags[i].textContent);
        }

        if (!targetTranslatedText) {
            $('#reference-viewer').empty();
            transcriptText = $(targetTag).text();
        }
        else {
            transcriptText = $(targetTag).html();
        }

        var phraseSearch = searchText;
        //var phraseSearch = searchText.trim();
        //phraseSearch += "\b";
        //phraseSearch = phraseSearch.replace(" ", "\b ");

        // find all matches in transcript
        var regex = new RegExp(phraseSearch, 'gi');

        var response = [];

        // Round #1
        // for each line try to find PhraseSearch reference first
        lines.forEach((line, lineidx) => {
            var line_response = this.FindMatches(regex, line);
            if (line_response.count > 0) response.push(line_response);
        });

        // Round #2
        if (response.count === 0) {
            // for each line try to find Tokens references
            lines.forEach((line, lineidx) => {
                var tokens = searchText.split(' ');
                if (tokens.length > 1) {
                    regex = new RegExp("(" + tokens.join('|') + ")", 'gi');
                    var line_response = this.FindMatches(regex, line);
                    if (line_response.count > 0) response.push(line_response);
                }
            });
        }

        //var response = this.FindMatches(regex, transcriptText);

        //// if the phrase search doesn't return anything let's try tokens
        //if (response.count === 0) {
        //    var tokens = searchText.split(' ');
        //    if (tokens.length > 1) {
        //        regex = new RegExp("(" + tokens.join('|') + ")", 'gi');
        //        response = this.FindMatches(regex, transcriptText);
        //    }
        //}

        // Round #3 
        // If no response found in the original text, go to the translated text
        if (response.count === 0) {
            regex = new RegExp(phraseSearch, 'gi');

            // Do another round on the translated text
            targetTag = '#translated-transcript-viewer-pre';
            transcriptText = $(targetTag).text();
            if (transcriptText.length > 0) {
                lines.forEach((line, lineidx) => {
                    var line_response = this.FindMatches(regex, line);
                    if (line_response.count > 0) response.push(line_response);
                });
            }
        }

        if (response.count > 0) {
            $(targetTag).html(response.html);
        }

        // for each match, select prev 50 and following 50 characters and add selections to list
        var transcriptCopy = transcriptText;

        // Calc height of reference viewer
        var contentHeight = $('.ms-Pivot-content').innerHeight();
        var tagViewerHeight = $('#tag-viewer').innerHeight();
        var detailsViewerHeight = $('#details-viewer').innerHeight();

        $('#reference-viewer').css("height", contentHeight - tagViewerHeight - detailsViewerHeight - 110);

        $.each(transcriptCopy.match(regex), function (index, value) {

            var startIdx;
            var ln = 400;

            if (value.length > 150) {
                startIdx = transcriptCopy.indexOf(value);
                ln = value.length;
            }
            else {
                if (transcriptCopy.indexOf(value) < 200) {
                    startIdx = 0;
                }
                else {
                    startIdx = transcriptCopy.indexOf(value) - 200;
                }

                ln = 400 + value.length;
            }

            var reference = transcriptCopy.substr(startIdx, ln);
            transcriptCopy = transcriptCopy.replace(value, "");

            reference = reference.replace(value, function (str) {
                return '<span class="highlight">' + str + '</span>';
            });

            var shortName = value.slice(0, 20).replace(/[^a-zA-Z ]/g, " ").replace(new RegExp(" ", 'g'), "_");

            $('#reference-viewer').append('<li class=\'reference list-group-item\' onclick=\'Microsoft.Search.Transcript.GoToReference("' + index + '_' + shortName + '")\'>...' + reference + '...</li>');
        });
    },

    GoToReference: function (selector) {

        var triggerEl = document.querySelector('#details-pivot-links a[href="#transcript-pivot"]')

        var tabinstance = bootstrap.Tab.getInstance(triggerEl);

        if (tabinstance) {
            tabinstance.show();
        }
        else {
            var tab = new bootstrap.Tab(triggerEl);
            tab.show();
        }

        var container = $('#transcript-viewer');
        var scrollTo = $("#" + selector);

        container.animate({
            scrollTop: scrollTo.offset().top - container.offset().top + container.scrollTop()
        });
    },

    GetMatches: function (string, regex, index) {
        var matches = [];
        var match;
        while (match === regex.exec(string)) {
            matches.push(match[index]);
        }
        return matches;
    },

    GetSearchReferences: function (q) {
        var copy = q;

        copy = copy.replace(/~\d+/gi, "");
        var matches = this.GetMatches(copy, /\w+/gi, 0);

        matches.forEach(function (match) {
            Microsoft.Search.Transcript.GetReferences(match, true);
        });
    },

    SearchTranscript: function (searchText) {
        $('#reference-viewer').empty();

        if (searchText !== "") {
            // get whole phrase
            Microsoft.Search.Results.Transcript.GetReferences(searchText, false);
        }
    }

}

// export default Microsoft.Search;