// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// Base Graph class & functions

class base_graph {
    constructor(data) {
        this.stats = data.graph.metadata;
        this.nodes = Object.keys(data.graph.nodes).map((key) => data.graph.nodes[key]);
        this.links = data.graph.edges;

        // Common attributes
        this.nodeRadius = 10;
    }

    get_stats() {
        return this.stats;
    }

    // Links methods
    link_stroke_color(d) {
        return Microsoft.Graph.d3_color_range(d.metadata.level);
    }

    // Nodes methods
    node_click(d, node) {
        if (node.metadata.id !== 0) {
            Microsoft.Facets.ChooseFacet(node.metadata.subtype, Base64.encode(node.label), null, "dynamic", node.metadata.subtype);
        }
    }

    node_title(d, i) {
        return d.label + " [" + d.metadata.subtype + "]\nLevel:" + d.metadata.level + "\nWeight:" + d.metadata.weight + "\nCount:" + d.metadata.count + "\nSource:" + d.metadata.source_count + "\nTarget:" + d.metadata.target_count;
    }

    node_color(d, i) {
        if (d.metadata.level > 2) {
            if (d.metadata.target_count > 1) {
                return Microsoft.Graph.d3_entity_color_range(d.metadata.subtypeidx);
            }
            else {
                return "#FFF";
            }
        }

        return Microsoft.Graph.d3_entity_color_range(d.metadata.subtypeidx);
    }

    node_stroke_color(d) {
        if (d.metadata.level > 2) {
            if (d.metadata.target_count > 1) {
                return "#FFF";
            }
            else {
                return Microsoft.Graph.ColorLuminance(Microsoft.Graph.d3_entity_color_range(d.metadata.subtypeidx), 0.2);
            }
        }
        return "#FFF"
    }
}

class d3_custom_graph extends base_graph {

    constructor(data) {
        super(data);
        this.width = 1200;
        this.height = 600;

        this.nodeSeparationFactor = 5;
        this.nodeChargeStrength = -200;
        this.nodeChargeAccuracy = 0.5;
        this.nodeDistance = 250;

    }

