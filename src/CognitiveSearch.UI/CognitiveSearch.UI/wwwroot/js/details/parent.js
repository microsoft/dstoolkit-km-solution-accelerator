// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// PARENT (Source) document

Microsoft.Search = Microsoft.Search || {};
Microsoft.Search.Results = Microsoft.Search.Results || {};
Microsoft.Search.Results.Parent = Microsoft.Search.Results.Parent || {};
Microsoft.Search.Results.Parent = {
    render_tab_with_fallback: function (result, tabular) {
        return this.render_tab(result, tabular, true);
    },
    render_tab: function (result, tabular, fallback = false) {
        // Embedded Images Tab content
        var embeddedContainerHTML = '';

        if (result.document_embedded) {
            embeddedContainerHTML = Microsoft.Search.Results.File.render_file_container(result.document_embedded, Microsoft.Utils.GetParentPathFromImage(result), result.image_data, result.page_number);
        }
        else {
            if (fallback && Microsoft.Utils.IsPDF(result.metadata_storage_path)) {
                embeddedContainerHTML = Microsoft.Search.Results.File.RenderSearchResultPreview(result)
            }
        }

        return embeddedContainerHTML;
    }
}