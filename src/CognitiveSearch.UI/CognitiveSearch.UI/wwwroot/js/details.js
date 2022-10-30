// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// Details aka Document Preview configuration

Microsoft.Results = Microsoft.Results || {};
Microsoft.Results.Details = Microsoft.Results.Details || {};
Microsoft.Results.Details = {
    tabulars: [],
    
    init: function () {
        return new Promise((resolve, reject) => {
            $.ajax({
                type: 'GET',
                url: '/config/details.json',
                dataType: 'json',
                success: function (data) {
                    Microsoft.Results.Details.tabulars = data;
                    resolve()
                },
                error: function (error) {
                    reject(error)
                },
            })
        });
    },

    render_document_details: function (result, idx) {
        // Disable navigation if idx = -1
        if (idx === -1 || Microsoft.Search.TotalCount === 1) {
            $('#prev-control').hide();
            $('#next-control').hide();
        }
        else {
            $('#prev-control').show();
            $('#next-control').show();
        }

        $('#details-pivot-content').empty();

        $('#result-id').val(result.index_key);
        $('#result-idx').val(idx);

        this.init_tab_panels();
        this.init_tab_links();

        // for each tab ask to render the content
        this.render_tab_content(result);

        this.adjust_active_tab();

        // Modal Header
        $('#document-quick-actions').html(Microsoft.Search.Actions.renderActions(result, true));
        $('#document-details').html(Microsoft.Results.Details.GetDocumentHeader(result, idx));

        $('#details-modal').modal('show');

        if (Microsoft.View.currentQuery) {
            Microsoft.Search.Results.Transcript.SearchTranscript(Microsoft.View.currentQuery);
        }

        //Log Click Events
        Microsoft.Telemetry.LogClickAnalytics(result.metadata_storage_name, 0);

    },

    render_single_tab_details: function (header, tab_content) {
        $('#single-tab-details-label').html(header);
        $('#single-tab-details-body').html(tab_content);
        $('#single-tab-details').modal('show');
    },

    adjust_active_tab: function () {
        for (var i = 0; i < this.tabulars.length; i++) {
            var tabular = this.tabulars[i];
            if (tabular.enable) {
                if (tabular.content_length > 0) {
                    var triggerEl = document.querySelector('#details-pivot-links a[href="#' + tabular.id + '-pivot"]');
                    var firstTab = new bootstrap.Tab(triggerEl);
                    firstTab.show() // Select tab by name
                    break;
                }
            }
        }
    },

    hide_tab: function (name) {
        $('#' + name + '-pivot-link').hide()
    },

    adjust_tab_icon: function (result, tabular) {
        var fileName = "Document";
        var pathExtension = result.metadata_storage_path.toLowerCase().split('.').pop();
        var override_icon = tabular.fonticon;

        // Embedded objects
        if (result.document_embedded) {
            if (Microsoft.Utils.IsImageExtension(pathExtension)) {
                fileName = Microsoft.Utils.GetImageFileTitle(result);
                override_icon = 'bi bi-file-image';
            }
            else {
                override_icon = 'bi bi-paperclip text-warning';
            }
        }
        else {
            if (Microsoft.Utils.IsImageExtension(pathExtension)) {
                override_icon = 'bi bi-file-image';
            }
        }
        
        // Videos 
        if (Microsoft.Utils.IsVideoExtension(pathExtension)) {
            fileName = "Video"
            override_icon = 'bi bi-camera-video';
        }

        $('#details-' + tabular.id + '-icon').removeClass(tabular.fonticon).addClass(override_icon).text(' ' + fileName);
    },

    init_tab_panels: function () {
        var pivotsContentHtml = '';

        for (var i = 0; i < this.tabulars.length; i++) {
            var tabular = this.tabulars[i];
            pivotsContentHtml += this.create_tab_pane(tabular);
            tabular.content_length = 0;
        }
        $('#details-pivot-content').html(pivotsContentHtml);
    },

    init_tab_links: function () {
        var pivotLinksHTML = '';

        for (var i = 0; i < this.tabulars.length; i++) {
            var tabular = this.tabulars[i];
            if (tabular.enable) {
                pivotLinksHTML += this.create_tab_link(tabular.id, tabular.localization["en"].name, tabular.fonticon, tabular.localization["en"].title);
            }
        }
        $('#details-pivot-links').html(pivotLinksHTML);
    },

    render_tab_content: function (result) {
        for (var i = 0; i < this.tabulars.length; i++) {
            var tabular = this.tabulars[i];
            if (tabular.enable) {
                if (tabular.renderingMethod) {
                    var content = Microsoft.Utils.executeFunctionByName(tabular.renderingMethod, window, result, tabular);
                    if (content !== undefined)
                    {
                        if (content.length > 0) {
                            $('#' + tabular.id + '-viewer').html(content);
                            if (tabular.adaptiveIcon) {
                                this.adjust_tab_icon(result, tabular);
                            }
                        }
                        else {
                            this.hide_tab(tabular.id);
                        }
                        tabular.content_length = content.length;
                    }
                    else {
                        // We can assume the tab is enabled and the renderer did the html append itself.
                        // this.hide_tab(tabular.id);
                        tabular.content_length = 1;
                    }
                }
            }
        }
    },

    create_tab_pane: function (tabular) {

        var tab_pane = '';

        if (tabular.active) {
            tab_pane += '<div id="' + tabular.id + '-pivot" class="tab-pane active" role="tabpanel" aria-labelledby="' + tabular.id + '-pivot-link"> ';
        }
        else {
            tab_pane += '<div id="' + tabular.id + '-pivot" class="tab-pane" role="tabpanel" aria-labelledby="' + tabular.id + '-pivot-link"> ';
        }

        if (tabular.viewerClass) {
            tab_pane += '<div id="' + tabular.id + '-viewer" class="details-default-viewer ' + tabular.viewerClass + '"></div>';
        }
        else {
            tab_pane += '<div id="' + tabular.id + '-viewer" class="details-default-viewer"></div>';
        }
        tab_pane += '</div>';
        return tab_pane;
    },

    create_tab_link: function (stem, title, icon = null, description = "") {
        var result = '';

        result += '<li class="nav-item" role="presentation">';
        result += '<a class="nav-link" ';
        result += '  title="' + description + '"  id="' + stem + '-pivot-link" data-bs-toggle="tab" href="#' + stem + '-pivot" role="tab" aria-controls="' + stem + '-pivot" aria-selected="false">';
        if (icon) {
            result += '<span id="details-' + stem + '-icon" class="' + icon + '">';
            result += ' ' + title;
            result += '</span>';
        }
        result += ' </a>';
        result += '</li>';

        return result;
    },

    ShowDocumentByIndex: function(idx) {
        if (idx < Microsoft.Search.results_keys_index.length) {
            Microsoft.Results.Details.ShowDocument(Microsoft.Search.results_keys_index[idx], idx);
        }
        else {
            // Ask for next page of results
        }
    },
    
    ShowDocument: function(index_key, idx) {
        $.postAPIJSON('/api/document/getbyindexkey',
            {
                index_key: index_key
            },
            function (data) {
                if (data.result) {
                    Microsoft.Results.Details.render_document_details(data.result, idx);
                }
            });
    },

    ShowDocumentAction: function(parameters) {
        parameters = JSON.parse(Base64.decode(parameters));
        var bkobj = parameters[0]; 
        $.postAPIJSON('/api/document/getbyindexkey',
            {
                index_key: bkobj.index_key
            },
            function (data) {
                if (data.result) {
                    Microsoft.Results.Details.render_document_details(data.result, bkobj.idx);
                }
            });
    },
    
    ShowDocumentById: function(id, idx = -1) {
        $.postAPIJSON('/api/document/getbyid',
            {
                document_id: id
            },
            function (data) {
                if (data.result) {
                    Microsoft.Results.Details.render_document_details(data.result, idx);
                }
            });
    },
    
    GetDocumentHeader: function(docresult, idx) {
    
        var headerContainerHTML = '';
    
        var pathExtension = docresult.metadata_storage_path.toLowerCase().split('.').pop();
        var iconPath = Microsoft.Utils.GetIconPathFromExtension(pathExtension);
        if (docresult.document_embedded) {
        } else {
            // If embedded images tab is not relevant then skip
            if (!Microsoft.Utils.IsImageExtension(pathExtension)) {
                headerContainerHTML += '<img class="ms-1 me-2" style="width: 32px;height: 32px;margin-left: 15px;" title="'+docresult.title+'" src="' + iconPath + '" />';
            }
        }
    
        headerContainerHTML += '<div class="document-header-title"> ';
        headerContainerHTML += Microsoft.Utils.GetDocumentTitle(docresult);
        headerContainerHTML += Microsoft.Utils.GetModificationLine(docresult);
        headerContainerHTML += '</div> ';
    
        return headerContainerHTML;
    },
    
    pMouseOver: function(ptag) {
        jQuery('.'+ptag).addClass("p-highlight-color");
    },
    
    pMouseOut: function(ptag) {
        jQuery('.' + ptag).removeClass("p-highlight-color");
    }
}

Microsoft.Results.Details.init();

$('#next-control').click(function () {
    var idx = parseInt($('#result-idx').val());

    if (idx < Microsoft.Search.TotalCount) {
        Microsoft.Results.Details.ShowDocumentByIndex(idx + 1);
    }
});

$('#prev-control').click(function () {
    var idx = parseInt($('#result-idx').val());

    if (idx > 0) {
        Microsoft.Results.Details.ShowDocumentByIndex(idx - 1);
    }
});

// export default Microsoft.Results;
