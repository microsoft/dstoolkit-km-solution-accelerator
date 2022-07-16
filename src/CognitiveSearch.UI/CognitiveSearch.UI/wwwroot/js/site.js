// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

var Microsoft = Microsoft || {};

// Global functions 
$.ajaxSetup({
    cache: false
});

jQuery["getJSON"] = function (url, data, callback) {
    return jQuery.ajax({
        url: url,
        type: "GET",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        data: JSON.stringify(data),
        success: callback
    });
};

jQuery["postJSON"] = function (url, data, callback) {
    return jQuery.ajax({
        url: url,
        type: "POST",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        data: JSON.stringify(data),
        success: callback
    });
};

jQuery["postXML"] = function (url, data, callback) {
    return jQuery.ajax({
        url: url,
        type: "POST",
        contentType: "application/json; charset=utf-8",
        dataType: "xml",
        data: JSON.stringify(data),
        success: callback
    });
};
