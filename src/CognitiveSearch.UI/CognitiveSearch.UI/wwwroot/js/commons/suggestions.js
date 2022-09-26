// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

//
// SUGGESTIONS
//
Microsoft.Suggestions = Microsoft.Suggestions || {};
Microsoft.Suggestions = {
    suggestions: [],

    suggestions_dict : {},
    engines_dict: {},

    init: function () {
        return new Promise((resolve, reject) => {
            $.ajax({
                type: 'GET',
                url: '/config/suggestions.json',
                dataType: 'json',
                success: function (data) {
                    Microsoft.Suggestions.suggestions = data;
                    resolve()
                },
                error: function (error) {
                    reject(error)
                },
            })
        });
    },

    configure: function (vertical) {

        var suggestions = vertical?.suggestions ? vertical.suggestions : Microsoft.Suggestions.suggestions;

        if (suggestions.length === 0) {
            suggestions = this.suggestions;
        }

        var suggestions_engines=[];

        for (var i = 0; i < suggestions.length; i++) {

            var entry = suggestions[i];

            var remote_url = entry.url;

            if (vertical?.filter) {
                remote_url += ('&filter='+vertical.filter);
            }

            // WebAPI Support
            var backendurl = remote_url;

            if (Microsoft.Config.data.webAPIBackend.isEnabled) {
                backendurl = Microsoft.Config.data.webAPIBackend.endpoint + remote_url;
            }

            var engine = new Bloodhound({
                datumTokenizer: Bloodhound.tokenizers.whitespace,
                queryTokenizer: Bloodhound.tokenizers.whitespace,
                remote: {
                    url: backendurl,
                    prepare: function (query, settings) {
                        settings.url = settings.url + '&term=' + query
                        settings.type = "POST";

                        // WebAPI Support - Authentication
                        //settings.contentType = "application/json; charset=UTF-8";
                        //settings.data = JSON.stringify(query);

                        return settings;
                    }
                }
            });

            var config = {
                name: entry.name,
                target: (entry.target ? entry.target : entry.name),
                source: engine,
                templates: entry.template
            }

            suggestions_engines.push(config);

            this.suggestions_dict[entry.name]=config
        }

        const key = vertical?.id ? vertical.id : "default";

        this.engines_dict[key] = suggestions_engines;

        // Typeahead - passing in `null` for the `options` arguments will result in the default
        // options being used
        $('#scrollable-dropdown-menu .typeahead').typeahead({ highlight: true }, ...Microsoft.Suggestions.engines_dict[key]);

        //$('.typeahead').bind('typeahead:change', function (ev, suggestion) {
        //    console.log('Suggestions change: ' + suggestion);
        //});

        $('.typeahead').bind('typeahead:select', function (ev, suggestion, dataset) {

            var suggestion_target = Microsoft.Suggestions.suggestions_dict[dataset].target;

            if (Microsoft.Search.Options.suggestionsAsFilter) {
                // Create a filter to target explicit documents and remove the full text noise
                Microsoft.Facets.ChooseFacetWithQueryAll(suggestion_target, Base64.encode(suggestion));
            }
            else {
                // Do a full text search with the selection suggestion
                Microsoft.Search.ReSearch(Microsoft.Suggestions.RemoveCharacters(suggestion));
            }
        });

    },
    RemoveCharacters: function (query) {
        return Microsoft.Utils.htmlEncode(query.replace(/-/g, ' ').replace(/_/g, ' ').replace(/&/g, ' '));
        
    }
}

//Microsoft.Suggestions.init().then(() => Microsoft.Suggestions.configure());
Microsoft.Suggestions.init();

// export default Microsoft.Suggestions;