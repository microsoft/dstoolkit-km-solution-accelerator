window.ENV = {
    ENVIRONMENT: "local",
    API_URL: "https://?", 
    APP_INSIGHTS_CS:
        "InstrumentationKey=?;IngestionEndpoint=?",
    AUTH: {
        clientId: "",
        authority: "",
        b2cPolicies: undefined,
        cacheLocation: "localStorage",
        knownAuthorities: ["login.microsoftonline.com"],
        resources: {
            api: {
                endpoint: "",
                scopes: ["api://?/api.access"],
            },
        },
    }
};
