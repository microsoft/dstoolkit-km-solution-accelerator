// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

Microsoft.Search = Microsoft.Search || {};
Microsoft.Search.Results = Microsoft.Search.Results || {};
Microsoft.Search.Results.File = Microsoft.Search.Results.File || {};
Microsoft.Search.Results.File = {

    RenderSearchResultPreview: function (result) {
        var fileContainerHTML = '';
        fileContainerHTML += this.render_file_container(result.document.embedded, result.metadata_storage_path, result.image ? result.image.image_data : null, result.page_number, result.parent);
        return fileContainerHTML;
    },
    render_file_container: function (document_embedded, path, image_data, pagenumber, parent) {
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
                if (document.embedded && Microsoft.Utils.IsPDF(parent.filename)) {
                    fileContainerHTML += '<button type="button" class="btn btn-light" onclick="Microsoft.Search.Results.get_next_page(\'' + parent.id + '\',' + pagenumber + ');" >Next Page</button>'
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
    }
}