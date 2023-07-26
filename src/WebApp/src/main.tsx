import React from "react";
import ReactDOM from "react-dom/client";
import { initializeLanguage } from "./utils/i18n/i18n";
import App from "./App";
import "./index.scss";

declare global {
    interface Window {
        ENV: any; // eslint-disable-line @typescript-eslint/no-explicit-any
        WcpConsent: any; // eslint-disable-line @typescript-eslint/no-explicit-any
        
    }
}

initializeLanguage();

ReactDOM.createRoot(document.getElementById("root") as HTMLElement).render(
    // <React.StrictMode>
        <App />
    // </React.StrictMode>
);