    chart() {

        var stats = this.get_stats();

        var container = d3.select("#graph-svg");

        var svg = container.append("svg")
            .attr("viewBox", [0, 0, this.width, this.height])
            .attr("width", this.width)
            .attr("height", this.height);

        // Zoom configuration
        var zoom = d3.zoom()
            .scaleExtent([1, 8])
            // .translateExtent([[0, 0], [this.width, this.height]])            
            .extent([[0, 0], [this.width, this.height]])
            .on("zoom", zoomed);
        svg.call(zoom);

        var simulation = d3.forceSimulation(this.nodes)
            .force("link", d3.forceLink(this.links).id(d => d.label))
            //.force("collide", d3.forceCollide(this.nodeRadius+5))
            .force("charge", d3.forceManyBody())
            .force("center", d3.forceCenter(this.width / 2, this.height / 2));

        var rootNode = svg.append("g");
        
        var link = rootNode.append("g")
            .selectAll("line")
            .data(this.links)
            .join("line")
                .attr("class", "link")
                .attr("stroke", this.link_stroke_color)
                .attr("stroke-opacity", link_stroke_opacity)
                .attr("stroke-width", d => Math.min(2, Math.sqrt(d.metadata.weight)))
                .on("mouseover", Microsoft.Graph.link_mouseover)
                .on("mouseout", Microsoft.Graph.link_mouseout);

        // Working
        var node = rootNode.append("g")
            .selectAll("circle")
            .data(this.nodes)
            .join("circle")
                .attr("stroke-width", 1.5)
                .attr("stroke", this.node_stroke_color)
                .attr("r", this.nodeRadius)
                .attr("fill", this.node_color)
                .on('click', this.node_click)
                .on("mouseover", Microsoft.Graph.node_mouseover)
                .on("mouseout", Microsoft.Graph.node_mouseout)
                .call(this.drag(simulation));

        // Working with icons
        //const node = svg.append("g")
        //    .attr("stroke", "#fff")
        //    .attr("stroke-width", 1.5)
        //    .selectAll("image")
        //    .data(nodes)
        //    .join("image")
        //        .attr("xlink:href", this.node_icon)
        //        .attr("x", -16)
        //        .attr("y", -16)
        //        .attr("width", 16)
        //        .attr("height", 16)
        //        .call(this.drag(simulation));

        //const node = svg.append("g")
        //    .attr("stroke", "#fff")
        //    .attr("stroke-width", 1.5)
        //    .selectAll("g")
        //    .data(nodes)
        //    .join("g")
        //    .call(this.drag(simulation));


        // Nodes elements 
        node.append("title")
            .text(this.node_Title);

        //node.append("text")
        //    .attr("dx", function (d, i) { d.id + 15; })
        //    .attr("dy", ".35em")
        //    .attr("fill", this.colorText)
        //    .text(this.fillText);


        var nodelabels = rootNode.append("g")
            .attr("font-family", "Segoe UI, sans-serif")
            .attr("font-size", 8)
            .attr("font-weight", "bold")
            .selectAll(".nodelabel")
            .data(this.nodes)
            .enter()
            .append("text")
            .attr("x", 16)
            .attr("y", 0)
            .attr("class", "nodelabel")
            .attr("font-weight", "bold")
            .attr("stroke", label_stroke_color)
            .text(this.fillText);

        //node.append("image")
        //    .attr("xlink:href", "https://github.com/favicon.ico")
        //    .attr("x", -8)
        //    .attr("y", -8)
        //    .attr("width", 16)
        //    .attr("height", 16);

        simulation.on("tick", () => {
            link
                .attr("x1", d => d.source.x)
                .attr("y1", d => d.source.y)
                .attr("x2", d => d.target.x)
                .attr("y2", d => d.target.y);

            node
                .attr("cx", d => d.x)
                .attr("cy", d => d.y);
            //.attr("x", d => d.x)
            //.attr("y", d => d.y);
            //.attr("transform", d => translate(d.x,d.y));

            nodelabels
                .attr("x", function (d) { return d.x; })
                .attr("y", function (d) { return d.y; });
        });

        // Zoom to fit the graph
        var rect = svg.node().getBoundingClientRect();
        if (rect.width && rect.height) {
            var scale = Math.min(this.width / rect.width, this.height / rect.height) * 0.95;
            zoom.scaleTo(svg, scale);
            zoom.translateTo(svg, rect.width / 2, rect.height / 2);
        }

        function zoomed({ transform }) {
            node.attr("transform", transform);
            link.attr("transform", transform);
            nodelabels.attr("transform", transform);
        }

        function link_stroke_opacity(d) {
            var normalized_weight = d.metadata.weight / stats.edges[d.metadata.level - 2].MaxWeight;
            return Math.max(0.1, normalized_weight);
        }

        function label_stroke_color(d) {
            return "inherit";
        }
    }

    drag = simulation => {

        function dragstarted(event) {
            if (!event.active) simulation.alphaTarget(0.3).restart();
            event.subject.fx = event.subject.x;
            event.subject.fy = event.subject.y;
        }

        function dragged(event) {
            event.subject.fx = event.x;
            event.subject.fy = event.y;
        }

        function dragended(event) {
            if (!event.active) simulation.alphaTarget(0);
            event.subject.fx = null;
            event.subject.fy = null;
        }

        return d3.drag()
            .on("start", dragstarted)
            .on("drag", dragged);
        //.on("end", dragended);
    }

    node_icon(d, i) {
        if (i === 0) {
            return "https://microsoft.com/favicon.ico";
        } else {
            return "https://github.com/favicon.ico";
        }
    }
    posCircle(d, i) {
        if (d.id === 0) {
            return 0;
        } else {
            return 50;
        }
    }

    //clickCircle(d, node) {

    //    if (node.metadata.id !== 0) {
    //        var temp = $("#q").val();
    //        if (temp.length === 0) {
    //            $("#q").val("(" + node.label + ")");
    //        }
    //        else {
    //            $("#q").val("(" + temp + ")+(" + node.label + ")");
    //        }
    //        // TODO do a refinement query here. 
    //        SearchEntities();
    //    }
    //}

    strokeCircle(d, i) {
        if (d.id === 0) {
            return 3;
        }
        else if ((d.metadata.weight * 0.25) > 2.5) { return 2.5; }
        else {
            return d.metadata.weight * 0.25;
        }
    }

    displayCircle(d, i) {
        if (d.id === 0) {
            return "inherit";
        }
        else if (d.metadata.level > 2) {
            if (d.metadata.target_count === 0) {
                return "none";
            }
            else {
                return "inherit";
            }
        }
        else {
            return "inherit";
        }
    }

    colorLink(d, i) {
        return d3_color_range(d.metadata.level);
    }

