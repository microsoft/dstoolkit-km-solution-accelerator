// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// HTML

Microsoft.Search = Microsoft.Search || {};
Microsoft.Search.Results = Microsoft.Search.Results || {};
Microsoft.Search.Results.HTML = Microsoft.Search.Results.HTML || {};
Microsoft.Search.Results.HTML = {

    render_tab: function (result) {
        $("#html-viewer").empty();
        var containerHTML = '';

        if (Microsoft.Search.SupportHTMLPreview(result)) {

            containerHTML += '<iframe id="htmlViewerIFrame" title="HTML Preview of a document" class="w-100 h-100">';
            containerHTML += '</iframe>';
            $("#html-viewer").html(containerHTML);

            $.postAPIText('/api/document/gethtml',
                {
                    path: result.metadata_storage_path
                },
                function (data) {
                    if (data && data.length > 0) {
                        try {
                            var parser = new DOMParser();
                            var htmlDoc = parser.parseFromString(data, 'text/html');
                            // $('htmlViewerIFrame').contents().find('body').html(htmlDoc.getElementsByTagName('body').html());
                            $('#htmlViewerIFrame').contents().find('head').html('<link rel="stylesheet" href="/css/iframe.css"/>');
                            $('#htmlViewerIFrame').contents().find('body').html(htmlDoc.body.getInnerHTML());
                        }
                        catch (err) {
                            console.log(err);
                        }
                    }
                });
        }
        return containerHTML;
    }
}
