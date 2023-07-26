window.ENV = {
    ENVIRONMENT: "__environment__",
    API_URL: "__api_url__",
    APP_INSIGHTS_CS: "__app_insights_cs__",
    AUTH: {
        clientId: "__auth_client_id__",
        authority: "https://__auth_instance__/__auth_tenant_id__",
        b2cPolicies: undefined,
        cacheLocation: "localStorage",
        knownAuthorities: ["__auth_instance__"],
        resources: {
            api: {
                endpoint: "",
                scopes: ["__auth_scope__"],
            },
        },
    }
};
