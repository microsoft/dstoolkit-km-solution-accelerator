// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// TABLES
Microsoft.Tables = Microsoft.Tables || {};
Microsoft.Tables = {
    MAX_NUMBER_ITEMS_PER_PAGE: 10,
    view_tag_id: "#search-results-content",

    render_table_result: function (docresult, target_tag_id) {
        var resultsHtml = '';

        Microsoft.Search.results_keys_index.push(docresult.index_key);
        docresult.idx = Microsoft.Search.results_keys_index.length - 1;

        var name = docresult.document_filename;
        var path = docresult.metadata_storage_path;
        var pathLower = path.toLowerCase();
        var pathExtension = pathLower.split('.').pop();

        if (path !== null) {

            resultsHtml += '<div class="row results-cluster-row">';
            resultsHtml += '<div class="col-md-2">';

            var iconPath = Microsoft.Utils.GetIconPathFromExtension(pathExtension);

            resultsHtml += '<a href="javascript:void(0)" onclick="Microsoft.Results.Details.ShowDocumentById(\'' + docresult.document_id + '\');" >';

            if (Microsoft.Utils.images_extensions.includes(pathExtension)) {
                resultsHtml += '<img alt="' + name + '" class="image-result" src="data:image/png;base64, ' + docresult.image.thumbnail_medium + '" title="' + docresult.metadata_storage_name + '" />';
            }
            else {
                resultsHtml += Microsoft.Search.RenderCoverImage(docresult,name,iconPath);
                // resultsHtml += '   <img alt="' + name + '" class="image-result cover-image" src="data:image/png;base64,R0lGODlhAQABAAD/ACwAAAAAAQABAAACADs=" data-src="/api/document/getcoverimage?document_id=' + docresult.document_id + '" title="' + docresult.metadata_storage_name + '"onError="this.onerror=null;this.src=\'' + iconPath + '\';"/>';
            }

            resultsHtml += '</a>';
            resultsHtml += Microsoft.Search.Actions.renderActions(docresult, false, "unset");
            resultsHtml += '</div>';

            // Tables list 
            var table_zone_id = docresult.index_key + "-tables";
            resultsHtml += '<div id="' + table_zone_id + '" class="col-md-10" >'

            // Add the document name here + last modific
            resultsHtml += Microsoft.Utils.GetDocumentTitle(docresult);
            resultsHtml += Microsoft.Utils.GetModificationLine(docresult);

            $(target_tag_id).append(resultsHtml);

            this.render_document_datatables(docresult, '#'+table_zone_id);

            $(target_tag_id).append('</div>');
        }   
    },
    
    render_document_tables: function (docresult) {
        this.render_document_datatables(docresult);
    },
    
    render_document_datatables: function (docresult, target_tag_id='#tables-viewer') {

        if (docresult.tables && docresult.tables.length > 0) {

            var tables = JSON.parse(docresult.tables);

            // for each table 
            for (var i = 0; i < tables.length; i++) {

                var extraMetadataContainerHTML = '';

                var table_id = docresult.index_key + i;
                extraMetadataContainerHTML += '<div class="container mt-2 border-start border-2 border-warning" style="overflow-x:auto;">';

                extraMetadataContainerHTML += '<table id=' + table_id + ' class="table metadata-table table-hover table-striped">';
                extraMetadataContainerHTML += '<thead>';

                var table = tables[i];
                var table_matrix = Array.from(Array(table.row_count), () => new Array(table.column_count));

                var headers_row_indexes = [];

                // fill up a matrix of cells
                for (var j = 0; j < table.cells.length; j++) {
                    var cell = table.cells[j];
                    table_matrix[cell.rowIndex][cell.colIndex] = cell;
                    if (cell.is_header) {
                        // Row index is an header row
                        if (!headers_row_indexes.includes(cell.rowIndex)) {
                            headers_row_indexes.push(cell.rowIndex);
                        }
                    }
                }

                // Process the headers
                if (headers_row_indexes.length > 0) {
                    for (var k = 0; k < headers_row_indexes.length; k++) {
                        var row = table_matrix[headers_row_indexes[k]];
                        extraMetadataContainerHTML += '<tr>';
                        for (var j = 0; j < table.column_count; j++) {
                            cell = row[j];
                            if (cell && cell.text) {
                                extraMetadataContainerHTML += '<th>' + cell.text + '</th>';
                            }
                            else {
                                extraMetadataContainerHTML += '<th></th>';
                            }
                        }
                        extraMetadataContainerHTML += '</tr>';
                    }
    
                }
                else {
                    extraMetadataContainerHTML += '<tr>';
                    for (let index = 0; index < table.column_count; index++) {
                        extraMetadataContainerHTML += '<th></th>';                        
                    }
                    extraMetadataContainerHTML += '</tr>';
                }

                extraMetadataContainerHTML += '</thead>';
                extraMetadataContainerHTML += '<tbody>';

                // for each row (excl header row)
                for (var k = 0; k < table.row_count; k++) {
                    if (!headers_row_indexes.includes(k)) {
                        var row = table_matrix[k];
                        extraMetadataContainerHTML += '<tr>';
                        for (var j = 0; j < table.column_count; j++) {
                            cell = row[j];
                            if (cell && cell.text) {
                                extraMetadataContainerHTML += '<td>' + cell.text + '</td>';
                            }
                            else {
                                extraMetadataContainerHTML += '<td></td>';
                            }
                        }
                        extraMetadataContainerHTML += '</tr>';
                    }
                }

                extraMetadataContainerHTML += '</tbody>';

                extraMetadataContainerHTML += '</table>';

                extraMetadataContainerHTML += '</div><br/>';

                $(target_tag_id).append(extraMetadataContainerHTML);

                var table = $('#' + table_id).DataTable({
                    dom: 'Bfrtip',
                    paging: false,
                    ordering: false,
                    info: false,
                    responsive:true,
                    buttons: ['copy', 'csv', 'excel', 'pdf', 'print'],
                    // buttons: [
                    // ],
                    searching: false
                });

                table.buttons().container().appendTo('#' + table_id+'_wrapper .col-md-6:eq(0)');
            }

            if (tables.length > 0) {
                $('#tables-pivot-link').html('<span class="bi bi-table"></span> Tables (' + tables.length + ')');
            }
            else {
                $('#tables-pivot-link').hide();
            }
        }
        else {
            $('#tables-pivot-link').hide();
        }
    },

    TablesSearch: function (query) {

        Microsoft.Search.setQueryInProgress();

        if (query !== undefined && query !== null) {
            $("#q").val(query)
        }

        if (Microsoft.Search.currentPage > 0) {
            if (Microsoft.View.currentQuery !== $("#q").val()) {
                Microsoft.Search.ResetSearch();
            }
        }
        Microsoft.View.currentQuery = $("#q").val();

        // Get center of map to use to score the search results

        $.postAPIJSON('/api/search/getdocuments',
            {
                queryText: Microsoft.View.currentQuery !== undefined ? Microsoft.View.currentQuery : "*",
                searchFacets: Microsoft.Facets.selectedFacets,
                currentPage: ++Microsoft.Search.currentPage,
                parameters: Microsoft.Search.Parameters,
                options: Microsoft.Search.Options,
                incomingFilter: 'tables_count ge 1'
            },
            function (data) {
                Microsoft.Tables.TablesUpdate(data, Microsoft.Search.currentPage);
            });
    },

    TablesUpdate: function(data, currentPage) {

        Microsoft.Search.ProcessSearchResponse(data,this.MAX_NUMBER_ITEMS_PER_PAGE); 

        // TABLES
        Microsoft.Tables.UpdateTablesResults(Microsoft.Search.results, currentPage);
    
    },
    
    UpdateTablesResults: function(results, currentPage) {

        if (currentPage === 1) {
            $(Microsoft.Tables.view_tag_id).empty();
        }

        if (results && results.length > 0) {
            for (var i = 0; i < results.length; i++) {
                var docresult = results[i].Document !== undefined ? results[i].Document : results[i];
                Microsoft.Tables.render_table_result(docresult, Microsoft.Tables.view_tag_id);
            }
        }

        return '';
    }
}

// export default Microsoft.Tables;