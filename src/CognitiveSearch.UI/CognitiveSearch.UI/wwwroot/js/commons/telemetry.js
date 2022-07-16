// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

//
// Telemetry
//
Microsoft.Telemetry = Microsoft.Telemetry || {};
Microsoft.Telemetry = {
    LogCustomEvent: function (name, properties) {
        window.appInsights.trackEvent({
            name: name,
            properties: properties
        });
    },
    LogSearchAnalytics: function (docCount) {
        if (docCount != null) {
            var recordedQuery = Microsoft.View.currentQuery;
            if (Microsoft.View.currentQuery == undefined || Microsoft.View.currentQuery == null) {
                recordedQuery = "*";
            }

            window.appInsights.trackEvent({
                name: 'Search',
                properties: {
                    SearchId: Microsoft.View.searchId,
                    QueryTerms: recordedQuery,
                    ResultCount: docCount
                }
            });
        }
    },
    LogClickAnalytics: function (fileName, index) {
        window.appInsights.trackEvent({
            name: 'Click',
            properties: {
                SearchId: Microsoft.View.searchId,
                ClickedDocId: fileName,
                Rank: index
            }
        });
    }
}

export default Microsoft.Telemetry;