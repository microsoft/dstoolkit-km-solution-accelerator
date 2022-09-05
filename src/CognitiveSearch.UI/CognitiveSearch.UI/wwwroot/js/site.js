// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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

// Web API Backend Support

jQuery["postAPIJSON"] = function (url, data, callback) {
    return createJQueryRequest(url, data, callback, 'POST', 'json');
};

jQuery["postAPIXML"] = function (url, data, callback) {
    return createJQueryRequest(url, data, callback, 'POST', 'xml');
};

function createJQueryRequest(url, data, callback, method, dataType) {

    if (Microsoft.Config.data.webAPIBackend.isEnabled) {
        // Append the API Backend host here
        var backendurl = Microsoft.Config.data.webAPIBackend.endpoint + url;

        if (Microsoft.Config.data.webAPIBackend.isAuthenticated) {
            return jQuery.ajax({
                url: backendurl,
                type: method,
                crossDomain: true,
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                data: JSON.stringify(data),
                beforeSend: function (xhr) {
                    /* Authorization header */
                    xhr.setRequestHeader("Authorization", "Bearer " + getAccessToken());
                },
                success: callback
            });
        }
        else {
            return jQuery.ajax({
                url: backendurl,
                type: method,
                crossDomain: true,
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                data: JSON.stringify(data),
                success: callback
            });
        }
    }
    else {
        return jQuery.ajax({
            url: url,
            type: method,
            contentType: "application/json; charset=utf-8",
            dataType: dataType,
            data: JSON.stringify(data),
            success: callback
        });
    }
}


// Get Azure AD EasyAuth Access Token

function getAccessToken() {
    return new Promise((resolve, reject) => {
        $.ajax("/.auth/refresh").done(function () {
            $.ajax({
                type: 'GET',
                url: '/.auth/me',
                dataType: 'json',
                cache: true,
                success: function (data) {
                    token = data[0]["access_token"];
                    resolve(token);
                },
                error: function (error) {
                    reject(error);
                }
            });
        }).fail(function () {
            reject("Token failed to refresh...")
        });
    });
}

