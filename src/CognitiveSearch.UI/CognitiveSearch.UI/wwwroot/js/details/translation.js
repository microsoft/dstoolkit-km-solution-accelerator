// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// Translation Images

Microsoft.Search = Microsoft.Search || {};
Microsoft.Search.Results = Microsoft.Search.Results || {};
Microsoft.Search.Results.Translation = Microsoft.Search.Results.Translation || {};
Microsoft.Search.Results.Translation = {

    render_tab: function (result) {
        // Translation Images Tab content
        var TranslationContainerHTML = '';
        var pathExtension = result.metadata_storage_path.toLowerCase().split('.').pop();

        if (!result.document.converted) {
            // Show the Translation Images if relevant
            if (!Microsoft.Utils.IsImageExtension(pathExtension)) {
                TranslationContainerHTML = this.render_Translation_results(result);
            }
        }
        return TranslationContainerHTML;
    },

    render_Translation_results: function (result) {

        var containerHTML = '<div class="progress"><div class="progress-bar progress-bar-striped bg-danger" role = "progressbar" style = "width: 100%" aria - valuenow="100" aria - valuemin="0" aria - valuemax="100"></div></div>';

        $.postAPIJSON('/api/document/getsiblings',
            {
                document_id: result.document_id,
                incomingFilter: "(parent/id eq '" + result.document_id + "') and (document/embedded eq false)",
            },
            function (data) {

                var containerHTML = '';

                if (data && data.count > 0) {
                    var results = data.results;

                    containerHTML += '<div class="row">';
                    containerHTML += Microsoft.All.UpdateResultsAsList(results);
                    containerHTML += '</div>';

                    $('#translation-pivot-link').append(' (' + data.count + ')');
                }
                else {
                    $('#translation-pivot-link').hide();
                }

                $('#translation-viewer').html(containerHTML);
            });

        return containerHTML;
    }
}
