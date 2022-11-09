// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

//
// Search FACETS - Static & Dynamic
//
Microsoft.Facets = Microsoft.Facets || {};
Microsoft.Facets = {
    isInitialized: false,
    facets: [],
    static_facets: [],
    selectedFacets: [],

    FACET_DEFAULT_TAGID: "#facet-nav",
    STATIC_FACET_DEFAULT_TAGID: "#search-facet-nav",

    init: function () {
        return new Promise((resolve, reject) => {
            $.ajax({
                type: 'GET',
                url: '/config/search-facets.json',
                dataType: 'json',
                success: function (data) {
                    Microsoft.Facets.isInitialized = true;
                    Microsoft.Facets.static_facets = data;
                    resolve()
                },
                error: function (error) {
                    reject(error)
                },
            })
        });
    },

    dateRanges: {},

    initDateRangeFilter: function () {
        //
        // CREDITS http://boolie.github.io/ 
        //
        this.dateRanges = {
            'Today': [moment(), moment()],
            'Yesterday': [moment().subtract(1, 'days'), moment().subtract(1, 'days')],
            'Last 7 Days': [moment().subtract(6, 'days'), moment()],
            'Last 14 Days': [moment().subtract(13, 'days'), moment()],
            'Last 30 Days': [moment().subtract(29, 'days'), moment()],
            'This Month': [moment().startOf('month'), moment().endOf('month')],
            'Last Month': [moment().subtract(1, 'month').startOf('month'), moment().subtract(1, 'month').endOf('month')]
        };

        $('#daterange').daterangepicker({
            format: 'YYYY-MM-DD',
            startDate: moment().subtract(1, 'month'),
            endDate: moment().endOf('month'),
            showRangeInputsOnCustomRangeOnly: true,
            ranges: Microsoft.Facets.dateRanges
        });

        $('#daterange').on('apply.daterangepicker', function (ev, picker) {
            Microsoft.Facets.ChooseDateRange(picker);
        });
    },

    GetTrendsFilter: function () {

        var facet_key = "Last Modified";
        var start_date = moment().subtract(30, 'days').startOf('day');
        var end_date = moment().endOf('day');

        return {
            key: facet_key,
            values: [{ value: start_date }, { value: end_date }],
            type: "daterange",
            target: "last_modified",
            label: 'Last 30 Days'
        };
    },

    ChooseDateRange: function (picker) {

        var facet_key = "Last Modified";

        var start_date = picker.startDate.format('YYYY-MM-DD');
        var end_date = picker.endDate.format('YYYY-MM-DD');

        if (this.selectedFacets !== undefined) {

            // facetValues where key == selected facet
            var result = this.selectedFacets.filter(function (f) { return f.key === facet_key; })[0];

            if (result) { // if that facet exists, update the range and label
                result.values = [{ value: start_date }, { value: end_date }];
                result.label = picker.chosenLabel ? picker.chosenLabel : null;
            }
            else {
                Microsoft.Facets.selectedFacets.push({
                    key: facet_key,
                    values: [{ value: start_date }, { value: end_date }],
                    type: "daterange",
                    target: "last_modified",
                    label: picker.chosenLabel === "Custom Range" ? (start_date + '..' + end_date) : picker.chosenLabel
                });
            }
        }

        Microsoft.Search.ReSearch();
    },

    // Home Facets
    load: function () {
        Microsoft.Facets.init().then(() => {
            Microsoft.Facets.renderHomeFacets();
        });
    },

    get_static_facet_by_id: function (value) {
        for (var i = 0; i < this.static_facets.length; i++) {
            if (this.static_facets[i].id === value) {
                return this.static_facets[i];
            }
        }
    },

    GetFacetAccordionHeaderId(nameid) {
        return nameid + '-facets';
    },

    GetFacetAccordionHeaderButtonId(nameid) {
        return this.GetFacetAccordionHeaderId(nameid) + '-button';
    },

    GetFacetAccordionItemId(nameid) {
        return this.GetFacetAccordionHeaderId(nameid) + "-accordion-item";
    },

    renderHomeFacets: function () {
        var facetResultsHTML = '';

        for (var i = 0; i < this.static_facets.length; i++) {

            var item = this.static_facets[i];

            if (item.includeHomePage) {

                var nameid = item.id;
                var name = item.name;
                var data = item.values;
                var title = Microsoft.Utils.GetFacetDisplayTitle(name);

                var facet_target = item.target ? item.target : null;

                if (item.rendering === "grid") {

                    facetResultsHTML += '<div class="row row-cols-3 gx-3 gy-3">';

                    var facet_target = item.target ? item.target : null;

                    if (data) {

                        for (var j = 0; j < data.length; j++) {

                            var facet_value = this.EncodeFacetValue(data[j].value);

                            var clickMethod = null;

                            if (facet_target) {
                                clickMethod = '"Microsoft.Facets.ChooseFacet(\'' + nameid + '\',\'' + facet_value + '\', \'' + j + '\',\'' + item.type + '\',\'' + facet_target + '\');"';
                            }
                            else {
                                clickMethod = '"Microsoft.Facets.ChooseFacet(\'' + nameid + '\',\'' + facet_value + '\', \'' + j + '\',\'' + item.type + '\');"';
                            }

                            facetResultsHTML += '<div class="col">'
                            //facetResultsHTML += '<div class="bg-dark rounded">'

                            facetResultsHTML += '<a class="text-white" href="javascript:void(0)" onclick=' + clickMethod + '>';
                            facetResultsHTML += '<div class="news-carousel-item-body rounded" style="min-height:5rem">'
                            //facetResultsHTML += '<div class="news-carousel-item-title">';
                            facetResultsHTML += '<span>' + data[j].value + '</span>';
                            facetResultsHTML += '</div>';
                            facetResultsHTML += '</a>';

                            //facetResultsHTML += '<a target="_blank" class="text-white" href="void:javascript(0)" onclick=\'' + clickMethod + '\'> ' + data[j].value + '</a>';
                            //facetResultsHTML += '</div>';
                            facetResultsHTML += '</div>';
                        }
                    }

                    facetResultsHTML += '</div>';

                }
                else {
                    var facetId = this.GetFacetAccordionHeaderId(nameid);
                    var facetButton = this.GetFacetAccordionHeaderButtonId(nameid);

                    facetResultsHTML += '<div class="col accordion accordion-flush mb-3" id="parent-' + nameid + '">';
                    facetResultsHTML += '<div class="accordion-item web-facet-accordion-item rounded">';
                    facetResultsHTML += '<h2 class="accordion-header accordion-item-' + nameid + '" id="' + facetId + '">';
                    facetResultsHTML += '<button id="'+facetButton+'" class="accordion-button web-facet-accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#' + nameid + '" aria-expanded="false" aria-controls="' + nameid + '">';
                    facetResultsHTML += title;
                    facetResultsHTML += '</button>';
                    facetResultsHTML += '</h2>';
                    facetResultsHTML += '<div id="' + nameid + '" class="accordion-collapse collapse" role="group" aria-labelledby="' + facetId + '" data-bs-parent="#parent-' + nameid + '">';

                    if (item.target) {
                        facetResultsHTML += '<div id="' + item.target + '-accordion-body" class="accordion-body">';
                    }
                    else {
                        facetResultsHTML += '<div id="' + nameid + '-accordion-body" class="accordion-body">';
                    }

                    if (data) {
                        for (var j = 0; j < data.length; j++) {
                            facetResultsHTML += '<div class="form-check">';
                            var facet_value = this.EncodeFacetValue(data[j].value);
                            var selectMethod = null;
                            var clickMethod = null;

                            if (facet_target) {
                                selectMethod = '"Microsoft.Facets.ChooseFacet(\'' + nameid + '\',\'' + facet_value + '\', \'' + j + '\',\'' + item.type + '\',\'' + facet_target + '\',search=false);"';
                                clickMethod = '"Microsoft.Facets.ChooseFacet(\'' + nameid + '\',\'' + facet_value + '\', \'' + j + '\',\'' + item.type + '\',\'' + facet_target + '\');"';
                            }
                            else {
                                selectMethod = '"Microsoft.Facets.ChooseFacet(\'' + nameid + '\',\'' + facet_value + '\', \'' + j + '\',\'' + item.type + '\',search=false);"';
                                clickMethod = '"Microsoft.Facets.ChooseFacet(\'' + nameid + '\',\'' + facet_value + '\', \'' + j + '\',\'' + item.type + '\');"';
                            }
                            facetResultsHTML += '   <input class="form-check-input facet-checkbox" type="checkbox"  id="' + nameid + '_' + facet_value.replaceAll("=", "") + '" onclick=' + selectMethod + '>';
                            facetResultsHTML += '   <label class="form-check-label web-facet-label" for="' + nameid + '_' + facet_value.replaceAll("=", "") + '" onclick=' + clickMethod + '>';

                            if (data[j].count) {
                                facetResultsHTML += '   <span>' + data[j].value + ' (' + data[j].count + ')</span> ';
                            }
                            else {
                                facetResultsHTML += '   <span>' + data[j].value + '</span> ';
                            }
                            facetResultsHTML += '   </label>';
                            facetResultsHTML += '</div>';
                        }
                    }

                    facetResultsHTML += '</div></div></div>';

                    facetResultsHTML += '</div>';
                }
            }
        }

        $('#search-facets-accordion').html(facetResultsHTML);
    },

    ClearAllFilters: function () {

        if (this.selectedFacets.length > 0) {

            $('.facet-checkbox').prop('checked', false);
            $('.facet-button').hide();

            $('.accordion-button').removeClass('text-danger');
            
            this.selectedFacets = [];

            $("#filterReset").empty();

            Microsoft.Search.ReSearch();
        }
    },

    UpdateFilterReset: function () {
        // This allows users to remove filters
        var htmlString = '';
        $("#filterReset").empty();

        if (this.selectedFacets && this.selectedFacets.length > 0) {
            $('#navigation-btn').addClass('btn-danger');
            $('#navigation-clear-all').addClass('btn-danger');

            htmlString += '<div class="btn-group" role="group" aria-label="Filters">';

            this.selectedFacets.forEach(function (item, index, array) { // foreach facet with a selected value

                var name = item.key;

                if (item.type === "daterange") {

                    htmlString += '<button id="filter-daterange-btn" type="button" class="btn btn-outline-danger btn-sm facet-button me-2">';
                    var display_range_value = null;
                    if (item.values && item.values.length > 0) {
                        if (item.label) {
                            display_range_value = item.label
                        }
                        else {
                            display_range_value = item.values.map((entry) => entry.value).join(' - ')
                        }
                        htmlString += display_range_value + ' <a class="filter-anchor" title="Remove ' + name + ' date range filter ' + display_range_value + '..." href="javascript:void(0)" onclick="Microsoft.Facets.RemoveFilter(\'' + name + '\', \'' + Microsoft.Facets.EncodeFacetValue(display_range_value) + '\')"><span class="bi bi-x text-danger"></span></a><br>';
                    }
                    htmlString += '</button>';
                }
                else {
                    if (item.values && item.values.length > 0) {
                        item.values.forEach(function (item2, index2, array) {

                            var title = Microsoft.Utils.GetFacetDisplayTitle(name);
                            var facetValueId = Microsoft.Facets.GetFacetValueId(name, item2.value);

                            htmlString += '<button id="' + facetValueId +'-btn" type="button" class="btn btn-outline-danger btn-sm facet-button me-2">';
                            htmlString += item2.value + ' <a class="filter-anchor" title="Remove ' + title + ' filter ' + item2.value + '..." href="javascript:void(0)" onclick="Microsoft.Facets.RemoveFilter(\'' + name + '\', \'' + Microsoft.Facets.EncodeFacetValue(item2.value) + '\')"><span class="bi bi-x text-danger"></span></a><br>';

                            if ($('#' + facetValueId)) {
                                $('#' + facetValueId).prop('checked', true);
                                // Update the text color of the parent dropdown
                                var facetButton = Microsoft.Facets.GetFacetAccordionHeaderButtonId(Microsoft.Utils.jqid(name));
                                $('#'+facetButton).addClass('text-danger');
                            }

                            htmlString += '</button>';
                        });
                    }
                }
            });

            htmlString += '</div>';
        }
        else {
            $('#navigation-btn').removeClass('btn-danger');
            $('#navigation-clear-all').removeClass('btn-danger');
        }
        $("#filterReset").html(htmlString);
    },

    GetFacetValueId: function (name,value,padding=true) {
        return Microsoft.Utils.jqid(name) + "_" + Microsoft.Facets.EncodeFacetValue(value, padding);
    },

    RemoveFilter: function (facet, value, search = true) {

        var facetid = Microsoft.Utils.jqid(facet + "_" + value.replaceAll("=", ""));

        if ($('#' + facetid)) {
            $('#' + facetid).prop('checked', false);
        }

        value = Base64.decode(value);

        // Remove a facet
        var result = this.selectedFacets.filter(function (f) { return f.key === facet; })[0];

        if (result) { // if that facet exists

            var idx = this.selectedFacets.indexOf(result);

            if (result.values.length <= 1 || result.type === "daterange") {
                this.selectedFacets.splice(idx, 1);
            }
            else {
                // Check if the value is already present in that facet or not
                var valueResult = this.selectedFacets[idx].values.filter(function (f) { return f.value === value; })[0];
                idx = result.values.indexOf(valueResult);
                result.values.splice(idx, 1);
            }
        }

        if (search) {
            Microsoft.Search.ReSearch();
        }
    },

    RenderWebFacets: function () {
        this.RenderStaticFacets(this.static_facets, false);
    },

    RenderStaticFacets: function (facets = this.static_facets, only_facets_with_target = false, alwaysOpen = false, tagid = this.STATIC_FACET_DEFAULT_TAGID) {
        this.RenderFacets(facets, only_facets_with_target, alwaysOpen, tagid, "static");
    },

    RenderFacets: function (facets = this.facets, only_facets_with_target = false, keepOpen = false, tagid = this.FACET_DEFAULT_TAGID, rendering_type = "dynamic", overwrite_mode = true) {

        // Clear Dynamic facets configured outside the Other Refiners section in the Navigation
        // same as clear not seen facets.
        for (var i = 0; i < this.static_facets.length; i++) {
            var facet = this.static_facets[i]; 
            if (facet.type === 'dynamic') {
                var nameid = Microsoft.Utils.jqid(facet.target);
                // Header button
                var facetButton = Microsoft.Facets.GetFacetAccordionHeaderButtonId(nameid);
                $('#'+facetButton).removeClass('text-danger');
                // Hide it                
                var facet_id_tag = nameid + "-accordion-item";
                $('#' + facet_id_tag).addClass("d-none");
            }
        }

        if (overwrite_mode) {
            $(tagid).empty();
        }

        var facetResultsHTML = '<div class="accordion accordion-flush" id="' + rendering_type + '-facets-accordion">';

        if (facets) {

            var ordered_keys = Object.keys(facets).sort();

            ordered_keys.forEach((item, idx) => {
                if (rendering_type === "static") {
                    var nameid = Microsoft.Utils.jqid(facets[idx].type === "static" ? facets[idx].id : facets[idx].target);
                    var name = facets[idx].name;
                    var data = facets[idx].values
                }
                else {
                    var nameid = Microsoft.Utils.jqid(item);
                    var name = item;
                    var data = facets[item]
                }

                var title = Microsoft.Utils.GetFacetDisplayTitle(name)

                if ((rendering_type === "static") && only_facets_with_target && !facets[item].target) {
                    return;
                }

                var facetId = this.GetFacetAccordionHeaderId(nameid);
                var facetButton = this.GetFacetAccordionHeaderButtonId(nameid);
                var facet_id_tag = this.GetFacetAccordionItemId(nameid);

                var facet_html = '';

                if (rendering_type === "static" && facets[idx].type === "dynamic") {
                    facet_html += '<div class="accordion-item d-none" id="' + facet_id_tag + '">';
                }
                else {
                    facet_html += '<div class="accordion-item" id="' + facet_id_tag + '">';
                }

                facet_html += '<h2 class="accordion-header accordion-item-' + nameid + '" id="' + facetId + '">';

                facet_html += '<button id="'+facetButton+'" class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#' + nameid + '" aria-expanded="true" aria-controls="' + nameid + '">';

                facet_html += title;
                facet_html += '</button>';
                facet_html += '</h2>';

                if (keepOpen) {
                    facet_html += '<div id="' + nameid + '" class="accordion-collapse " aria-labelledby="' + facetId + '" data-bs-parent="#' + rendering_type + '-facets-accordion">';
                }
                else {
                    facet_html += '<div id="' + nameid + '" class="accordion-collapse collapse" role="group" aria-labelledby="' + facetId + '" data-bs-parent="#' + rendering_type + '-facets-accordion">';
                }

                facet_html += '<div id="' + nameid + '-accordion-body" class="accordion-body">';

                // Facet Body
                var facet_body_tag = "#" + nameid + "-accordion-body";
                var facet_body_html = '';

                var facet_target = rendering_type === "static" ? facets[item].target : name
                var facet_key = rendering_type === "static" ? facets[item].id : name

                if (data) {
                    for (var j = 0; j < data.length; j++) {
                        facet_body_html += '<div class="form-check">';
                        var facet_value = this.EncodeFacetValue(data[j].value);
                        var clickMethod = null;

                        // Support for target on specific values
                        if (data[j].target) {
                            clickMethod = 'Microsoft.Facets.ChooseFacet(\'' + facet_key + '\',\'' + facet_value + '\', \'' + j + '\',\'' + rendering_type + '\');';
                        }
                        else {
                            if (facet_target) {
                                clickMethod = 'Microsoft.Facets.ChooseFacet(\'' + facet_key + '\',\'' + facet_value + '\', \'' + j + '\',\'' + rendering_type + '\',\'' + facet_target + '\');';
                            }
                            else {
                                // the facet query
                                clickMethod = 'Microsoft.Facets.ChooseFacet(\'' + facet_key + '\',\'' + facet_value + '\', \'' + j + '\',\'' + rendering_type + '\');';
                            }
                        }

                        facet_body_html += '   <input class="form-check-input facet-checkbox" type="checkbox"  id="' + nameid + '_' + facet_value.replaceAll("=", "") + '" onclick="' + clickMethod + '">';
                        facet_body_html += '   <label class="form-check-label "for="' + nameid + '_' + facet_value.replaceAll("=", "") + '" >';
                        if (data[j].count) {
                            facet_body_html += '   <span>' + data[j].value + ' (' + data[j].count + ')</span> ';
                        }
                        else {
                            facet_body_html += '   <span>' + data[j].value + '</span> ';
                        }
                        facet_body_html += '   </label>';
                        facet_body_html += '</div>';
                    }
                }
                facet_html += facet_body_html;

                facet_html += '</div>';
                facet_html += '</div></div>';

                // In the case of dynamic filter configured outside the Other Refiners section in the Navigation               
                if ($(facet_body_tag).length) {
                    $('#' + facet_id_tag).removeClass("d-none");
                    $(facet_body_tag).html(facet_body_html);
                }
                else {
                    facetResultsHTML += facet_html;
                }
            });
        }

        facetResultsHTML += '</div>';

        $(tagid).append(facetResultsHTML);

    },

    EncodeFacetValue: function (value, padding=false) {
        return Base64.encode(value, padding);
    },

    ChooseFacetWithQueryAll: function (facet_key, value) {
        this.ChooseFacet(facet_key, value, null, "dynamic", null, true, '')
    },
    
    ChooseFacet: function (facet_key, value, valueIdx, facet_type = "dynamic", target = null, search = true, query = null) {

        value = Base64.decode(value);

        var facet_cfg = this.get_static_facet_by_id(facet_key);        
        var facet_source = facet_type === "static" ? facet_cfg.values : this.facets[facet_key];

        if (this.selectedFacets !== undefined) {

            // facetValues where key == selected facet
            var result = this.selectedFacets.filter(function (f) { return f.key === facet_key; })[0];

            if (result) { // if that facet exists
                var idx = this.selectedFacets.indexOf(result);

                // Check if the value is already present in that facet or not
                var valueResult = result.values.filter(function (f) { return f.value === value; })[0];
                //if (!result.values.includes(value)) {
                if (!valueResult) {
                    // Checking event
                    if (valueIdx) {
                        result.values.push(facet_source[valueIdx]);
                    }
                    else {
                        result.values.push({ value: value });
                    }
                    Microsoft.Facets.selectedFacets[idx] = result;
                }
                else {
                    // Unchecking event - Remove when last value
                    if (result.values.length <= 1) {
                        Microsoft.Facets.selectedFacets.splice(idx, 1);

                        if (facet_type === "static") {
                            var facetButton = Microsoft.Facets.GetFacetAccordionHeaderButtonId(Microsoft.Utils.jqid(facet_key));
                            $('#'+facetButton).removeClass('text-danger');
                        }
                    }
                    else {
                        // Remove facet value entry
                        valueIdx = result.values.indexOf(valueResult);

                        result.values.splice(valueIdx, 1);
                    }
                }
            }
            else {

                Microsoft.Facets.selectedFacets.push({
                    key: facet_key,
                    values: valueIdx ? [facet_source[valueIdx]] : [{ value: value }],
                    type: facet_type,
                    operator: facet_cfg?.operator ? facet_cfg.operator : null,
                    target: target ? target : null
                });
            }
        }

        if (search) {
            Microsoft.Search.ReSearch(query);
        }
    }
}

// export default Microsoft.Facets;