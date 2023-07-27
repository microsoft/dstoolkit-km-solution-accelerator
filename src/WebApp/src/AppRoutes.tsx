import React, { useEffect } from "react";
import { Routes, Route, Outlet } from "react-router-dom";
import { useMsal } from "@azure/msal-react";
import { InteractionStatus, RedirectRequest } from "@azure/msal-browser";
import { Auth } from "./utils/auth/auth";
import { Home } from "./pages/home/home";

function App() {
    // const { accounts } = useMsal();

    return (
        <Routes>
            <Route path="/" element={<Home />} />

            <Route path="/search" element={<Home isSearchResultsPage={true} />} />

            {/* <Route
                path="/something"
                element={
                    <ProtectedRoute isAllowed={isPlatformAdmin(accounts)}>
                        <PageA />
                    </ProtectedRoute>
                }
            /> */}
            <Route path="*" element={<NotFound />} />
        </Routes>
    );
}

function ProtectedRoute({ isAllowed, children }: { isAllowed?: boolean; children: JSX.Element }): JSX.Element | null {
    const { instance, inProgress } = useMsal();

    if (isAllowed === undefined) isAllowed = instance.getActiveAccount() !== null;

    useEffect(() => {
        // Force user login if he isn't and has no access.
        if (!isAllowed && inProgress === InteractionStatus.None && instance.getActiveAccount() == null) {
            instance.loginRedirect(Auth.getAuthenticationRequest() as RedirectRequest);
        }
    }, [inProgress]);

    if (inProgress && inProgress === InteractionStatus.None) {
        if (isAllowed) return children ? children : <Outlet />;
        else return <Unauthorized />;
    } else {
        return null;
    }
}

function NotFound() {
    return (
        <main className="p-8 md:px-24">
            <h1>Not Found</h1>
        </main>
    );
}

function Unauthorized() {
    const { instance } = useMsal();

    function signOut() {
        instance.logoutRedirect();
    }
    return (
        <main className="p-8 md:px-24">
            <h1>Unauthorized</h1>
            <button onClick={signOut}>Logout</button>
        </main>
    );
}
export default App;
