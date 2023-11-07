window.ENV = {
    ENVIRONMENT: "local",
    API_URL: "http://localhost:5901",
    APP_INSIGHTS_CS: "InstrumentationKey=?;IngestionEndpoint=?",
    AUTH: {
        clientId: "175604f9-b2d4-4aef-b642-4211857111b8",
        authority: "https://login.microsoftonline.com/16b3c013-d300-468d-ac64-7eda0820b6d3",
        b2cPolicies: undefined,
        cacheLocation: "localStorage",
        knownAuthorities: ["login.microsoftonline.com"],
        resources: {
            api: {
                endpoint: "",
                scopes: ["api://175604f9-b2d4-4aef-b642-4211857111b8/api.access"],
            },
        },
    },
};
