// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// SIBLINGS 

Microsoft.Search = Microsoft.Search || {};
Microsoft.Search.Results = Microsoft.Search.Results || {};
Microsoft.Search.Results.Siblings = Microsoft.Search.Results.Siblings || {};
Microsoft.Search.Results.Siblings = {
    
    render_tab: function (result, tabular) {
        var containerHTML = '';
        var pathExtension = result.metadata_storage_path.toLowerCase().split('.').pop();

        if (result.document.embedded) {
            if (Microsoft.Utils.IsImageExtension(pathExtension)) {
                containerHTML = this.render_siblings_results(result, tabular);
            }
        }
        return containerHTML;
    },
    render_siblings_results: function (result, tabular) {

        var containerHTML = '<div class="progress"><div class="progress-bar progress-bar-striped bg-danger" role = "progressbar" style = "width: 100%" aria - valuenow="100" aria - valuemin="0" aria - valuemax="100"></div></div>';

        if (result.parent) {
            $.postAPIJSON('/api/document/getsiblings',
            {
                document_id: result.parent.id,
                incomingFilter: "parent/id eq '" + result.parent.id + "' ",
                parameters: {
                    RowCount: 50,
                    inOrderBy: ["page_number asc"]
                }
            },
            function (data) {
                Microsoft.Search.Results.Siblings.append_siblings(data.results);
            });
        }

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

