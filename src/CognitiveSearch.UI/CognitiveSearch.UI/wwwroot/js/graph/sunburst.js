// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

//CREDITS https://observablehq.com/@d3/zoomable-sunburst

Microsoft.d3_sunburst = Microsoft.d3_sunburst || {};
Microsoft.d3_sunburst = {
    margin: {
        top: 1,
        right: 1,
        bottom: 1,
        left: 1
    },
    width: 800,
    height: 1000,
    flaredata: null,
    format: d3.format(",d"),
    radius: 0,
    init: function () {
        return new Promise((resolve, reject) => {
            $.ajax({
                type: 'GET',
                url: '/config/static-flare.json',
                dataType: 'json',
                success: function (data) {
                    d3_sunburst.flaredata = data;
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
            .sort((a, b) => b.value - a.value);
        return d3.partition()
            .size([2 * Math.PI, root.height + 1])
            (root);
    },
    chart: function (tagid) {

        if (!this.flaredata) {
            return;
        }
        else {
            data = this.flaredata;
        }

        if (!tagid) {
            tagid = "#graph-svg-sunburst";
        }

        d3.select(tagid).empty();

        // Ensure we have a radius
        this.radius = (Math.min(this.width, this.height) / 8) - 10;

        const arc = d3.arc()
            .startAngle(d => d.x0)
            .endAngle(d => d.x1)
            .padAngle(d => Math.min((d.x1 - d.x0) / 2, 0.005))
            .padRadius(this.radius * 1.5)
            .innerRadius(d => d.y0 * this.radius)
            .outerRadius(d => Math.max(d.y0 * this.radius, d.y1 * this.radius - 1))

        const color = d3.scaleOrdinal(d3.quantize(d3.interpolateRainbow, data.children.length + 1))

        const root = this.partition(data);

        root.each(d => d.current = d);

        const svg = d3.select(tagid).append("svg")
            .attr("viewBox", [0, 0, this.width, this.height])
            .attr("width", this.width + this.margin.left + this.margin.right)
            .attr("height", this.height + this.margin.top + this.margin.bottom)
            .style("font", "14px verdana");

        const g = svg.append("g")
            .attr("transform", `translate(${this.width / 2},${this.width / 2})`);

        const path = g.append("g")
            .selectAll("path")
            .data(root.descendants().slice(1))
            .join("path")
            .attr("fill", d => { while (d.depth > 1) d = d.parent; return color(d.data.name); })
            .attr("fill-opacity", d => arcVisible(d.current) ? (d.children ? 0.6 : 0.4) : 0)
            .attr("d", d => arc(d.current));

        path.filter(d => d.children)
            .style("cursor", "pointer")
            .on("click", clicked);

        path.append("title")
            .text(d => `${d.ancestors().map(d => d.data.name).reverse().join("/")}\n${this.format(d.value)}`);

        const label = g.append("g")
            .attr("pointer-events", "none")
            .attr("text-anchor", "middle")
            .style("user-select", "none")
            .selectAll("text")
            .data(root.descendants().slice(1))
            .join("text")
            .attr("fill", "white")
            .attr("dy", "0.35em")
            .attr("fill-opacity", d => +labelVisible(d.current))
            .attr("transform", d => labelTransform(d.current, this.radius))
            .text(d => d.data.name);

        const parent = g.append("circle")
            .datum(root)
            .attr("r", this.radius)
            .attr("fill", "none")
            .attr("pointer-events", "all")
            .on("click", clicked);

        function clicked(event, p) {
            parent.datum(p.parent || root);

            root.each(d => d.target = {
                x0: Math.max(0, Math.min(1, (d.x0 - p.x0) / (p.x1 - p.x0))) * 2 * Math.PI,
                x1: Math.max(0, Math.min(1, (d.x1 - p.x0) / (p.x1 - p.x0))) * 2 * Math.PI,
                y0: Math.max(0, d.y0 - p.depth),
                y1: Math.max(0, d.y1 - p.depth)
            });

            const t = g.transition().duration(750);

            // Transition the data on all arcs, even the ones that aren’t visible,
            // so that if this transition is interrupted, entering arcs will start
            // the next transition from the desired position.
            path.transition(t)
                .tween("data", d => {
                    const i = d3.interpolate(d.current, d.target);
                    return t => d.current = i(t);
                })
                .filter(function (d) {
                    return +this.getAttribute("fill-opacity") || arcVisible(d.target);
                })
                .attr("fill-opacity", d => arcVisible(d.target) ? (d.children ? 0.6 : 0.4) : 0)
                .attrTween("d", d => () => arc(d.current));

            label.filter(function (d) {
                return +this.getAttribute("fill-opacity") || labelVisible(d.target);
            }).transition(t)
                .attr("fill-opacity", d => +labelVisible(d.target))
                .attrTween("transform", d => () => labelTransform(d.current, d3_sunburst.radius));
        }

        function arcVisible(d) {
            return d.y1 <= 3 && d.y0 >= 1 && d.x1 > d.x0;
        }

        function labelVisible(d) {
            return d.y1 <= 3 && d.y0 >= 1 && (d.y1 - d.y0) * (d.x1 - d.x0) > 0.03;
        }

        function labelTransform(d, radius) {
            const x = (d.x0 + d.x1) / 2 * 180 / Math.PI;
            const y = (d.y0 + d.y1) / 2 * radius;
            return `rotate(${x - 90}) translate(${y},0) rotate(${x < 180 ? 0 : 180})`;
        }
    },

    load: function () {
        d3_sunburst.init().then(() => {
            d3_sunburst.chart();
        });
    }
}

export default Microsoft.d3_sunburst;