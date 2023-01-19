// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// HTML

Microsoft.Search = Microsoft.Search || {};
Microsoft.Search.Results = Microsoft.Search.Results || {};
Microsoft.Search.Results.HTML = Microsoft.Search.Results.HTML || {};
Microsoft.Search.Results.HTML = {

    render_tab: function (result, tabular, targetid="#html-viewer") {
        $(targetid).empty();
        var containerHTML = '';

        if (Microsoft.Search.SupportHTMLPreview(result)) {

            containerHTML+='<div class="col-md-10 h-100">'
            containerHTML += '<iframe id="htmlViewerIFrame" title="HTML Preview of a document" class="w-100 h-100">';
            containerHTML += '</iframe>';
            containerHTML+='</div>'
            containerHTML+='<div class="col-md-2">';
            containerHTML+=Microsoft.Search.Results.Metadata.render_metadata_side_pane(result);
            containerHTML+='</div>';

            $(targetid).html(containerHTML);

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
                            $('#htmlViewerIFrame').contents().find('head').append('<link href="https://cdn.jsdelivr.net/npm/bootstrap@5.2.3/dist/css/bootstrap.min.css" rel="stylesheet" integrity="sha384-rbsA2VBKQhggwzxH7pPCaAqO46MgnOM80zW1RWuH61DGLwZJEdK2Kadq2F9CUG65" crossorigin="anonymous"/>');
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
