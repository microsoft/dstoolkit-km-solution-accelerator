// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.


//
// Graph - Static & Dynamic
//
Microsoft.Graph = Microsoft.Graph || {};
Microsoft.Graph = {
    isInitialized: false,
    graph_data: null,
    currentMaxNodes: 10,
    currentMaxLevels: 1,
    graphType: "fdgraph",

    currentEntity: "organizations",
    currentModel: "model2",
    currentVisualization: null,

    d3_color_range: null,
    d3_entity_color_range: null,
    selectedGraphFields: {},

    // Init function to add all necessary events listeners
    initEntityGraph: function () {
        try {
            // https://github.com/d3/d3-scale-chromatic/blob/master/README.md
            // D3 Variables
            //d3_color_range = d3.scaleOrdinal(d3.schemeCategory10);
            //d3_entity_color_range = d3.scaleOrdinal(d3.schemeSet3);

            this.d3_color_range = d3.scaleOrdinal(d3.schemeDark2);
            this.d3_entity_color_range = d3.scaleOrdinal(d3.schemeCategory10);

            if (d3_sankey) {
                this.currentVisualization = d3_sankey;
            }

        } catch (e) {
            console.log('d3_sankey visualisation not defined yet')
        }

        $(".checkbox-menu").on("change", "input[type='checkbox']", function () {
            $(this).closest("li").toggleClass("active", this.checked);
            if (this.checked)
                Microsoft.Graph.selectedGraphFields[this.value] = true;
            else 
            {
                delete Microsoft.Graph.selectedGraphFields[this.value];

            }

            Microsoft.Graph.SearchEntities($("#q").val());
        });

    },

    ClearEntities: function () {
        $(".rounded-pill").addClass("d-none");
        $(".rounded-pill").css("background-color", "unset");
    },

    RenderEntities: function () {
        // Check on the selected Entities for the graph
        var checkedFacet = [];

        this.ClearEntities();

        var keys = Object.keys(this.selectedGraphFields);

        for (var i = 0; i < keys.length; i++) {
            var fkey = '#' + keys[i] + '-badge'
            // Color index 0 is reserved to the query node itself...
            var bgcolor = this.d3_entity_color_range(checkedFacet.length + 1);
            $(fkey).removeClass("d-none");
            $(fkey).css("background-color", bgcolor);
            $(fkey+"-indicator").removeClass("d-none");
            $(fkey+"-indicator").css("background-color", bgcolor);
            checkedFacet.push(keys[i]);
        }
        return checkedFacet;
    },

    SearchEntities: function (query, checkedFacet, checkedModel) {

        if (Object.keys(this.selectedGraphFields).length == 0) {
            Microsoft.Search.setQueryCompleted();
            this.ClearEntities();
            $("#graph-message").html("No entities selected");
            // Clear the current SVG canva 
            d3.select("svg").remove();            
            return;
        }

        if (query !== undefined && query !== null) {
            $("#q").val(query)
        }

        if (Microsoft.Search.currentPage > 0) {
            if (Microsoft.View.currentQuery !== $("#q").val()) {
                Microsoft.Search.currentPage = 0;
            }
        }

        Microsoft.View.currentQuery = $("#q").val();

        // Check on the selected Entities for the graph
        var checkedFacet = this.RenderEntities();

        // See what Model is currently selected 
        if (this.checkedModel === undefined || this.checkedModel === null) {
            this.checkedModel = this.currentModel;
        }

        // Clear the current SVG canva
        d3.select("svg").remove();

        Microsoft.Search.setQueryInProgress();

        Microsoft.Graph.GetGraph(Microsoft.View.currentQuery, checkedFacet, checkedModel);
    },


    // Load Graph with Search data

    GetGraph: function (q, graphFacet, graphModel) {
        if (q === null) {
            q = "*";
        }

        $.postJSON("/api/graph/getgraphdata",
            {
                queryText: q,
                facets: graphFacet,
                graphType: Microsoft.Graph.graphType,
                maxLevels: Microsoft.Graph.currentMaxLevels,
                maxNodes: Microsoft.Graph.currentMaxNodes,
                searchFacets: Microsoft.Facets.selectedFacets,
                model: graphModel,
                options: Microsoft.Search.Options
            },
            function (data) {
                var temp_nodes = Object.keys(data.graph.nodes).map((key) => data.graph.nodes[key]);

                $("#graph-message").html(' Found ' + temp_nodes.length + ' Vertices & ' + data.graph.edges.length + ' Edges (' + Microsoft.Graph.currentModel + ')');

                Microsoft.Graph.graph_data = data;

                Microsoft.Graph.renderSVG();

                Microsoft.Facets.UpdateFilterReset();

                Microsoft.Search.setQueryCompleted();
            }
        );
    },

    changeModelType: function (value) {
        if (Microsoft.Graph.graphType != value) {
            Microsoft.Graph.currentModel = value;
            Microsoft.Graph.SearchEntities($("#q").val());
        }
    },

    renderSVG: function () {

        d3.select("svg").remove();

        // if (Microsoft.Graph.graphType === "icicle") Microsoft.Graph.d3_icicle.chart();
        // if (Microsoft.Graph.graphType === "sunburst") Microsoft.Graph.d3_sunburst.chart();
        if (Microsoft.Graph.graphType === "sankey") Microsoft.d3_sankey.chart(this.graph_data);
        if (Microsoft.Graph.graphType === "customgraph") Microsoft.d3_custom_graph.chart(this.graph_data);
        if (Microsoft.Graph.graphType === "fdgraph") Microsoft.d3_fdgraph.chart(this.graph_data);
    },

    changeGraphType: function (value) {
        if (Microsoft.Graph.graphType != value) {
            Microsoft.Graph.graphType = value;
            Microsoft.Graph.renderSVG();
        }
    },

    changeMaxLevels: function (value, commit) {
        if (Microsoft.Graph.currentMaxLevels != value || commit) {
            Microsoft.Graph.currentMaxLevels = value;
            if (commit)
            Microsoft.Graph.SearchEntities();
            else
            Microsoft.Graph.UpdateGraphParameterUI(); // Preview
        }
    },

    changeMaxNodes: function (value, commit) {
        if (Microsoft.Graph.currentMaxNodes != value || commit) {
            Microsoft.Graph.currentMaxNodes = value;
            if (commit)
            Microsoft.Graph.SearchEntities();
            else
            Microsoft.Graph.UpdateGraphParameterUI(); // Preview
        }
    },

    UpdateGraphParameterUI: function () {
        $("#lbl-currentMaxLevels").text(Microsoft.Graph.currentMaxLevels);
        $("#slider-currentMaxLevels").val(Microsoft.Graph.currentMaxLevels);
        $("#lbl-currentMaxNodes").text(Microsoft.Graph.currentMaxNodes);
        $("#slider-currentMaxNodes").val(Microsoft.Graph.currentMaxNodes);
    },


    node_click: function (event, d) {
        if (d.metadata.id !== 0) {
            Microsoft.Facets.ChooseFacet(d.metadata.subtype, Base64.encode(d.label), null, "dynamic", d.metadata.subtype);
        }
    },

    node_mouseover: function (event, d) {

        var nodeDetailsTable = '';
        nodeDetailsTable += '<div style="overflow-x:auto;">';
        nodeDetailsTable += '<h5>' + d.label + '</h5>';
        nodeDetailsTable += '<table class="table table-hover table-striped table-bordered">';
        nodeDetailsTable += '<thead><tr><th>Key</th><th>Value</th></tr></thead>';
        nodeDetailsTable += '<tbody>';

        //nodeDetailsTable += '<tr><td class="key">Label</td><td class="wrapword">' + d.label + '</td></tr>';

        for (var prop in d.metadata) {
            nodeDetailsTable += '<tr><td class="key">' + prop + '</td><td class="wrapword text-break" >' + d.metadata[prop] + '</td></tr>';
        }

        nodeDetailsTable += '</tbody>';
        nodeDetailsTable += '</table>';
        nodeDetailsTable += '</div>';

        $("#node-viewer").html(nodeDetailsTable);
    },

    node_mouseout: function (event, d) {
        //$("#node-viewer").style.display = "none";
    },

    link_mouseover: function (event, d) {

        var nodeDetailsTable = '';
        nodeDetailsTable += '<div style="overflow-x:auto;">';

        if (d.directed) {
            nodeDetailsTable += '<h5>' + d.source.label + ' <span class="bi bi-arrow-right-circle-fill"></span> ' + d.target.label + '</h5>';
        }
        else {
            nodeDetailsTable += '<h5>' + d.source.label + ' <span class="bi bi-loop"></span> ' + d.target.label + '</h5>';
        }

        nodeDetailsTable += '<table class="table table-hover table-striped table-bordered">';
        nodeDetailsTable += '<thead><tr><th>Key</th><th>Value</th></tr></thead>';
        nodeDetailsTable += '<tbody>';

        nodeDetailsTable += '<tr><td class="key">Directed</td><td class="wrapword text-break">' + d.directed + '</td></tr>';
        nodeDetailsTable += '<tr><td class="key">Relation</td><td class="wrapword text-break">' + d.relation + '</td></tr>';

        for (var prop in d.metadata) {
            nodeDetailsTable += '<tr><td class="key">' + prop + '</td><td class="wrapword text-break" >' + d.metadata[prop] + '</td></tr>';
        }

        nodeDetailsTable += '</tbody>';
        nodeDetailsTable += '</table>';
        nodeDetailsTable += '</div>';

        $("#link-viewer").html(nodeDetailsTable);
    },

    link_mouseout: function (event, d) {
        //$("#node-viewer").style.display = "none";
    },


    // CREDIT https://www.sitepoint.com/javascript-generate-lighter-darker-color/
    ColorLuminance: function (hex, lum) {

        // validate hex string
        hex = String(hex).replace(/[^0-9a-f]/gi, '');
        if (hex.length < 6) {
            hex = hex[0] + hex[0] + hex[1] + hex[1] + hex[2] + hex[2];
        }
        lum = lum || 0;

        // convert to decimal and change luminosity
        var rgb = "#", c, i;
        for (i = 0; i < 3; i++) {
            c = parseInt(hex.substr(i * 2, 2), 16);
            c = Math.round(Math.min(Math.max(0, c + (c * lum)), 255)).toString(16);
            rgb += ("00" + c).substr(c.length);
        }

        return rgb;
    }

}

export default Microsoft.Graph;