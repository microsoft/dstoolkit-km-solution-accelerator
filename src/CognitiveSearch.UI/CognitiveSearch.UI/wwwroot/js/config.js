// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

Microsoft.Config = Microsoft.Config || {};
Microsoft.Config = {
    data: {},
    init: function () {
        return new Promise((resolve, reject) => {
            $.ajax({
                type: 'GET',
                url: '/api/config/get',
                dataType: 'json',
                success: function (data) {
                    Microsoft.Config.data = data;
                    resolve()
                },
                error: function (error) {
                    reject(error)
                },
            })
        });
    }
}

