// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

//
// Search Result Quick Actions
//
Microsoft.Search.Actions = Microsoft.Search.Actions || {};
Microsoft.Search.Actions = {
    
    actions: [],

    init: function () {
        return new Promise((resolve, reject) => {
            $.ajax({
                type: 'GET',
                url: '/config/actions.json',
                dataType: 'json',
                success: function (data) {
                    Microsoft.Search.Actions.actions = data;
                    resolve()
                },
                error: function (error) {
                    reject(error)
                },
            })
        });
    },

    hideShowActions: function (id) {
        var tagid = "#actions-" + id;
        if ($(tagid).is(':visible')) {
            $(tagid).css("cssText", "display: none !important;");
        }
        else {
            $(tagid).css("cssText", "display: flex !important;");
        }
    },

    renderActionButton: function (docresult, action, eltClass = '', displayName = true) {
        var htmlDiv = '';
        if (action.enable) {
            var conditionValidated = true;

            if (action.condition) {
                conditionValidated = eval(action.condition);
            }

            if (conditionValidated) {
                // Do something with the parameters
                var parameters = [];

                // Parameter 0 - Action object
                if (action.parameters) {
                    var params = Object.create(action.parameters);
                    for (let index = 0; index < params.length; index++) {
                        const element = Object.create(params[index]);
                        for (var key in element) {
                            element[key] = eval(element[key]);
                        }
                        parameters.push(element);                    
                    }
                }

                var paramBase64Str = Base64.encode(JSON.stringify(parameters));

                htmlDiv += '<button type="button" title="' + action.title + '" class="' + eltClass + ' btn btn-sm ' + action.class + '" onclick="' + action.method + '(\'' + paramBase64Str + '\');">';
                if (action.icon) {
                    if (displayName) {
                        htmlDiv += ' <span class="' + action.icon + ' me-2"></span>'
                    }
                    else {
                        htmlDiv += ' <span class="' + action.icon + '"></span>'
                    }
                }

                if (displayName) {
                    if (action.name) {
                        htmlDiv += '<span>'+action.name+'</span>';
                    }    
                }

                htmlDiv += '</button>'
            }
        }
        return htmlDiv;
    },

    renderActions: function (docresult, staticActions = false, initialStyle = "none") {
        var htmlDiv = ''

        // Actions
        htmlDiv += '<div class="row">';

        if (staticActions) {
            htmlDiv += '<div class="search-result-actions">';
        }
        else {
            htmlDiv += '<div class="search-result-actions" id="actions-' + docresult.index_key + '" style="display:' + initialStyle + ' !important;">';

        }

        htmlDiv += '    <div class="col-md-12" style="padding: 5px;">';
        htmlDiv += '            <div class="d-grid gap-2 d-md-flex" >';
        if (docresult.document_embedded) {
            htmlDiv += '<button onclick="Microsoft.Results.Details.ShowDocumentById(\'' + docresult.image_parentid + '\')" class="btn btn-outline-success btn-sm" ><span class="bi bi-file-earmark" title="Show source document details"></span></button>'
        }

        for (var i = 0; i < this.actions.length; i++) {
            if (this.actions[i].enable) {
                htmlDiv += this.renderActionButton(docresult, this.actions[i],'',false);
            }
        }

        htmlDiv += '            </div>';
        htmlDiv += '    </div>';
        htmlDiv += '</div>';
        htmlDiv += '</div>';

        return htmlDiv;
    },

    renderActionsAsMenu: function (docresult, staticActions = false, initialStyle = "none") {
        var htmlDiv = ''

        // Actions
        htmlDiv += '<div class="dropdown" id="actions-' + docresult.index_key + '">';
        htmlDiv += '<button class="btn btn-sm text-primary" type="button" id="dropdownMenu2" data-bs-toggle="dropdown" aria-expanded="false">';
        htmlDiv += '...';
        htmlDiv += '</button>';

        htmlDiv += '<ul class="dropdown-menu" aria-labelledby="dropdownMenu2">';

        if (docresult.document_embedded) {
            htmlDiv += '<li>';
            htmlDiv += '<button onclick="Microsoft.Results.Details.ShowDocumentById(\'' + docresult.image_parentid + '\')" class="dropdown-item btn btn-outline-success btn-sm">';
            htmlDiv += '<span class="bi bi-file-earmark me-1" title="Show source document details"/>';
            htmlDiv += 'Open Parent';
            htmlDiv += '</button>';
            htmlDiv += '</li>';
        }

        for (var i = 0; i < this.actions.length; i++) {

            if (this.actions[i].enable) {
                htmlDiv += '<li>';
                for (var i = 0; i < this.actions.length; i++) {
                    if (this.actions[i].enable) {
                        htmlDiv += this.renderActionButton(docresult, this.actions[i], "dropdown-item");
                    }
                }
                htmlDiv += '</li>';
            }
        }
        htmlDiv += '</ul>';
        htmlDiv += '</div>';

        return htmlDiv;
    },

    // Some commons actions

    copySummaryToClipboard: function(parameters) {
        parameters = JSON.parse(Base64.decode(parameters));
        var bkobj = parameters[0];
        navigator.clipboard.writeText(bkobj.summary);
    },

    copyNewsDescriptionToClipboard: function(parameters) {
        parameters = JSON.parse(Base64.decode(parameters));
        var bkobj = parameters[0];
        navigator.clipboard.writeText(bkobj.description);
    }
}

Microsoft.Search.Actions.init();

export default Microsoft.Search.Actions;