// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

//
// NEWS FEED Methods
//
Microsoft.News = Microsoft.News || {};
Microsoft.News = {
    MAX_NUMBER_ITEMS_PER_PAGE: 20,
    LIVE_NEWS_TAG: "#livenews-feed",
    LATEST_NEWS_TAG: "#news-feed-section",

    view_result_tag: "#news-results-content",
    
    news_facets: [],
    selected_feeds: [],

    init: function () {
        return new Promise((resolve, reject) => {
            $.ajax({
                type: 'GET',
                url: '/config/news-facets.json',
                dataType: 'json',
                success: function (data) {
                    Microsoft.News.news_facets = data;
                    resolve()
                },
                error: function (error) {
                    reject(error)
                },
            })
        });
    },

    GetAllNewsFeeds: function () {
        var feedKeys = Object.keys(this.news_facets);

        var allFeeds = [];
        for (var j = 0; j < feedKeys.length; j++) {
            var feed_key=feedKeys[j];
            for (var i = 0; i < this.news_facets[feed_key].length; i++) {
                var feed = this.news_facets[feed_key][i];
                allFeeds.push(feed);
            }
        }
        return allFeeds;
    },
    
    GetHomeNewsFeeds: function () {
        // filter out feeds that are not included on the home page 
        var allFeeds = this.GetAllNewsFeeds().filter(function (feed) { return feed.includeHomePage }); 

        return allFeeds;
    },

    FetchHomeNews: function () {
        this.init().then(() => {
            this.GetLiveNews();
        });
    },

    GetLiveNews: function () {
        this.GetLiveNewsFeed(this.RenderHomeNewsList);
    },

    GetLiveNewsFeed: function (update_method) {
        var selectedFeeds = this.GetHomeNewsFeeds();

        $.postXML('/api/news/getliveaggregatedfeed', selectedFeeds, update_method);
    },

    // Carousel aren't accessible friendly.
    RenderHomeNewsCarousel: function (data, status, xhr) {
        var resultsHtml = '';

        var feeds = [];

        var xmlDoc = xhr.responseXML;

        // access to XML nodes and get node values
        var channel = xmlDoc.getElementsByTagName('channel');
        var items = xmlDoc.getElementsByTagName('item');

        for (var i = 0; i < items.length; i++) {
            var entry = items[i];

            var feed_entry = {};

            for (var j = 0; j < entry.children.length; j++) {
                var tag = entry.children[j];

                if (feed_entry[tag.tagName]) {
                    feed_entry[tag.tagName] += '||' + tag.textContent;
                }
                else {
                    if (tag.tagName === "media:content") {
                        feed_entry[tag.tagName] = tag.getAttribute('url');
                    }
                    else {
                        feed_entry[tag.tagName] = tag.textContent;
                    }
                }
            }
            feeds.push(feed_entry);
        }

        // Redering the feeds item
        for (var i = 0; i < feeds.length; i++) {
            var feed_entry = feeds[i];
            resultsHtml += '<div>'

            resultsHtml += '<div class="news-carousel-item">'

            resultsHtml += '<div class="news-carousel-item-body rounded">'

            resultsHtml += '<h4 class="news-carousel-ìtem-title" id="item-' + i + '">';
            resultsHtml += '<a target="_blank" href="' + feed_entry.link + '">' + feed_entry.title + '</a>';
            resultsHtml += '</h4>';

            if (feed_entry.pubDate) {
                var d = new Date(feed_entry.pubDate);
                resultsHtml += '<h6 class="news-carousel-item-date">' + d.toLocaleString() + '</h6>';

                if (feed_entry.description) {
                    resultsHtml += '<span class="news-carousel-item-description">';
                    resultsHtml += feed_entry.description;
                    resultsHtml += '</span>';
                }
            }
            if (feed_entry['media:content']) {
                if (feed_entry['media:description']) {
                    resultsHtml += '<img title="' + feed_entry['media:description'] + '" src="' + feed_entry['media:content'] + '" class="news-carousel-image img-fluid rounded float-right" onError="this.onerror=null;this.src=\'/images/blank.png\';"></img>';
                }
                else {
                    resultsHtml += '<img src="' + feed_entry['media:content'] + '" class="news-carousel-image img-fluid rounded float-right" onError="this.onerror=null;this.src=\'/images/blank.png\';"></img>';
                }
            }
            resultsHtml += '</div>';
            resultsHtml += '</div>';

            resultsHtml += '</div>';
        }

        $(Microsoft.News.LIVE_NEWS_TAG).html(resultsHtml);

        $(Microsoft.News.LIVE_NEWS_TAG).not('.slick-initialized').slick({
            infinite: true,
            slidesToShow: 3,
            slidesToScroll: 2,
            nextArrow: '<span class="bi bi-chevron-right nextArrowBtn btn btn-dark"></span>',
            prevArrow: '<span class="bi bi-chevron-left prevArrowBtn btn btn-dark"></span>',
            variableWidth: true,
            //adaptiveHeight: true,
            autoplay: true,
            autoplaySpeed: 5000
        });
    },

    RenderHomeNewsList: function (data, status, xhr) {

        var feeds = [];

        var xmlDoc = xhr.responseXML;

        // access to XML nodes and get node values
        var channel = xmlDoc.getElementsByTagName('channel');
        var items = xmlDoc.getElementsByTagName('item');

        for (var i = 0; i < items.length; i++) {
            var entry = items[i];

            var feed_entry = {};

            for (var j = 0; j < entry.children.length; j++) {
                var tag = entry.children[j];

                if (feed_entry[tag.tagName]) {
                    feed_entry[tag.tagName] += '||' + tag.textContent;
                }
                else {
                    if (tag.tagName === "media:content") {
                        feed_entry[tag.tagName] = tag.getAttribute('url');
                    }
                    else {
                        feed_entry[tag.tagName] = tag.textContent;
                    }
                }
            }

            feeds.push(feed_entry);
        }

        Microsoft.News.RenderLiveNewsList(feeds, false, Microsoft.News.LIVE_NEWS_TAG);

    },

    ClearFeed: function () {
        $(Microsoft.News.LATEST_NEWS_TAG).empty();
    },

    // LIVE NEWS REGION
    LiveNewsSearch: function () {

        Microsoft.Search.setQueryInProgress(); 
        
        if (this.selected_feeds.length > 0) {
            $.postXML('/api/news/getliveaggregatedfeed', this.selected_feeds, Microsoft.News.LiveNewsUpdate);
        }
        else {
            $.postXML('/api/news/getliveaggregatedfeed', this.GetAllNewsFeeds(), Microsoft.News.LiveNewsUpdate);
        }
    },

    LiveNewsUpdate: function (data, status, xhr) {

        var feeds = [];

        var xmlDoc = xhr.responseXML;

        // access to XML nodes and get node values
        var channel = xmlDoc.getElementsByTagName('channel');
        var items = xmlDoc.getElementsByTagName('item');

        for (var i = 0; i < items.length; i++) {
            var entry = items[i];

            var feed_entry = {};

            for (var j = 0; j < entry.children.length; j++) {
                var tag = entry.children[j];

                if (feed_entry[tag.tagName]) {
                    feed_entry[tag.tagName] += '||' + tag.textContent;
                }
                else {
                    if (tag.tagName === "media:content") {
                        feed_entry[tag.tagName] = tag.getAttribute('url');
                    }
                    else {
                        feed_entry[tag.tagName] = tag.textContent;
                    }
                }
            }

            feeds.push(feed_entry);
        }

        Microsoft.News.RenderLiveNewsResults(feeds, feeds);

        $("#doc-count").html(' Found ' + feeds.length.toLocaleString('en-US') + ' news articles.');

        //Filters
        Microsoft.News.UpdateFeedFilterReset();

        Microsoft.Search.setQueryCompleted();
    },

    RenderLiveNewsResults: function (nav_feeds, feeds, quickactions = false) {

        this.RenderLiveNewsNavigation(nav_feeds);

        this.RenderLiveNewsList(feeds, quickactions);
    },

    RenderLiveNewsNavigation: function (nav_feeds, targetTag = '#navbar-feeds') {
        var resultsHtml = '';

        resultsHtml += '<a class="navbar-brand" href="javascript:void(0)">Latest News</a>';
        resultsHtml += '<nav class="nav nav-pills flex-column">';
        resultsHtml += '';
        resultsHtml += '';

        // Navigation Feeds
        for (var i = 0; i < nav_feeds.length; ++i) {
            var navfeed = nav_feeds[i];

            resultsHtml += '<a class="nav-link" href="#item-' + i + '">' + navfeed.title + '</a>';

        }

        resultsHtml += '</nav>';

        $(targetTag).html(resultsHtml);
    },

    RenderLiveNewsList: function (feeds, quickactions = false, targetTag = Microsoft.News.view_result_tag) {

        var resultsHtml = '<div data-bs-spy="scroll" data-bs-target="#navbar-feeds" data-bs-offset="0" tabindex="0">';

        for (var i = 0; i < feeds.length; i++) {
            var feed_entry = feeds[i];
            resultsHtml += '<div class="row news-feed-row rounded">'

            resultsHtml += '<div class="col-md-10">'
            resultsHtml += '<h4 id="item-' + i + '">' + '<a target="_blank" href="' + feed_entry.link + '">' + feed_entry.title + '</a></h4>';

            if (feed_entry.pubDate) {
                var d = new Date(feed_entry.pubDate);
                resultsHtml += '<h6>' + d.toLocaleString() + '</h6>';
                if (feed_entry.description) {
                    resultsHtml += '<p>';
                    resultsHtml += feed_entry.description;
                    resultsHtml += '</p>';
                }
            }

            if (feed_entry.category) {
                var categories = feed_entry.category.split('||');
                for (var k = 0; k < categories.length; k++) {
                    resultsHtml += '<button class="tag">' + categories[k] + '</button>'
                }
            }

            resultsHtml += '</div>';

            // Preview part
            resultsHtml += '<div class="col-md-2">'
            if (quickactions) {
                resultsHtml += this.renderNewsQuickActions(feed_entry);
            }

            if (feed_entry['media:content']) {
                if (feed_entry['media:description']) {
                    resultsHtml += '<img title="' + feed_entry['media:description'] + '" src="' + feed_entry['media:content'] + '" class="img-fluid rounded float-right" onError="this.onerror=null;this.src=\'/images/blank.png\';" ></img>';
                }
                else {
                    resultsHtml += '<img title="'+feed_entry.title+'" src="' + feed_entry['media:content'] + '" class="img-fluid rounded float-right" onError="this.onerror=null;this.src=\'/images/blank.png\';" ></img>';
                }
            }

            resultsHtml += '</div>';

            resultsHtml += '</div>';
        }
        resultsHtml += '</div>';

        $(targetTag).html(resultsHtml);
    },

    ClearNewsResults: function (targetTag = Microsoft.News.view_result_tag) {
        $(targetTag).empty();
    },

    UpdateNewsFacets: function (facets, alwaysOpen = true) {

        $("#news-facet-nav").empty();

        var facetResultsHTML = '<div class="accordion accordion-flush" id="facets-accordion">';

        if (facets) {

            for (var item in facets) {
                var name = item;
                var nameid = Microsoft.News.normalizeFacetId(item);
                var data = facets[item];
                var facetId = nameid + '-facets';

                facetResultsHTML += '<div class="accordion-item">';
                facetResultsHTML += '<h2 class="accordion-header accordion-item-' + nameid + '" id="' + facetId + '">';
                facetResultsHTML += '<button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#' + nameid + '" aria-expanded="true" aria-controls="' + nameid + '">';
                facetResultsHTML += name;
                facetResultsHTML += '</button>';
                facetResultsHTML += '</h2>';

                if (alwaysOpen) {
                    facetResultsHTML += '<div id="' + nameid + '" class="accordion-collapse collapse" role="group" aria-labelledby="' + facetId + '">';
                }
                else {
                    facetResultsHTML += '<div id="' + nameid + '" class="accordion-collapse collapse" role="group" aria-labelledby="' + facetId + '" data-bs-parent="#facets-accordion">';
                }

                facetResultsHTML += '<div class="accordion-body">';

                if (data !== null) {
                    // Enforce Source names sorting
                    data = data.sort((a, b) => (a.Source > b.Source) ? 1 : -1);

                    for (var j = 0; j < data.length; j++) {
                        facetResultsHTML += '<div class="form-check">';

                        var facet_value = Base64.encode(data[j].Source).replaceAll("=", "");
                        var facet_raw_value = data[j].Source;
                        var facet_feed_url = Base64.encode(data[j].RSSFeedURL);

                        facetResultsHTML += '   <input class="form-check-input facet-checkbox" type="checkbox"  id="' + nameid + '_' + facet_value + '" onclick="Microsoft.News.ChooseFeed(\'' + nameid + '\',\'' + facet_raw_value + '\',\'' + facet_feed_url + '\', \'' + j + '\');">';
                        facetResultsHTML += '   <label class="form-check-label"for="' + nameid + '_' + facet_value + '" >';

                        if (data[j].count) {
                            facetResultsHTML += '   <span>' + facet_raw_value + ' (' + data[j].count + ')</span> ';
                        }
                        else {
                            facetResultsHTML += '   <span>' + facet_raw_value + '</span> ';
                        }
                        facetResultsHTML += '   </label>';
                        facetResultsHTML += '</div>';
                    }
                }

                facetResultsHTML += '</div></div></div>';
            }
        }

        facetResultsHTML += '</div>';

        $("#news-facet-nav").append(facetResultsHTML);

    },

    ChooseFeed: function (source, key, value, position) {
        value = Base64.decode(value);

        if (this.selected_feeds !== undefined) {

            //selected_feeds.push(value);

            // facetValues where key == selected facet
            var result = this.selected_feeds.filter(function (f) { return f.id === key; })[0];

            if (result) { // if that facet exists
                var idx = this.selected_feeds.indexOf(result);

                this.selected_feeds.splice(idx, 1);
            }
            else {
                this.selected_feeds.push({
                    id: key,
                    source: source,
                    rssfeedurl: value
                });
            }
        }

        Microsoft.Search.ReSearch();
    },

    RemoveFeed: function (source, facet_key) {

        var nameid = source + "_" + Base64.encode(facet_key).replaceAll("=", "");

        if ($('#' + nameid)) {
            $('#' + nameid).prop('checked', false);
        }

        // Find the feed facet if it exists
        var result = this.selected_feeds.filter(function (f) { return f.id === facet_key; })[0];

        if (result) { // if that facet exists
            var idx = this.selected_feeds.indexOf(result);

            this.selected_feeds.splice(idx, 1);
        }

        Microsoft.Search.ReSearch();
    },

    ClearAllNewsFilters: function () {
        if (this.selected_feeds.length > 0) {
            $('.facet-checkbox').prop('checked', false);
            $('.facet-button').hide();
            this.selected_feeds = [];
            $("#filterReset").empty();
            Microsoft.Search.ReSearch();
        }
    },

    UpdateFeedFilterReset: function (appendMode = false) {

        var htmlString = '';

        if (this.selected_feeds && this.selected_feeds.length > 0) {

            htmlString += '<div class="btn-group" role="group" aria-label="Filters">';

            this.selected_feeds.forEach(function (item, index, array) {
                htmlString += '<button type="button" class="btn btn-outline-primary btn-sm me-2">';
                htmlString += item.id + ' <a class="filter-anchor" title="' + item.source + '" href="javascript:void(0)" onclick="Microsoft.News.RemoveFeed(\'' + item.source + '\',\'' + item.id + '\')"><span class="bi bi-x text-primary"></span></a><br>';
                $('#' + Microsoft.News.normalizeFacetId(item.id) + '_' + index).addClass('is-checked');
                htmlString += '</button>';
            });

            htmlString += '</div>';
        }

        if (appendMode) {
            $("#filterReset").append(htmlString);
        }
        else {
            $("#filterReset").html(htmlString);
        }
    },

    normalizeFacetId: function (key) {
        return key.toLowerCase().replaceAll(" ", "-").replaceAll("&", "");
    },

    // NEWS QUICK ACTIONS 
    renderNewsQuickActions: function (feed_entry) {
        var htmlDiv = ''

        htmlDiv += '    <div class="col-md-12" style="padding: 5px;">';
        htmlDiv += '            <div class="d-grid gap-2 d-md-flex" >';
        htmlDiv += '                <button type="button" class="btn btn-outline-info btn-sm" onclick="previewNews(\'' + feed_entry.link + '\');">'
        htmlDiv += '                    <i class="oi oi-magnifying-glass"></span>'
        htmlDiv += '                </button>'
        htmlDiv += '                <button type="button" class="btn btn-outline-warning btn-sm" onclick="window.alert(\'Custom Action 3\');">'
        htmlDiv += '                    <i class="oi oi-cloud-upload"></span>'
        htmlDiv += '                </button>'
        htmlDiv += '            </div>';
        htmlDiv += '    </div>';

        return htmlDiv;
    },

    previewNews: function (feed_url) {

        $('#details-modal-body').html('<embed src="' + feed_url + '" />');

        $('#preview-modal').modal('show');
    }

}

// export default Microsoft.News;