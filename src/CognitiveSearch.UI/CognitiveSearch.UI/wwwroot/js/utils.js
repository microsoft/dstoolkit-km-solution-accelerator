// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

Microsoft.Utils = Microsoft.Utils || {};
Microsoft.Utils = {

    toggleCollapsable: function (id) {
        $("#collapsable-" + id).collapse('toggle');
        $("#result-" + id).collapse('toggle');
    },

    toggleDiv: function (tag) {
        var displayStatus = document.getElementById(tag);
        if (displayStatus.style.display == 'none') {
            //If the div is hidden, show it
            displayStatus.style.display = 'block';
        } else {
            //If the div is shown, hide it
            displayStatus.style.display = 'none';
        }
    },

    jqid: function (id) {
        return (!id) ? null : id.replace(/(\/|:|\.|\\|\[|\+|\]|,|=|@)/g, '\\$1');
    },

    replaceAll: function (str, find, replace) {
        return str.replace(new RegExp(find, 'g'), replace);
    },

    htmlEncode: function (value) {
        return $('<div/>').text(value).html();
    },

    htmlDecode: function (value) {
        return $('<div/>').html(value).text();
    },

    // Global Search methods
    OpenView: function (path, query) {
        this.OpenSearchPagewithFacets(path, query)
    },

    OpenSearchPagewithFacets: function (path, query) {

        $("#navigation-form").attr("action", path);

        if (query === null || query === undefined) {
            query = $("#q").val();
        }

        $("#navigation-q").val(query);
        
        if (Microsoft.Facets.selectedFacets && Microsoft.Facets.selectedFacets.length > 0) {
            var searchFacetsAsString = JSON.stringify(Microsoft.Facets.selectedFacets)
            $("#navigation-facets").val(Base64.encode(searchFacetsAsString));
        }

        $("#navigation-form").submit();

    },

    executeFunctionByName: function (functionName, context /*, args */) {
        var args = Array.prototype.slice.call(arguments, 2);
        var namespaces = functionName.split(".");
        if (namespaces.length > 0) {
            var func = namespaces.pop();
            for (var i = 0; i < namespaces.length; i++) {
                context = context[namespaces[i]];
            }
            return context[func].apply(context, args);
        }
        else {
            context[functionName](args);
        }
    },

    // https://docs.microsoft.com/en-us/azure/app-service/overview-authentication-authorization
    // Provider	Header names
    // Azure Active Directory	X-MS-TOKEN-AAD-ID-TOKEN
    // X-MS-TOKEN-AAD-ACCESS-TOKEN
    // X-MS-TOKEN-AAD-EXPIRES-ON
    // X-MS-TOKEN-AAD-REFRESH-TOKEN

    // Security related functions
    // refreshTokens:function() {
    //     let refreshUrl = "/.auth/refresh";
    //     $.ajax(refreshUrl).done(function () {
    //         console.log("Token refresh completed successfully.");
    //     }).fail(function () {
    //         console.log("Token refresh failed. See application logs for details.");
    //     });
    // },


    GetDisplayTitle: function (name) {
        // Split the string at all space characters
        return name.split(' ')
            // get rid of any extra spaces using trim
            .map(a => a.trim())
            // Convert first char to upper case for each word
            .map(a => a[0].toUpperCase() + a.substring(1))
            // Join all the strings back together
            .join(" ")
        //return name.replace(/([A-Z])/g, ' $1').replace(/^./, function (str) { return str.toUpperCase(); });
    },

    GetFacetDisplayTitle: function (name) {
        name = name.replace("Ml ", "").replace("image_", "");
        name = name.replace("_", " ");
        return this.GetDisplayTitle(name)
    },

    images_extensions: ["jpg", "jpeg", "gif", "png", "bmp", "tiff", "tif", "emf", "wmf"],

    IsImageExtension: function (pathExtension) {
        return this.images_extensions.includes(pathExtension)
    },

    videos_extensions: ["mp4", "wmv"],

    IsVideoExtension: function (pathExtension) {
        return this.videos_extensions.includes(pathExtension)
    },

    IsTIFFImage: function (pathExtension) {
        if (pathExtension === "tif" || pathExtension === "tiff") {
            return true;
        }
        else {
            return false;
        }
    },

    IsPDF: function (pathLower) {
        var pathExtension = pathLower.split('.').pop();
        if (pathExtension === "pdf") {
            return true;
        }
        else {
            return false;
        }
    },

    GetImagePageorSlideNumber: function (name) {
        // image-00000-00001.extension
        var toks = name.split('-');
        if (toks.length > 2) {
            return parseInt(toks[1]);
        }
        else {
            return "0";
        }
    },

    GetImageFileTitle: function (result) {
        var filename;
        var pathExtension = Base64.decode(result.image_parentfilename).toLowerCase().split('.').pop();

        if (pathExtension === "ppt" || pathExtension === "pptx") {
            filename = "Slide " + this.GetImagePageorSlideNumber(result.metadata_storage_name);
        }
        else {
            filename = "Page " + this.GetImagePageorSlideNumber(result.metadata_storage_name);
        }
        return filename;
    },


    GetImageParentDocumentExtension: function (result) {
        var pathLower = this.GetParentPathFromImage(result).toLowerCase();
        return pathLower.split('.').pop();
    },

    GetParentPathFromImage: function (result) {
        return Base64.decode(result.image_parenturl)
    },

    MaxItemsPerPage: 500,
    MaxRowLimit: 500,
    MaxItemsExport: 1000,
    defaultColors: [
        '#4dc9f6',
        '#f67019',
        '#f53794',
        '#537bc4',
        '#acc236',
        '#166a8f',
        '#00a950',
        '#58595b',
        '#8549ba'
    ],
    IconMappings: {
        "PowerPoint": "icpptx.svg",
        "odp": "icpptx.svg",
        "ppt": "icpptx.svg",
        "pptx": "icpptx.svg",
        "pptm": "icpptx.svg",
        "potm": "icpptx.svg",
        "potx": "icpptx.svg",
        "ppam": "icpptx.svg",
        "ppsm": "icpptx.svg",
        "ppsx": "icpptx.svg",

        "Word": "icdocx.svg",
        "docx": "icdocx.svg",
        "doc": "icdocx.svg",
        "docm": "icdocx.svg",
        "dot": "icdocx.svg",
        "nws": "icdocx.svg",
        "dotx": "icdocx.svg",

        "Visio": "icvsdx.svg",
        "vsdx": "icvsdx.svg",
        "vsd": "icvsdx.svg",
        "vsx": "icvsdx.svg",

        "Excel": "icxlsx.svg",
        "xlsx": "icxlsx.svg",
        "xls": "icxlsx.svg",
        "xlsb": "icxlsx.svg",
        "xlsm": "icxlsx.svg",
        "xltm": "icxlsx.svg",
        "xltx": "icxlsx.svg",
        "xlam": "icxlsx.svg",
        "odc": "icxlsx.svg",
        "ods": "icxlsx.svg",

        "OneNote": "icone.svg",
        "one": "icone.svg",

        "PDF": "icpdf.svg",
        "pdf": "icpdf.svg",

        "Image": "photo.svg",
        "bmp": "photo.svg",
        "jpg": "photo.svg",
        "jpeg": "photo.svg",
        "png": "photo.svg",
        "tiff": "photo.svg",
        "gif": "photo.svg",
        "rle": "photo.svg",
        "wmf": "photo.svg",
        "dib": "photo.svg",
        "ico": "photo.svg",
        "iwpd": "photo.svg",
        "odg": "photo.svg",

        "Video": "video.svg",
        "wmv": "video.svg",
        "mp4": "video.svg",

        "Email": "email.svg",
        "msg": "email.svg",
        "eml": "email.svg",
        "exch": "email.svg",

        "SharePoint Site": "site.svg",
        "Team Site": "site.svg",

        "Web page": "html.svg",
        "aspx": "html.svg",
        "html": "html.svg",
        "mhtml": "html.svg",
        "htm": "html.svg",
        "txt": "html.svg",

        "csv": "feed.png",
        "feed": "feed.png",

        "Zip": "iczip.svg",
        "zip": "iczip.svg",
        "rar": "iczip.svg",

        "newtab": "newtab.png",
        "News Article": "html.svg",
        "Folder": "folder.svg",
        "Kaltura": "Kaltura.svg",
        "Yammer": "Yammer.svg",
        "Stream": "Stream.svg",
        "All": "genericfile.svg",

        "bing": "newBing_ic_32.svg"
    },
    FindInitials: function (str) {
        var splitStr = str.toLowerCase().split(' ');
        var maxCaps = splitStr.length > 3 ? 3 : splitStr.length;
        for (var i = 0; i < splitStr.length; i++) {
            splitStr[i] = i < maxCaps ? splitStr[i].charAt(0).toUpperCase() : "";
        }
        // Directly return the joined string
        return (splitStr.join('')).trim();
    },
    PickColor: function () {
        var random_color = Microsoft.Colors[Math.floor(Math.random() * Microsoft.Colors.length)];
        return random_color;
    },
    EscapeString: function (unescapedString) {
        var escapedString = unescapedString;
        if (unescapedString != null) {
            escapedString = unescapedString.replace(/\\/g, "\\\\");
            escapedString = escapedString.replace(/\'/g, "\\\'");
            escapedString = escapedString.replace(/\r?\n|\r/g, "");
            escapedString = escapedString.replace(/,/g, "");
        }
        return escapedString;
    },
    MapLabelToIconFilename: function (label) {
        var IconFileName = "genericfile.svg";

        if (label in this.IconMappings) {
            IconFileName = this.IconMappings[label];
        }
        return ("/icons/" + IconFileName);
    },
    OpenInNewTab: function (url) {
        window.open(url, '_blank');
    },
    GetUrlVars: function (url) {
        var vars = {};
        var parts = url.replace(/[?&#]+([^=&#]+)=([^&^#]*)/gi,
            function (m, key, value) {
                vars[key] = value;
            });
        return vars;
    },
    GetIconPathFromExtension: function (pathExtension) {
        return Microsoft.Utils.MapLabelToIconFilename(pathExtension);
    },

    GetDocumentTitle: function (docresult, only_parent = false) {

        var startTag = '<a target="_blank" href="' + Microsoft.Search.GetSASTokenFromPath(docresult.metadata_storage_path) + '">';
        if (docresult.restricted) {
            startTag += '<h5 class="document-title bi bi-lock"> ';
        }
        else {
            startTag += '<h5 class="document-title"> ';
        }
        var endTag = '</h5></a>';

        if (docresult.document_embedded) {

            var page_slide = this.GetImagePageorSlideNumber(docresult.metadata_storage_name);

            var parentExtension = this.GetImageParentDocumentExtension(docresult);

            if (only_parent) {
                return startTag + Base64.decode(docresult.image_parentfilename) + endTag;
            }
            else {
                if (parentExtension === "ppt" || parentExtension === "pptx") {
                    return startTag + 'Slide ' + page_slide + ' - ' + Base64.decode(docresult.image_parentfilename) + endTag;
                }
                else {
                    return startTag + 'Page ' + page_slide + ' - ' + Base64.decode(docresult.image_parentfilename) + endTag;
                }
            }
        }

        if (docresult.title) {
            return startTag + docresult.title + endTag
        }
        else {
            if (docresult.metadata_storage_name) {
                return startTag + docresult.metadata_storage_name + endTag
            }
        }
        return "Untitled";
    },

    //TODO GetModificationLine(docresult) refactor this in a better way
    GetModificationLine: function (docresult) {

        var resultHtml = '<form class="row g-1 bg-light ms-2">';

        var mdate = docresult.last_modified ? docresult.last_modified : docresult.source_last_modified;

        if (mdate) {
            var d = new Date(mdate);
            resultHtml += '<div class="col">';
            resultHtml += '<label class="modification-line me-2" for="floatingInputValue">Last Modified</label>';
            resultHtml += '<input type="text" readonly class="p-0 border-0 bg-light" id="floatingInputValue" title="' + d.toLocaleString() + '" value="' + d.toLocaleString() + '">';
            resultHtml += '</div>';
        }

        if (docresult.authors) {
            if (docresult.authors.length > 0) {
                var author = docresult.authors[0];

                resultHtml += '<div class="col">';
                resultHtml += '<label class="modification-line me-2" for="floatingInputValue">Author(s)</label>';

                if (author.length > 60) {
                    // resultHtml += ' Author(s) ' + author.substring(0, 50) + "..."
                    resultHtml += '<input type="text" readonly class="p-0 border-0 bg-light" id="floatingInputValue" title="' + author + '" value="' + author.substring(0, 50) + '...">';
                }
                else {
                    // resultHtml += ' Author(s) ' + author
                    resultHtml += '<input type="text" readonly class="p-0 border-0 bg-light" id="floatingInputValue" title="' + author + '" value="' + author + '...">';
                }
                resultHtml += '</div>';
            }
        }

        resultHtml += '</form>';
        return resultHtml;
    },

    GetModificationTime: function (docresult, style = 'text-white') {

        var resultHtml = '';

        if (docresult.last_modified) {
            var d = new Date(docresult.last_modified);
            resultHtml += d.toLocaleString()
        }

        if (resultHtml.length > 0) {
            return '<h6 class="modification-time ' + style + '"> ' + resultHtml + '</h6>';
        }
        else {
            return resultHtml;
        }

    },

    GetLocationsFromResult: function (result) {
        var locs = [];

        if (result.locations) {
            locs = locs.concat(result.locations);
        }
        if (result.countries) {
            locs = locs.concat(result.countries);
        }
        if (result.capitals) {
            locs = locs.concat(result.capitals);
        }
        if (result.cities) {
            locs = locs.concat(result.cities);
        }
        if (result.landmarks) {
            locs = locs.concat(result.landmarks);
        }

        return window.btoa(locs.join('|'));
    },

    IsOfficeDocument: function (pathExtension) {
        if (pathExtension === "doc" || pathExtension === "ppt" || pathExtension === "xls" || pathExtension === "docx" || pathExtension === "pptx" || pathExtension === "pptm" || pathExtension === "xlsx" || pathExtension === "xlsm") {
            return true;
        }
        else {
            return false;
        }
    },
    IsPowerPointDocument: function (pathExtension) {
        if (pathExtension === "ppt" || pathExtension === "pptx" || pathExtension === "pptm") {
            return true;
        }
        else {
            return false;
        }
    },

    // Scroll Utils 
    // When the user clicks on the button, scroll to the top of the document
    ScrollTopFunction: function () {
        document.body.scrollTop = 0; // For Safari
        document.documentElement.scrollTop = 0; // For Chrome, Firefox, IE and Opera
    }

}

// export default Microsoft.Utils;