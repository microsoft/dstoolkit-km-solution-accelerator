import React, { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { HeaderBar, NavLocation } from "../../components/headerBar/headerBar";
import { Paged } from "../../types/paged";
import { Spinner, Dropdown, Option } from "@fluentui/react-components";
import { httpClient } from "../../utils/httpClient/httpClient";
import { Header } from "../../components/header/header";
import { useNavigate, useSearchParams } from "react-router-dom";
import { SearchBox, SearchBoxHandle } from "../../components/searchBox/searchBox";
import { Filter } from "../../components/filter/filter";
import { FacetType } from "../../types/facet";

interface HomeProps {
    isSearchResultsPage?: boolean;
}

export function Home({ isSearchResultsPage }: HomeProps) {
    const { t } = useTranslation();
    const [isLoading, setIsLoading] = useState<boolean>(false);
    const [query, setQuery] = useState<string>();
    const [searchParams, setSearchParams] = useSearchParams();
    const [filters, setFilters] = useState<Record<FacetType, string[]>>();

    const navigate = useNavigate();

    const searchBoxRef = React.createRef<SearchBoxHandle>();

    useEffect(() => {
        console.log("*** isSearchResultsPage", isSearchResultsPage);
        if (isSearchResultsPage) {
            setQuery(decodeURIComponent(searchParams.get("q") || ""));
        } else if (query) {
            // We are back to home - clear query
            setQuery("");
            searchBoxRef.current?.reset();
        }
    }, [isSearchResultsPage]);

    function onSearchChanged(searchValue: string): void {
        console.log("*** onSearchChanged", searchValue);
        if (searchValue) {
            if (isSearchResultsPage) {
                const updatedSearchParams = new URLSearchParams(searchParams.toString());
                updatedSearchParams.set("q", encodeURIComponent(searchValue));
                setSearchParams(updatedSearchParams.toString());
                setQuery(searchValue);
            } else {
                navigate(`/search?q=${encodeURIComponent(searchValue)}`);
            }
        } else {
            setSearchParams("");
            setQuery("");
        }
    }

    function onFilterChanged(newFilters: Record<FacetType, string[]>): void {
        console.log("*** onFilterChanged");
        setFilters(newFilters);
    }

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
            <Header
                className="bg-contain bg-right-bottom bg-no-repeat md:bg-[url('/img/header-default.png')]"
                size={"large"}
            >
                <HeaderBar location={NavLocation.Home} />
                <div>
                    <div>
                        <h1 className="max-sm:text-3xl">{t("pages.home.title")}</h1>
                        <div className="mb-10 w-full text-lg md:w-1/2">{t("pages.home.subtitle")}</div>

                        <SearchBox
                            ref={searchBoxRef}
                            className={`w-full ${
                                !isSearchResultsPage
                                    ? "items-center"
                                    : "items-baseline justify-center max-sm:items-center"
                            }`}
                            labelClassName={`font-semilight ${
                                !isSearchResultsPage
                                    ? "text-[23px] max-sm:text-base"
                                    : "text-[33px] max-sm:text-base leading-8"
                            }`}
                            inputClassName="max-w-xs flex-grow"
                            onSearchChanged={onSearchChanged}
                            initialValue={query}
                        />
                    </div>
                </div>
            </Header>
            <main className="grid grid-cols-1 gap-y-4 px-8 pt-8 md:grid-cols-4 md:gap-x-12 md:px-24">
                <Filter className="mt-5" onFilterChanged={onFilterChanged} />
                

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