    linkStrokeWidth(d, i) {
        if (d.metadata.level === 1) {
            if ((d.metadata.weight * 0.8) > 30) {
                return "30px";
            }
            else {
                return "" + d.metadata.weight * 0.8 + "px";
            }
        }
        else {
            if ((d.metadata.weight * 0.8) > 10) {
                return "10px";
            }
            else {
                return "" + d.metadata.weight * 0.8 + "px";
            }
        }
    }

    colorText(d, i) {
        if (d.id === 0) {
            return "White";
        }
        else {
            return "black";
        }
    }

    fillText(d, i) {
        if (i === 0) {
            return "";
        }
        else {
            return d.label;
        }
    }
}

class d3_fdgraph extends base_graph {

    constructor(data) {
        super(data);
        this.width=1200;
        this.height=600;
    }

    chart() {
        //// Convert the JSON Graph to expected format for D3 FD Graph
        //var nodes = Object.keys(data.graph.nodes).map((key) => data.graph.nodes[key]);
        //var links = data.graph.edges;
        var stats = this.get_stats();

        var container = d3.select("#graph-svg");

        var svg = container.append("svg")
            .attr("viewBox", [0, 0, this.width, this.height])
            .attr("width", this.width)
            .attr("height", this.height);

        // Zoom configuration
        const zoom = d3.zoom()
            .scaleExtent([1, 8])
            // .translateExtent([[0, 0], [this.width, this.height]])            
            .extent([[0, 0], [this.width, this.height]])
            .on("zoom", zoomed);
        svg.call(zoom);

        var simulation = d3.forceSimulation(this.nodes)
            .force("link", d3.forceLink(this.links).id(d => d.label))
            .force("charge", d3.forceManyBody())
            .force("center", d3.forceCenter(this.width / 2, this.height / 2));

        var rootNode = svg.append("g");

        // Edges

        var link = rootNode.append("g")
            .selectAll("line")
            .data(this.links)
            .join("line")
                .attr("class", "link")
                .attr("stroke", this.link_stroke_color)
                .attr("stroke-opacity", link_stroke_opacity)
                .attr("stroke-width", d => Math.min(2, Math.sqrt(d.metadata.weight)))
                .on("mouseover", Microsoft.Graph.link_mouseover)
                .on("mouseout", Microsoft.Graph.link_mouseout);

        // WORKING ONE
        var node = rootNode.append("g")
            .selectAll("circle")
            .data(this.nodes)
            .join("circle")
                .attr("r", this.nodeRadius)
                .attr("fill", this.node_color)
                .attr("stroke", this.node_stroke_color)
                .attr("stroke-width", 1.5)
                .attr("class", "node")
                .on("mouseover", Microsoft.Graph.node_mouseover)
                .on("mouseout", Microsoft.Graph.node_mouseout)
                .on('click', this.node_click)
                .call(drag(simulation));

        node.append("title")
            .text(this.node_title);

        node.append("text")
            .attr("dx", 12)
            .attr("dy", ".35em")
            .attr("font-weight", "bold")
            .text(function (d) { return d.label; });

        simulation.on("tick", () => {
            link
                .attr("x1", d => d.source.x)
                .attr("y1", d => d.source.y)
                .attr("x2", d => d.target.x)
                .attr("y2", d => d.target.y);

            node
                .attr("cx", d => d.x)
                .attr("cy", d => d.y);
        });

        //invalidation.then(() => simulation.stop());

        // Zoom to fit
        var rect = svg.node().getBoundingClientRect();
        if (rect.width && rect.height) {
            var scale = Math.min(this.width / rect.width, this.height / rect.height) * 0.95;
            zoom.scaleTo(svg, scale);
            zoom.translateTo(svg, rect.width / 2, rect.height / 2);
        }

        function zoomed ({ transform }) {
            node.attr("transform", transform);
            link.attr("transform", transform);
        }

        function link_stroke_opacity(d) {
            var normalized_weight = d.metadata.weight / stats.edges[d.metadata.level - 2].MaxWeight;
            return Math.max(0.1, normalized_weight);
        }

        function drag(similation) {

            function dragstarted(event) {
                if (!event.active) simulation.alphaTarget(0.3).restart();
                event.subject.fx = event.subject.x;
                event.subject.fy = event.subject.y;
            }

            function dragged(event) {
                event.subject.fx = event.x;
                event.subject.fy = event.y;
            }

            function dragended(event) {
                if (!event.active) simulation.alphaTarget(0);
                event.subject.fx = null;
                event.subject.fy = null;
            }

            return d3.drag()
                .on("start", dragstarted)
                .on("drag", dragged);
                //.on("end", dragended);
        }
    }
}