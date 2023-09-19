import React, { Suspense } from "react";
import { BrowserRouter } from "react-router-dom";
import { Layout } from "./components/layout/layout";
import { Telemetry } from "./utils/telemetry/telemetry";
import { AppInsightsContext, ReactPlugin } from "@microsoft/applicationinsights-react-js";
import { MsalProvider } from "@azure/msal-react";
import { Auth } from "./utils/auth/auth";
import { FluentProvider, makeStyles, webLightTheme } from "@fluentui/react-components";
import resolveConfig from "tailwindcss/resolveConfig";
import TailwindConfig from "../tailwind.config";
import AppRoutes from "./AppRoutes";
import { SnackbarProvider } from "notistack";
import { SnackbarSuccess } from "./components/snackbar/snackbarSuccess";
import { SnackbarError } from "./components/snackbar/snackbarError";

/* Application insights initialization */
const reactPlugin: ReactPlugin = Telemetry.initAppInsights(window.ENV.APP_INSIGHTS_CS, true);

/* MSAL should be instantiated outside of the component tree to prevent it from being re-instantiated on re-renders.
 * For more, visit: https://github.com/AzureAD/microsoft-authentication-library-for-js/blob/dev/lib/msal-react/docs/getting-started.md
 */
const msalInstance = Auth.initAuth(
    window.ENV?.AUTH.clientId,
    window.ENV?.AUTH.authority,
    window.ENV?.AUTH.knownAuthorities,
    window.ENV?.AUTH.cacheLocation,
    window.ENV.AUTH.resources
);

// FluentUI v9 theme customization using tailwind defined values
const fullConfig = resolveConfig(TailwindConfig);

// eslint-disable-next-line @typescript-eslint/no-non-null-assertion, @typescript-eslint/no-explicit-any
webLightTheme.colorBrandForegroundLink = (fullConfig.theme!.colors as any).primary["100"];
// eslint-disable-next-line @typescript-eslint/no-non-null-assertion, @typescript-eslint/no-explicit-any
webLightTheme.colorNeutralForeground1 = (fullConfig.theme!.colors as any).black;

function App() {
    return (
        <Suspense>
            <MsalProvider instance={msalInstance}>
                <AppInsightsContext.Provider value={reactPlugin}>
                    <FluentProvider theme={webLightTheme}>
                        <BrowserRouter>
                            <SnackbarProvider
                                anchorOrigin={{ vertical: "top", horizontal: "center" }}
                                Components={{ success: SnackbarSuccess, error: SnackbarError }}
                            >
                                <Layout>
                                    <AppRoutes />
                                </Layout>
                            </SnackbarProvider>
                        </BrowserRouter>
                    </FluentProvider>
                </AppInsightsContext.Provider>
            </MsalProvider>
        </Suspense>
    );
}

export default App;
