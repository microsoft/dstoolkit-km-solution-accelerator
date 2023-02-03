// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// Entities Tokens HTML

Microsoft.Search = Microsoft.Search || {};
Microsoft.Search.Results = Microsoft.Search.Results || {};
Microsoft.Search.Results.Tokens = Microsoft.Search.Results.Tokens || {};
Microsoft.Search.Results.Tokens = {
    render_tab: function (result, tabular, targetid="#tokens-viewer") {

        var tokensContainerHTML = $(targetid).html();

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

