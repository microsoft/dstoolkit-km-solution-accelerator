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
        this.render_all_tabs(result);

        this.adjust_active_tab();

        // Modal Header
        $('#document-quick-actions').html(Microsoft.Search.Actions.renderActions(result, true, null, false));
        
        $('#document-details').html(Microsoft.Results.Details.GetDocumentHeader(result, idx));

        $('#details-modal').modal('show');

        if (Microsoft.View.currentQuery) {
            Microsoft.Search.Results.Transcript.SearchTranscript(Microsoft.View.currentQuery);
        }

        Microsoft.Search.ProcessCoverImage(); 

        //Log Click Events
        Microsoft.Telemetry.LogClickAnalytics(result.metadata_storage_name, 0);

    },


    render_quick_details: function (index_key, details_key) {

        $('#quickview-content').empty();

        var tabular = Microsoft.Results.Details.get_tab_by_id(details_key);

        if (! this.select_tab(tabular.id)) 
        {
            $.postAPIJSON('/api/document/getbyindexkey',
            {
                index_key: index_key
            },
            function (data) {
                if (data.result) {
                    var result = data.result;
                    if (tabular) {
                        $('#quickview-title').html("<h2>"+tabular.localization.en.title+"</h2>");
                        var tabular_viewer_tag=('#' + tabular.id + '-quick-viewer');
                        $('#quickview-content').html('<div id="'+(tabular.id + '-quick-viewer')+'"></div>');
                        Microsoft.Results.Details.render_tab_content(result, tabular,tabular_viewer_tag);
                
                        // Modal Header
                        $('#quickview-modal').modal('show');
                
                        Microsoft.Search.ProcessCoverImage();    
                    }    
                }
            });    
        }
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

    select_tab: function (tabularid) {
        var triggerEl = $('#details-pivot-links a[href="#' + tabularid + '-pivot"]');
        if (triggerEl.length) {
            var firstTab = new bootstrap.Tab(triggerEl);
            firstTab.show();
            return true;    
        }
        return false;
    },

    hide_tab: function (name) {
        $('#' + name + '-pivot-link').hide();
    },

    adjust_tab_icon: function (result, tabular) {
        var fileName = "Document";
        var pathExtension = result.metadata_storage_path.toLowerCase().split('.').pop();
        var override_icon = tabular.fonticon;

        // Embedded objects
        if (result.document.embedded) {
            if (Microsoft.Utils.IsImageExtension(pathExtension)) {
                fileName = Microsoft.Utils.GetImageFileTitle(result);
                override_icon = 'bi bi-file-image';
            }
            else {
                override_icon = 'bi bi-paperclip';
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

    get_tab_by_id: function(id) {
        for (var i = 0; i < this.tabulars.length; i++) {
            var tabular = this.tabulars[i];
            if (tabular.id === id) {
                return tabular
            }
        }
    },

    render_all_tabs: function (result) {
        for (var i = 0; i < this.tabulars.length; i++) {
            var tabular = this.tabulars[i];
            this.render_tab_content(result, tabular,('#' + tabular.id + '-viewer'));
        }
    },
    
    render_tab_content: function (result , tabular, targetid) {
        if (tabular.enable) {
            if (tabular.renderingMethod) {                
                new Promise((resolve, reject) => {
                    var content = Microsoft.Utils.executeFunctionByName(tabular.renderingMethod, window, result, tabular, targetid);
                    if (content !== undefined)
                    {
                        if (content.length > 0) {
                            var HTMLContent = '';
                            // Warning 
                            if (result.document.translated) {
                                HTMLContent += '<div class="border border-warning rounded bg-warning text-dark p-1">This content is the result of an automatic Document Translation.</div>';
                            }
                            HTMLContent += content;
                            $(targetid).html(HTMLContent);
                            // $('#' + tabular.id + '-viewer').html(HTMLContent);
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
                });
            }
        }
    },

    create_tab_pane: function (tabular) {

        var tab_pane = '';

        tab_pane += '<div id="' + tabular.id + '-pivot" class="tab-pane" role="tabpanel" aria-labelledby="' + tabular.id + '-pivot-link"> ';

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

    // ShowDocumentAction: function(parameters) {
    //     parameters = JSON.parse(Base64.decode(parameters));
    //     var bkobj = parameters[0]; 
    //     $.postAPIJSON('/api/document/getbyindexkey',
    //         {
    //             index_key: bkobj.index_key
    //         },
    //         function (data) {
    //             if (data.result) {
    //                 Microsoft.Results.Details.render_document_details(data.result, bkobj.idx);
    //             }
    //         });
    // },
    
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
    
    ShowRelatedDocumentById: function(id, idx = -1) {

        $.postAPIJSON('/api/document/getsiblings',
        {
            document_id: id,
            incomingFilter: "(parent/id eq '" + id + "') and (document/embedded eq false)",
            parameters: {
                RowCount: 1
            }
        },
        function (data) {
            if (data.results && data.count == 1) {
                Microsoft.Results.Details.render_document_details(data.results[0], idx);
            }
        });
    },
    
    GetDocumentHeader: function(docresult, idx) {
    
        var headerContainerHTML = '';
    
        var pathExtension = docresult.metadata_storage_path.toLowerCase().split('.').pop();
        
        var iconPath = Microsoft.Utils.GetIconPathFromExtension(pathExtension);

        if (Microsoft.Utils.IsImageExtension(pathExtension)) {
            var alttitle = Microsoft.Utils.GetImageFileTitle(docresult);
            if (docresult.document.embedded && docresult.image) {
                headerContainerHTML += '<img alt="' + alttitle + '" class="image-result img-thumbnail" src="data:image/png;base64, ' + docresult.image.thumbnail_medium + '" title="' + Base64.decode(docresult.parent.filename) + '" />';
            }
            else {
                headerContainerHTML += '<img alt="' + alttitle + '" class="image-result img-thumbnail" src="data:image/png;base64, ' + docresult.image.thumbnail_medium + '" title="' + docresult.metadata_storage_name + '" />';
            }
        }
        else { 
            headerContainerHTML += '<img class="ms-1 me-2" style="width: 32px;height: 32px;margin-left: 15px;" title="'+docresult.title+'" src="' + iconPath + '" />';            
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

// Clean the quickview modal content when hidden
$('#details-modal').on('hidden.bs.modal', function () {
    $('#quickview-content').empty();
});

// Clean the modal content when hidden
$('#details-modal').on('hidden.bs.modal', function () {
    $('#details-pivot-links').empty();
    $('#details-pivot-content').empty();
});

// export default Microsoft.Results;
