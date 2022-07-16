// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

//
// Maps
//
Microsoft.Maps = Microsoft.Maps || {};
Microsoft.Maps = {

    // Variables
    map: null,
    baseLayer: null,
    controls: [],
    showMap: true,
    drawingManager: null,
    popup: null,
    mapEvents: ['boxzoomend', 'boxzoomstart', 'click', 'contextmenu', 'data', 'dblclick', 'drag', 'dragend', 'dragstart', 'error', 'idle', 'layeradded', 'layerremoved', 'load', 'mousedown', 'mouseenter', 'mouseleave', 'mousemove', 'mouseout', 'mouseover', 'mouseup', 'move', 'moveend', 'movestart', 'pitch', 'pitchend', 'pitchstart', 'ready', 'render', 'resize', 'rotate', 'rotateend', 'rotatestart', 'sourceadded', 'sourcedata', 'sourceremoved', 'styledata', 'styleimagemissing', 'tokenacquired', 'touchcancel', 'touchend', 'touchmove', 'touchstart', 'wheel', 'zoom', 'zoomend', 'zoomstart'],
    symbolLayerEvents: ['mousedown', 'mouseup', 'mouseover', 'mousemove', 'click', 'dblclick', 'mouseout', 'mouseenter', 'mouseleave', 'contextmenu', 'touchstart', 'touchend', 'touchmove', 'touchcancel', 'wheel'],
    countriesDataSource: null,
    defaultColor: '#ACFF88',
    colorScale: [
        5, '#09e076',
        //10, '#0bbf67',
        10, '#07af5c',
        15, '#f7e305',
        20, '#f7c707',
        25, '#f78205',
        30, '#f75e05',
        35, '#de2104'
    ],
    // Add more geo-based facets here. Default is countries.
    geofacets: ["countries"],

    datasources: {},
    layers: [],
    mapcountries: [],
    init: function () {
        return new Promise((resolve, reject) => {
            $.ajax({
                type: 'GET',
                url: '/config/maps.json',
                dataType: 'json',
                success: function (data) {
                    Microsoft.Maps.layers = data;
                    resolve()
                },
                error: function (error) {
                    reject(error)
                },
            })
        });
    },
    loadCountries: function () {
        $.getJSON("/data/countries-list.json", null, function (data) {
            Microsoft.Maps.mapcountries = data.list;
        });
    },
    ClearDataSources: function () {
        for (var i = 0; i < this.layers.length; i++) {
            var layer = this.layers[i];
            if (layer.clearable) {
                if (this.datasources[layer.id] !== undefined) {
                    this.datasources[layer.id].clear();
                }
            }
        }
    },
    hasDataSources: function () {
        return (Object.keys(this.datasources).length > 0);
    },

    initMap: function () {

        this.countriesDataSource = new atlas.source.DataSource("countries");
        //Load a dataset of polygons that have metadata we can style against.
        this.countriesDataSource.importDataFromUrl('/data/countries-fc.json');
    },

    WorldMapSearch: function (query) {

        Microsoft.Search.setQueryInProgress();

        if (query !== undefined && query !== null) {
            $("#q").val(query)
        }

        Microsoft.View.currentQuery = $("#q").val();

        if (this.map === null || typeof this.map === 'undefined') {
            // Create the map
            this.AuthenticateResultsMap();
        }
        else {
            this.AddMapPoints();
        }

        Microsoft.Search.setQueryCompleted();
    },

    //  Authenticates the map and shows some locations.
    AuthenticateResultsMap: function () {
        $.post('/api/map/getmapcredentials', {},
            function (data) {

                if (data.mapKey === null || data.mapKey === "") {
                    Microsoft.Maps.showMap = false;
                    return;
                }

                // default map coordinates
                var coordinates = [-122.32, 47.60];

                // Authenticate the map using the key 
                Microsoft.Maps.map = new atlas.Map('WorldMap', {
                    center: coordinates,
                    zoom: 4,
                    visibility: "visible",
                    showBuildingModels: true,
                    enableAccessibility: true,
                    language: 'en-US',
                    view: 'Auto',
                    authOptions: {
                        authType: 'subscriptionKey',
                        subscriptionKey: data.mapKey
                    }
                });

                //Wait until the map resources are ready.
                Microsoft.Maps.map.events.add('ready', function () {

                    Microsoft.Maps.AddNavigationControls();

                    Microsoft.Maps.AddSelectionControls();

                    Microsoft.Maps.map.controls.add([
                        //Add the custom control to the map.
                        new atlas.control.FullscreenControl({
                            style: 'auto'
                        })
                    ], {
                        position: 'top-right'
                    });

                    // Redraw the map to fix a scaling issue.
                    Microsoft.Maps.map.resize();

                });

                Microsoft.Maps.AddMapPoints();

                return;
            });
        return;
    },

    AddNavigationControls: function () {
        //Remove all controls on the map.
        Microsoft.Maps.map.controls.remove(this.controls);

        this.controls = [
            new atlas.control.ZoomControl({
                zoomDelta: 1
            }),
            new atlas.control.PitchControl({
                pitchDegreesDelta: 10
            }),
            new atlas.control.CompassControl({
                rotationDegreesDelta: 10
            }),
            new atlas.control.TrafficControl(),
            new atlas.control.StyleControl({
                layout: "icons",
                autoSelectionMode: true,
                mapStyles: ['road', 'road_shaded_relief', 'grayscale_light', 'night', 'grayscale_dark', 'satellite', 'satellite_road_labels', 'high_contrast_dark']
            })
        ];

        //Get input options.
        var positionOption = "bottom-right";

        //Add controls to the map.
        Microsoft.Maps.map.controls.add(this.controls, {
            position: positionOption
        });

        Microsoft.Maps.map.controls.add(new atlas.control.TrafficLegendControl(), {
            position: 'bottom-left'
        });
    },

    AddSelectionControls: function () {

        //Create an instance of the drawing manager and display the drawing toolbar.
        this.drawingManager = new atlas.drawing.DrawingManager(Microsoft.Maps.map, {
            toolbar: new atlas.control.DrawingToolbar({ position: 'top-right', style: 'light' })
        });

        //Hide the polygon fill area as only want to show outline of search area.
        this.drawingManager.getLayers().polygonLayer.setOptions({ visible: false });

        //Clear the map and drawing canvas when the user enters into a drawing mode.
        this.map.events.add('drawingmodechanged', this.drawingManager, Microsoft.Maps.drawingModeChanged);

        //Monitor for when a polygon drawing has been completed.
        this.map.events.add('drawingcomplete', this.drawingManager, Microsoft.Maps.searchPolygon);

    },

    drawingModeChanged: function (mode) {
        //Clear the drawing canvas when the user enters into a drawing mode.
        if (mode.startsWith('draw')) {
            Microsoft.Maps.popup.close();

            Microsoft.Maps.ClearAllSources();

            Microsoft.Maps.drawingManager.getSource().clear();
        }
    },

    searchPolygon: function (searchArea) {
        //Exit drawing mode.
        Microsoft.Maps.drawingManager.setOptions({ mode: 'idle' });

        ////Get the POI query value. 
        //var query = document.getElementById('queryTbx').value;

        //If the search area is a circle, use its center and radius to search.
        if (searchArea.isCircle()) {
            var center = searchArea.getCoordinates();

            alert(' Radius search');

            //    //Search for POI's within a radius.
            //    searchURL.searchPOI(atlas.service.Aborter.timeout(3000), query, {
            //        lon: center[0],
            //        lat: center[1],
            //        radius: searchArea.getProperties().radius,
            //        limit: 100,
            //        view: 'Auto'
            //    }).then(showResults, error => {
            //        alert(error);
            //    });
        } else {
            var body = searchArea.toJson();
            alert('Polygon search');

            //    //Search for points of interest inside the search area.
            //    searchURL.searchInsideGeometry(atlas.service.Aborter.timeout(3000), query, body, {
            //        limit: 100,
            //        view: 'Auto'
            //    }).then(showResults, error => {
            //        alert(error);
            //    });
        }
    },

    createLayersPanel: function () {

        var html = [];

        html.push('<div class="d-flex">');
        html.push(' <legend><span class="bi bi-pin-map-fill"></span> Map Layers</legend>');
        html.push(' <button type="button" class="btn btn-sm btn-outline-dark mt-0 mb-2 ms-1 me-1 bi bi-arrows-expand" onclick="Microsoft.Utils.toggleDiv(\'hideable-map-layers-panel\');" data-bs-toggle="button" title="Hide/Expand the layers details"></button>');
        html.push('</div>');

        html.push('<div id="hideable-map-layers-panel">');

        for (var i = 0; i < Microsoft.Maps.layers.length; i++) {
            var layer = Microsoft.Maps.layers[i];

            if (layer.enabled) {
                html.push('<div id="', layer.id, '">');

                html.push('<div class="form-check">');
                html.push('<input class="form-check-input" type="checkbox" value="" id="', layer.id, '-checkbox" onclick="Microsoft.Maps.viewLayer(\'', layer.id, '-checkbox\',\'', layer.id, '\');"');
                if (layer.visible) {
                    html.push('checked ');
                }
                //if (layer.notStyled) {
                //    html.push(' disabled ');        
                //}
                html.push('>');
                html.push('<label class="form-check-label" for="', layer.id, '-checkbox"><strong>', layer.name, '<span id="', layer.id, '-count"></span></strong></label>');

                html.push('<div id="legend" class="d-flex">');
                // Load Layer Legends here
                if (layer.type === "FacetExtrusionLayer") {
                    html.push('<i style="background:', this.defaultColor, '"></i> 0-', this.colorScale[0], '<br/>');

                    for (var j = 0; j < this.colorScale.length; j += 2) {
                        html.push(
                            '<i style="background:', (this.colorScale[j + 1]), '"></i> ',
                            this.colorScale[j], (this.colorScale[j + 2] ? '&ndash;' + this.colorScale[j + 2] + '<br/>' : '+')
                        );
                    }
                }
                html.push('</div>');
                html.push('</div>');

                html.push('</div>');
            }
        }

        html.push('</div>');

        document.getElementById('map-layers').innerHTML += html.join('');
    },

    GeneratePopupContent: function (shapes) {

        var indicators = [];
        var items = [];

        for (var i = 0; i < shapes.length; i++) {
            var properties = shapes[i].getProperties();

            var image = properties.image;

            var path = image.metadata_storage_path;
            var pathLower = path.toLowerCase();

            if (i === 0) {
                indicators.push('<button type="button" data-bs-target="#carouselExampleCaptions" data-bs-slide-to="0" class="active" aria-current="true" aria-label="Slide 1"></button>');
                items.push('<div class="carousel-item active">');
            } else {
                indicators.push('<button type="button" data-bs-target="#carouselExampleCaptions" data-bs-slide-to="1" aria-label="Slide 2"></button>');
                items.push('<div class="carousel-item">');
            }

            items.push('<center>');
            items.push('<h6>' + properties.title + '</h6>');
            items.push('<h7><strong>' + Microsoft.Utils.GetImageFileTitle(image) + '</strong></h7>');
            items.push('</center>');

            if (image.document_embedded) {
                items.push('<span class="d-inline-block text-truncate" style="max-width: 250px;">' + Base64.decode(image.image_parentfilename) + '</span>');
                items.push('<div class="image-result-img d-block w-100" onclick="Microsoft.Results.Details.ShowDocument(\'' + image.index_key + '\');">');
                items.push('<img class="image-result-map" src="data:image/png;base64, ' + image.image.thumbnail_medium + '" title="' + Base64.decode(image.image_parentfilename) + '" />');
                items.push('</div>');
            }
            else {
                items.push('<span class="d-inline-block text-truncate" style="max-width: 250px;">' + image.metadata_storage_name + '</span>');
                items.push('<div class="image-result-img d-block w-100" onclick="Microsoft.Results.Details.ShowDocument(\'' + image.index_key + '\');">');
                items.push('<img class="image-result-map" src="data:image/png;base64, ' + image.image.thumbnail_medium + '" title="' + image.metadata_storage_name + '" />');
                items.push('</div>');
            }

            items.push('</div>');
        }

        var popupTemplate = '<div class="customInfobox">';
        popupTemplate += '<div id="carouselExampleCaptions" class="carousel slide carousel-fade" data-bs-ride="carousel">';
        //popupTemplate += '<div class="carousel-indicators">';
        //popupTemplate+=indicators.join('');
        //popupTemplate+='</div>';
        popupTemplate += '<div class="carousel-inner">';
        popupTemplate += items.join('');
        popupTemplate += '</div>';

        // Add controls 
        if (shapes.length > 1) {
            // Prev button
            popupTemplate += '<button class="carousel-control-prev" type="button" data-bs-target="#carouselExampleCaptions" data-bs-slide="prev">';
            popupTemplate += '<span class="carousel-control-prev-icon bg-danger" aria-hidden="true"></span>';
            popupTemplate += '<span class="visually-hidden">Previous</span>';
            popupTemplate += '</button>';
            // Next button
            popupTemplate += '<button class="carousel-control-next" type="button" data-bs-target="#carouselExampleCaptions" data-bs-slide="next">';
            popupTemplate += '<span class="carousel-control-next-icon bg-danger" aria-hidden="true"></span>';
            popupTemplate += '<span class="visually-hidden">Next</span>';
            popupTemplate += '</button>';
        }
        popupTemplate += '</div>';

        popupTemplate += '</div>';

        return popupTemplate;
    },

    ClearCountriesDataSource: function () {
        for (var i = 0; i < this.countriesDataSource.shapes.length; i++) {
            this.countriesDataSource.shapes[i].data.properties.count = 0;
        }
    },

    ClearAllSources: function () {
        this.ClearDataSources();

        this.ClearCountriesDataSource();
    },

    AddDataToSources: function () {
        for (var i = 0; i < Microsoft.Maps.layers.length; i++) {
            var layer = Microsoft.Maps.layers[i];
            if (layer.enabled) {
                if (layer.dataSourceMethod) {
                    // Add the data to the sources
                    Microsoft.Utils.executeFunctionByName(layer.dataSourceMethod, window, Microsoft.Maps.datasources[layer.id]);
                }
            }
        }
    },

    // Adds map points and re-centers the map based on results
    AddMapPoints: function () {
        var coordinates;

        if (Microsoft.Maps.hasDataSources()) {

            Microsoft.Maps.ClearAllSources();

            Microsoft.Maps.AddDataToSources();

            if (coordinates) {
                this.map.setCamera({ center: coordinates });
            }
        }
        else {

            //Wait until the map resources are ready for first set up.
            Microsoft.Maps.map.events.add('ready', function () {

                // Add all configured data sources
                for (var i = 0; i < Microsoft.Maps.layers.length; i++) {
                    var layer = Microsoft.Maps.layers[i];
                    if (layer.enabled) {
                        var newsource = new atlas.source.DataSource(layer.id, layer.sourceOptions);
                        Microsoft.Maps.datasources[layer.id] = newsource;

                        if (layer.dataSourceMethod) {
                            // Add the data to the sources
                            Microsoft.Utils.executeFunctionByName(layer.dataSourceMethod, window, Microsoft.Maps.datasources[layer.id]);
                        }

                        Microsoft.Maps.map.sources.add(newsource);
                    }
                }

                //take the last coordinates.
                if (coordinates) { Microsoft.Maps.map.setCamera({ center: coordinates }); }

                //Create a legend.
                Microsoft.Maps.createLayersPanel();

                //Create a popup but leave it closed so we can update it and display it later.
                Microsoft.Maps.popup = new atlas.Popup({
                    pixelOffset: [0, -18],
                    closeButton: true
                });

                // PRE-DEFINED LAYERS
                for (var i = 0; i < Microsoft.Maps.layers.length; i++) {
                    var layer = Microsoft.Maps.layers[i];
                    if (layer.enabled) {
                        switch (layer.type) {
                            case "SearchResultsLayer":

                                var clusterLayer = new atlas.layer.BubbleLayer(
                                    Microsoft.Maps.datasources[layer.id],
                                    layer.id + "_cluster",
                                    {
                                        source: layer.id,
                                        radius: layer.radius ? layer.radius : 14,
                                        color: layer.color ? layer.color : "#8B0000",
                                        strokeColor: layer.strokeColor ? layer.strokeColor : "white",
                                        strokeWidth: layer.strokeWidth ? layer.strokeWidth : 1,
                                        blur: layer.blur ? layer.blur : 0,
                                        opacity: layer.opacity ? layer.opacity : 0,
                                        filter: ['has', 'point_count'], //Filter individual points from this layer.
                                    }
                                );
                                Microsoft.Maps.map.events.add('click', clusterLayer, Microsoft.Maps.clusterClicked);
                                Microsoft.Maps.map.layers.add(clusterLayer);

                                //Add a layer for rendering data point count.
                                Microsoft.Maps.map.layers.add(new atlas.layer.SymbolLayer(Microsoft.Maps.datasources[layer.id], null, {
                                    iconOptions: {
                                        //Hide the icon image.
                                        image: "none"
                                    },
                                    filter: ['has', 'point_count'], //Filter individual points from this layer.
                                    textOptions: {
                                        textField: ['get', 'point_count'],
                                        font: ['SegoeUi-Bold'],
                                        color: layer.textcolor ? layer.textcolor : "#FFFFFF",
                                        offset: layer.textoffset ? layer.textoffset : [0, 0]
                                    },
                                }));

                                //Create a layer to render the individual locations (not clustered)
                                var symbolLayer = new atlas.layer.SymbolLayer(Microsoft.Maps.datasources[layer.id], layer.id, {
                                    filter: ['!', ['has', 'point_count']], //Filter out clustered points from this layer.
                                    iconOptions: {
                                        image: layer.iconmarker ? layer.iconmarker : "marker-blue",
                                        offset: layer.iconoffset ? layer.iconoffset : [-5, -5],
                                        anchor: layer.iconanchor ? layer.iconanchor : "center"
                                    }
                                })
                                Microsoft.Maps.map.events.add('click', symbolLayer, Microsoft.Maps.mapClick);
                                Microsoft.Maps.map.layers.add(symbolLayer);

                                layer["maplayerid"] = [clusterLayer.id, symbolLayer.id];

                                break;

                            case "FacetExtrusionLayer":
                                //Create a stepped expression based on the color scale.
                                var steppedExp = [
                                    'step',
                                    ['get', 'count'],
                                    Microsoft.Maps.defaultColor
                                ];
                                steppedExp = steppedExp.concat(Microsoft.Maps.colorScale);

                                var templayer = new atlas.layer.PolygonExtrusionLayer(Microsoft.Maps.datasources[layer.id], layer.id, {
                                    base: 1000,
                                    fillColor: steppedExp,
                                    fillOpacity: 0.7,
                                    //    height: [
                                    //        'interpolate',
                                    //        ['linear'],
                                    //        ['get', 'count'],
                                    //        0, 50,
                                    //        5, 75,
                                    //        10, 100,
                                    //        15, 125,
                                    //        20, 150,
                                    //        25, 175
                                    //    ]
                                    height: 2000,
                                    visible: layer.visible
                                    //}));
                                });
                                //Create and add a polygon extrusion layer to the map below the labels so that they are still readable.
                                if (layer.level) {
                                    Microsoft.Maps.map.layers.add(templayer, layer.level);
                                }
                                else {
                                    Microsoft.Maps.map.layers.add(templayer);
                                }
                                //map.events.add('click', templayer, mapClick);
                                layer["maplayerid"] = [templayer.id];
                                break;

                            case "OgcMapLayer":
                                var templayer = new atlas.layer.OgcMapLayer({
                                    url: layer.url,
                                    activeLayers: layer.activeLayers, //Optionally specify the layer to render. If not specified, first layer listed in capabilities will be rendered.
                                    styles: layer.styles,
                                    bringIntoView: true, //Optionally have the focus the map over where the layer is available.
                                    visible: layer.visible,
                                    debug: true
                                });

                                layer["maplayerid"] = [templayer.id];

                                //Add a click event to the map.
                                //map.events.add('click', templayer, mapClick);
                                Microsoft.Maps.map.events.add('click', Microsoft.Maps.mapClick);
                                //Add mouse events to the layer to show/hide a popup when hovering over a marker.
                                //map.events.add('mouseover', templayer, mapClick);
                                //map.events.add('mouseout', templayer, mapClick);

                                if (layer.level) {
                                    Microsoft.Maps.map.layers.add(templayer, layer.level);
                                }
                                else {
                                    Microsoft.Maps.map.layers.add(templayer);
                                }

                                break;
                        }
                    }
                }
            });

            // This is necessary for the map to resize correctly after the 
            // map is actually in view.
            $('#maps-pivot-link').on("click", function () {
                window.setTimeout(function () {
                    Microsoft.Maps.map.map.resize();
                }, 100);
            });
        }
    },

    viewLayer: function (checkboxid, layerid) {
        // Get the checkbox
        var checkBox = document.getElementById(checkboxid);

        for (var i = 0; i < Microsoft.Maps.layers.length; i++) {
            var layer = Microsoft.Maps.layers[i];
            if (layerid === layer.id) {
                for (var j = 0; j < layer.maplayerid.length; j++) {
                    var id = layer.maplayerid[j];
                    var reslayer = Microsoft.Maps.map.layers.getLayerById(id);
                    if (reslayer) {
                        var options = reslayer.getOptions();
                        options.visible = checkBox.checked;
                        reslayer.setOptions(options);
                    }
                }
                break;
            }
        }
    },

    //Handle click events on the map.
    mapClick: function (e) {
        //Make sure that the point exists.
        if (e.shapes && e.shapes.length > 0) {
            var content;

            if (e.layerId === "images") {
                content = Microsoft.Maps.GeneratePopupContent([e.shapes[0]]);
            }
            else {

                if (e.shapes[0].properties && Object.keys(e.shapes[0].properties).length > 0) {
                    content = atlas.PopupTemplate.applyTemplate(e.shapes[0].properties)
                }
                else {
                    return;
                }
            }

            Microsoft.Maps.popup.setOptions({
                //Update the content of the popup.
                content: content,
                //Update the popup's position with the symbol's coordinate.
                position: e.position
            });

            if (Microsoft.Maps.popup.isOpen() !== true) {
                //Open the popup.
                Microsoft.Maps.popup.open(Microsoft.Maps.map);
            }
            //    else {
            //        popup.close();
            //        popup.open(map);
            //    }
        }
    },

    clusterClicked: function (e) {
        if (e && e.shapes && e.shapes.length > 0 && e.shapes[0].properties.cluster) {
            //Get the clustered point from the event.
            var cluster = e.shapes[0];
            var datasource = Microsoft.Maps.datasources[cluster.layer.source];
            ////If there are more than 10 points in the cluster, zoom the map in. 
            //if (cluster.properties.point_count > 10) {

            //    //Get the cluster expansion zoom level. This is the zoom level at which the cluster starts to break apart.
            //    mapDataSource.getClusterExpansionZoom(cluster.properties.cluster_id).then(function (zoom) {

            //        //Update the map camera to be centered over the cluster. 
            //        map.setCamera({
            //            center: cluster.geometry.coordinates,
            //            zoom: zoom,
            //            type: 'ease',
            //            duration: 200
            //        });
            //    });
            //} else {
            //If there are 10 or less points in a cluster, display a popup with a list.

            //Get all points in the cluster. Set the offset to 0 and the limit to Infinity to return all points.
            datasource.getClusterLeaves(cluster.properties.cluster_id, Infinity, 0).then(function (points) {

                var html = ['<div style="padding:10px;">', points[0].getProperties().title, '<br/><ul>'];

                //Create a list of links for each point. Use one of the properties as the display text. Pass the ID of each shape into a function when clicked.
                for (var i = 0; i < points.length; i++) {
                    html.push('<li><a href="javascript:void(0)" onclick="Microsoft.Maps.shapeSelected(\'', cluster.layer.source, '\',\'', points[i].getId(), '\')">', points[i].getProperties().title, '</a></li>');
                }

                html.push('</ul></div>');

                var content = Microsoft.Maps.GeneratePopupContent(points);
                //Update the content and position of the popup.
                Microsoft.Maps.popup.setOptions({
                    content: content,
                    position: cluster.geometry.coordinates,
                    pixelOffset: [0, -18]
                });

                //Open the popup.
                Microsoft.Maps.popup.open(Microsoft.Maps.map);
            });
            //}
        }
    },

    shapeSelected: function (source, id) {
        var datasource = Microsoft.Maps.datasources[source];

        //Get the shape from the data source by ID.
        var shape = datasource.getShapeById(id);

        Microsoft.Results.Details.ShowDocumentById(shape.data.properties.image.index_key);

        //    //Update the content of the popup with the details of the selected point.
        //    popup.setOptions({
        //        content: atlas.PopupTemplate.applyTemplate(shape.getProperties())
        //    });
    },


    FetchImages: function (targetDataSource) {

        $.postJSON('/api/search/getimages',
            {
                queryText: Microsoft.View.currentQuery !== undefined ? Microsoft.View.currentQuery : "*",
                searchFacets: Microsoft.Facets.selectedFacets,
                parameters: Microsoft.Search.Parameters,
                options: Microsoft.Search.Options
            },
            function (data) {

                Microsoft.Search.ProcessSearchResponse(data); 

                var coordinates;

                for (var i = 0; i < Microsoft.Search.results.length; i++) {

                    var image = Microsoft.Search.results[i].Document !== undefined ? Microsoft.Search.results[i].Document : Microsoft.Search.results[i];

                    if (image.locations) {
                        coordinates = Microsoft.Maps.addImageLocationsToMap(targetDataSource, image.locations, image);
                    }
                    if (image.countries) {
                        coordinates = Microsoft.Maps.addImageLocationsToMap(targetDataSource, image.countries, image);
                    }
                    if (image.capitals) {
                        coordinates = Microsoft.Maps.addImageLocationsToMap(targetDataSource, image.capitals, image);
                    }
                    if (image.cities) {
                        coordinates = Microsoft.Maps.addImageLocationsToMap(targetDataSource, image.cities, image);
                    }
                    if (image.landmarks) {
                        coordinates = Microsoft.Maps.addImageLocationsToMap(targetDataSource, image.landmarks, image);
                    }
                }

                $("#" + targetDataSource.id + "-count").html(" (" + data.count + ")");
                // Special case that documents datasource is feeding the facets extrusion datasource as well.
                Microsoft.Maps.UpdateFacetsExtrusions(data.facets, Microsoft.Maps.datasources["images_facets"]);

                return coordinates;
            });
    },

    UpdateFacetsExtrusions: function (facets, targetDataSource) {
        if (facets) {
            for (var item in facets) {
                if (this.geofacets.includes(item)) {
                    var data = facets[item];
                    if (data !== null) {
                        for (var j = 0; j < data.length; j++) {
                            // data[j].value / data[j].count 
                            for (var i = 0; i < this.countriesDataSource.shapes.length; i++) {
                                if (this.countriesDataSource.shapes[i].data.properties.name === data[j].value) {
                                    this.countriesDataSource.shapes[i].data.properties.count += data[j].count;
                                }
                            }
                        }
                    }
                }
            }

            // Copy countries shapes with count >0 
            for (var i = 0; i < this.countriesDataSource.shapes.length; i++) {
                if (this.countriesDataSource.shapes[i].data.properties.count > 0) {
                    targetDataSource.add(this.countriesDataSource.shapes[i]);
                }
            }
        }
    },

    addImageLocationsToMap: function (targetDataSource, locations, image) {
        //var pushpin, pins = [], locs = [], output = '';
        var coordinates;

        for (var i = 0; i < locations.length; i++) {

            coordinates = this.getLocation(locations[i]);

            if (coordinates) {
                if (coordinates !== null && typeof coordinates !== 'undefined') {
                    //Add the symbol to the data source.
                    targetDataSource.add(new atlas.data.Feature(new atlas.data.Point(coordinates), {
                        id: image.index_key,
                        image: image,
                        title: locations[i],
                    }));
                }
            }
        }

        return coordinates;
    },

    // This should be in the backend
    getLocation: function (name) {
        for (var i = 0; i < Microsoft.Maps.mapcountries.length; i++) {
            if (name && Microsoft.Maps.mapcountries[i].Name) {
                if (Microsoft.Maps.mapcountries[i].Name.toUpperCase() === name.toUpperCase()) {
                    return [Microsoft.Maps.mapcountries[i].Longitude, Microsoft.Maps.mapcountries[i].Latitude];
                }
            }
        }
    }
}

Microsoft.Maps.init().then(() => {
    Microsoft.Maps.loadCountries();
});

// export default Microsoft.Maps;