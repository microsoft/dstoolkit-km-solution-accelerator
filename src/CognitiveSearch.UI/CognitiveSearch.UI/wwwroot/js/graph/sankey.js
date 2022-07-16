// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

Microsoft.d3_sankey = Microsoft.d3_sankey || {};
Microsoft.d3_sankey = {
    margin: {
        top: 1,
        right: 1,
        bottom: 6,
        left: 1
    },
    width: 1000,
    height: 600,
    units: "TWh",
    count: 0,
    convert_node: function (d) {
        d["category"] = d.label.replace(/ .*/, "")
    },
    convert_edge: function (d) {
        return {
            source: d.source, target: d.target, value: d.metadata.weight
        };
    },
    chart: function (data) {

        var temp_nodes = Object.keys(data.graph.nodes).map((key) => data.graph.nodes[key]);
        var temp_links = data.graph.edges.map(d => this.convert_edge(d));

        var container = d3.select("#graph-svg");

        var svg = container.append("svg")
            .attr("width", this.width + this.margin.left + this.margin.right)
            .attr("height", this.height + this.margin.top + this.margin.bottom)
            .attr("viewBox", [0, 0, this.width, this.height]);

        try {
            const sankey = d3.sankey()
                .nodeId(d => d.label)
                //.nodeAlign(d3[`sankey${align[0].toUpperCase()}${align.slice(1)}`])
                .nodeWidth(15)
                .nodePadding(10)
                //.nodeAlign('justify')
                .size([this.width, this.height])
                .extent([[1, 5], [this.width - 1, this.height - 5]]);


            const { nodes, links } = sankey({
                nodes: temp_nodes,
                links: temp_links
            });

            // Now we have the data in the correct format, we can focus on rendering

            const node = svg.append("g")
                .attr("class", "node")
                .attr("stroke", "#000")
                .selectAll("rect")
                .data(nodes)
                .join("rect")
                .attr("x", d => d.x0)
                .attr("y", d => d.y0)
                .attr("height", d => d.y1 - d.y0)
                .attr("width", d => d.x1 - d.x0)
                .attr("fill", this.color)
                .on('click', Microsoft.Graph.node_click)
                .on("mouseover", Microsoft.Graph.node_mouseover)
                .on("mouseout", Microsoft.Graph.node_mouseout);

            node.append("title")
                .text(this.node_title);

            const link = svg.append("g")
                .attr("fill", "none")
                .attr("stroke-opacity", 0.5)
                .selectAll("g")
                .data(links)
                .join("g")
                .style("mix-blend-mode", "multiply");

            // select
            var edgeColor = "path";

            if (edgeColor === "path") {
                const gradient = link.append("linearGradient")
                    .attr("id", d => (d.uid = this.generate_uid("link")).id)
                    .attr("gradientUnits", "userSpaceOnUse")
                    .attr("x1", d => d.source.x1)
                    .attr("x2", d => d.target.x0);

                gradient.append("stop")
                    .attr("offset", "0%")
                    .attr("stop-color", d => this.color(d.source));

                gradient.append("stop")
                    .attr("offset", "100%")
                    .attr("stop-color", d => this.color(d.target));
            }

            link.append("path")
                .attr("class", "link")
                .attr("d", d3.sankeyLinkHorizontal())
                .attr("stroke", d => edgeColor === "none" ? "#aaa"
                    : edgeColor === "path" ? d.uid.url
                        : edgeColor === "input" ? this.color(d.source)
                            : this.color(d.target))
                .attr("stroke-width", d => Math.max(3, d.width));


            link.append("title")
                .text(this.edge_title);

            const labels = svg.append("g")
                .attr("font-family", "Segoe UI, sans-serif")
                .attr("font-size", 12)
                .attr("font-weight", "bold")
                .selectAll("text")
                .data(nodes)
                .join("text")
                .attr("x", d => d.x0 < this.width / 2 ? d.x1 + 6 : d.x0 - 6)
                .attr("y", d => (d.y1 + d.y0) / 2)
                .attr("dy", "0.35em")
                .attr("text-anchor", d => d.x0 < this.width / 2 ? "start" : "end")
                .text(d => d.label);
        }
        catch (err) {
            throw new Error("D3 sankey exception - " + err);
        }

        function dragmove(d) {
            d3.select(this).attr("transform", 
                "translate(" + (
                       d.x = Math.max(0, Math.min(this.width - d.dx, d3.event.x))
                    ) + "," + (
                           d.y = Math.max(0, Math.min(this.height - d.dy, d3.event.y))
                    ) + ")");
            sankey.relayout();
            link.attr("d", path);
        }
    },
    generate_uid: function (name) {
        var id = ("O-" + (name == null ? "" : name + "-") + ++this.count);
        var href = new URL(`#${id}`, location) + "";
        return { id: id, url: "url(" + href + ")" };
    },
    format: function (d) {
        const format = d3.format(",.0f");
        return this.units ? d => ("" + format(d) + " " + this.units) : format;
    },
    color: function (d) {
        if (d.metadata.level > 2) {
            if (d.metadata.target_count > 1) {
                return Microsoft.Graph.d3_entity_color_range(d.metadata.subtypeidx);
            }
            else {
                return "#FFF";
            }
        }

        return Microsoft.Graph.d3_entity_color_range(d.metadata.subtypeidx);
    },
    node_title: function (d) {
        return ("" + d.label + "\n" + d.metadata.weight);
    },
    edge_title: function (d) {
        return ("" + d.source.label + " " + d.target.label + "\n" + d.value);
    }
};

export default Microsoft.d3_sankey;