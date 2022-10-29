// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// EMBEDDED Images

Microsoft.Search = Microsoft.Search || {};
Microsoft.Search.Results = Microsoft.Search.Results || {};
Microsoft.Search.Results.Embedded = Microsoft.Search.Results.Embedded || {};
Microsoft.Search.Results.Embedded = {
    render_tab: function (result) {
        // Embedded Images Tab content
        var embeddedContainerHTML = '';
        var pathExtension = result.metadata_storage_path.toLowerCase().split('.').pop();

        if (!result.document_converted) {
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

                // List of embedded documents : images or attachments
                if (data && data.count > 0) {
                    var results = data.results;

                    if (results[0].Document.content_group === 'Image') {

                        containerHTML += '<div class="imagesResults">';

                        for (var i = 0; i < results.length; i++) {
                            containerHTML += Microsoft.Images.render_image_result(results[i]);
                        }
                    }
                    else { 
                        containerHTML += '<div class="row">';
                        containerHTML += Microsoft.All.UpdateResultsAsList(results);
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