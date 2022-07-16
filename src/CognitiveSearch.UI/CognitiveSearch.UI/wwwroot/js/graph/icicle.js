// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

//CREDITS https://observablehq.com/@d3/zoomable-icicle

Microsoft.d3_icicle = Microsoft.d3_icicle || {};
Microsoft.d3_icicle = {
    margin: {
        top: 1,
        right: 1,
        bottom: 1,
        left: 1
    },
    width: 800,
    height: 600,
    flaredata: null,
    format: d3.format(",d"),
    init: function () {
        return new Promise((resolve, reject) => {
            $.ajax({
                type: 'GET',
                url: '/config/static-flare.json',
                dataType: 'json',
                success: function (data) {
                    d3_icicle.flaredata = data;
                    resolve()
                },
                error: function (error) {
                    reject(error)
                },
            })
        });
    },
    partition: function (data) {
        const root = d3.hierarchy(data)
            .sum(d => d.value)
            .sort((a, b) => b.height - a.height || b.value - a.value);
        return d3.partition()
            .size([this.height, (root.height + 1) * this.width / 3])
            (root);
    },
    chart: function (tagid, customClickHandling) {

        if (!this.flaredata) {
            return;
        }
        else {
            data = this.flaredata;
        }

        const color = d3.scaleOrdinal(d3.quantize(d3.interpolateRainbow, data.children.length + 1));

        if (!tagid) {
            tagid = "#graph-svg-icicle";
        }

        d3.select(tagid).empty();

        const root = this.partition(data);

        let focus = root;

        const svg = d3.select(tagid).append("svg")
            .attr("viewBox", [0, 0, this.width, this.height])
            .attr("width", this.width + this.margin.left + this.margin.right)
            .attr("height", this.height + this.margin.top + this.margin.bottom)
            .style("font", "14px verdana");

        const cell = svg
            .selectAll("g")
            .data(root.descendants())
            .join("g")
            .attr("transform", d => `translate(${d.y0},${d.x0})`);

        const rect = cell.append("rect")
            .attr("width", d => rectWidth(d))
            .attr("height", d => rectHeight(d))
            .attr("fill-opacity", 0.6)
            .attr("fill", d => {
                if (!d.depth) return "#ccc";
                while (d.depth > 1) d = d.parent;
                return color(d.data.name);
            })
            .style("cursor", "pointer")
            .on("click", clicked);

        const text = cell.append("text")
            .style("user-select", "none")
            .attr("pointer-events", "none")
            .attr("x", 5)
            .attr("y", 20)
            .attr("fill-opacity", d => +labelVisible(d));

        text.append("tspan")
            //.attr("font","bold 12px sans-serif")
            // .attr("x", 5)
            // .attr("y", 20)
            .attr("fill", "white")
            .text(d => d.data.name);

        //const tspan = text.append("tspan")
        //    .attr("class", "text-white")
        //    .attr("fill-opacity", d => labelVisible(d) * 0.7)
        //    .text(d => ` ${this.format(d.value)}`);

        cell.append("title")
            .text(d => `${d.ancestors().map(d => d.data.name).reverse().join("/")}\n${this.format(d.value)}`);

        function clicked(event, p) {
            focus = focus === p ? p = p.parent : p;

            root.each(d => d.target = {
                x0: (d.x0 - p.x0) / (p.x1 - p.x0) * d3_icicle.height,
                x1: (d.x1 - p.x0) / (p.x1 - p.x0) * d3_icicle.height,
                y0: d.y0 - p.y0,
                y1: d.y1 - p.y0
            });

            const t = cell.transition().duration(750)
                .attr("transform", d => `translate(${d.target.y0},${d.target.x0})`);

            rect.transition(t).attr("height", d => rectHeight(d.target));
            text.transition(t).attr("fill-opacity", d => +labelVisible(d.target));
            //tspan.transition(t).attr("fill-opacity", d => labelVisible(d.target) * 0.7);
        }

        function rectWidth(d) {
            // if (!d.depth) return "150";
            return d.y1 - d.y0 - 1;
        }
        function rectHeight(d) {
            return d.x1 - d.x0 - Math.min(1, (d.x1 - d.x0) / 2);
        }

        function labelVisible(d) {
            return d.y1 <= d3_icicle.width && d.y0 >= 0 && d.x1 - d.x0 > 16;
        }
    },

    load: function () {
        d3_icicle.init().then(() => {
            d3_icicle.chart();
        });
    }
}

export default Microsoft.d3_icicle;