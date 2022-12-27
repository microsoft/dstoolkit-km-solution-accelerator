// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// KVP = Key Value Pairs

Microsoft.Search = Microsoft.Search || {};
Microsoft.Search.Results = Microsoft.Search.Results || {};
Microsoft.Search.Results.KVP = Microsoft.Search.Results.KVP || {};
Microsoft.Search.Results.KVP = {

    render_tab: function (docresult) {
        return this.render_KVP_results(docresult);;
    },

    render_KVP_results: function (docresult) {

        var containerHTML = '';

        if (docresult.kvs_count > 0) {

            var kvps = JSON.parse(docresult.kvs);
            var table_id = 'kvs-'+docresult.index_key;

            containerHTML += '<table id='+table_id+' class="table metadata-table table-hover table-striped">';
            containerHTML += '<thead><tr><th data-field="key" class="key">Key</th><th data-field="value">Value</th></tr></thead>';
            containerHTML += '<tbody>';

            for (var k = 0; k < kvps.length; k++) {
                var pair = kvps[k];
                containerHTML += '<tr><td class="key">' + pair.key + '</td><td class="wrapword text-break">' + pair.value + '</td></tr>';                                    
            };

            containerHTML += '</tbody>';
            containerHTML += '</table>';

            $('#kvp-pivot-link').append(' (' + docresult.kvs_count + ')');

            $('#kvp-viewer').html(containerHTML);

            var table = $('#' + table_id).DataTable({
                dom: 'Bfrtip',
                paging: false,
                ordering: false,
                info: false,
                responsive:true,
                buttons: ['copy', 'csv', 'excel', 'pdf', 'print'],
                searching: false
            });
    
            table.buttons().container().appendTo('#' + table_id+'_wrapper .col-md-6:eq(0)');
        }
        else {
            $('#kvp-pivot-link').hide();
        }

        return containerHTML;
    }
}
