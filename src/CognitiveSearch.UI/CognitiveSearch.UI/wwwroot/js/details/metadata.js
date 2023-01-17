// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// METADATA

Microsoft.Search = Microsoft.Search || {};
Microsoft.Search.Results = Microsoft.Search.Results || {};
Microsoft.Search.Results.Metadata = Microsoft.Search.Results.Metadata || {};
Microsoft.Search.Results.Metadata = {
    render_tab: function (result, tabular, targetid="#metadata-viewer") {

        var metadataContainerHTML = $(targetid).html();

        metadataContainerHTML = '';
        metadataContainerHTML += '<table class="table table-hover table-striped"><thead><tr><th data-field="key" class="key">Key</th><th data-field="value">Value</th></tr></thead>';
        metadataContainerHTML += '<tbody>';

        var excluding_fields = ["content", "merged_content", "translated_text", "tables", "kvs", "paragraphs", "image_data", "thumbnail_small", "thumbnail_medium", "tokens_html"];

        var keys = Object.keys(result).sort();

        for (var k = 0; k < keys.length; k++) {
            var key = keys[k];
            if (result.hasOwnProperty(key)) {
                if (!excluding_fields.includes(key)) {
                    if (result[key] !== null) {
                        var value = result[key];
                        if (value.length && value.length > 0) {
                            if (key.indexOf("parentfilename") > -1 || key.indexOf("parenturl") > -1) {
                                value = Base64.decode(value);
                            }
                            metadataContainerHTML += '<tr><td class="key">' + key + '</td><td class="wrapword text-break">' + value + '</td></tr>';
                        }
                        else {
                            if (Number.isInteger(value) || (typeof value === 'boolean')) {
                                metadataContainerHTML += '<tr><td class="key">' + key + '</td><td class="wrapword text-break">' + value + '</td></tr>';
                            }
                            else {
                                if (Object.keys(value).length > 0) {
                                    metadataContainerHTML += '<tr><td class="key text-start">' + key + '</td><td class="wrapword text-break"></td></tr>';
                                    for (var subkey in value) {
                                        if (!excluding_fields.includes(subkey)) {
                                            if (value[subkey]) {
                                                var displayValue = value[subkey];
                                                if (subkey.indexOf("parentfilename") > -1 || subkey.indexOf("parenturl") > -1) {
                                                    displayValue = Base64.decode(displayValue);
                                                }
                                                metadataContainerHTML += '<tr><td class="key ps-5">' + subkey + '</td><td class="wrapword text-break">' + displayValue + '</td></tr>';
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };

        metadataContainerHTML += '</tbody>';
        metadataContainerHTML += '</table></div><br/>';

        if (! result.document.converted) {
            $.postAPIJSON('/api/document/getmetadata',
                {
                    path: result.metadata_storage_path
                },
                function (data) {
                    if (data && data.length > 0) {
                        try {
                            result = JSON.parse(data)
                            var extraMetadataContainerHTML = $(targetid).html();

                            extraMetadataContainerHTML += '<h4 id="available_metadata">File Metadata</h4><div style="overflow-x:auto;">';
                            extraMetadataContainerHTML += '<table class="table table-hover table-striped">';
                            extraMetadataContainerHTML += '<thead><tr><th data-field="key" class="key">Key</th><th data-field="key" class="key">Normalized</th><th data-field="value">Value</th></tr></thead>';
                            extraMetadataContainerHTML += '<tbody>';

                            var keys = Object.keys(result).sort();

                            for (var k = 0; k < keys.length; k++) {
                                var key = keys[k];
                                if (result.hasOwnProperty(key)) {
                                    if (!key.startsWith("X-TIKA")) {
                                        extraMetadataContainerHTML += '<tr><td class="key">' + key + '</td><td class="key">' + Microsoft.Utils.replaceAll(Microsoft.Utils.replaceAll(key," ","-"),":","-") + '</td><td class="wrapword text-break">' + result[key] + '</td></tr>';                                    
                                    }
                                }
                            };

                            extraMetadataContainerHTML += '</tbody>';
                            extraMetadataContainerHTML += '</table></div><br/>';

                            $(targetid).html(extraMetadataContainerHTML);
                        }
                        catch (exception) {
                        }
                    }
                });
        }

        return metadataContainerHTML;
    }
}
