import React, { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { HeaderBar, NavLocation } from "../../components/headerBar/headerBar";
import { Paged } from "../../types/paged";
import { Spinner, Dropdown, Option } from "@fluentui/react-components";
import { httpClient } from "../../utils/httpClient/httpClient";
import { useNavigate, useSearchParams } from "react-router-dom";
import { Header } from "../../components/header/header";

interface HomeProps {
    isSearchResultsPage?: boolean;
}

export function Home({ isSearchResultsPage }: HomeProps) {
    const { t } = useTranslation();
    const [isLoading, setIsLoading] = useState<boolean>(false);
    const navigate = useNavigate();

    // Custom hook that can be used instead of useEffect() with zero dependencies.
    // Avoids a double execution of the effect when in React 18 DEV mode with <React.StrictMode>
    // useEffectOnce(() => {
    // });

    // async function loadDataAsync() {
    //     setIsLoading(true);
    //     const result: Paged<DataType> = await httpClient.post(
    //         `${window.ENV.API_URL}/something`,
    //         payload
    //     );
    //     setData(result);
    //     setIsLoading(false);
    // }

    return (
        <>
            <Header className="" size={"large"}>
                <HeaderBar location={NavLocation.Home} />
                <div className="flex max-h-[60%] justify-between">
                    <div>
                        <h1 className="text-3xl">{t("pages.home.title")}</h1>
                        <div className="text-lg">{t("pages.home.subtitle")}</div>
                    </div>
                    {/* <div>
                        <img src="/img/logo.png" className="h-full w-auto object-contain" alt="logo" />
                    </div> */}
                </div>
            </Header>
            <main className="px-8 pt-8 md:px-24">
                {isLoading && (
                    <div className="mt-16 w-full">
                        <Spinner size="extra-large" />
                    </div>
                )}
                {!isLoading && <>Page content</>}
            </main>
        </>
    );
}
