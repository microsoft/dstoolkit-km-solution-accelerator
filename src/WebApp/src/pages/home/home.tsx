import React, { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { HeaderBar, NavLocation } from "../../components/headerBar/headerBar";
import { Paged } from "../../types/paged";
import { Spinner, Dropdown, Option, Divider, Button } from "@fluentui/react-components";
import { httpClient } from "../../utils/httpClient/httpClient";
import { Header } from "../../components/header/header";
import { useNavigate, useSearchParams } from "react-router-dom";
import { SearchBox, SearchBoxHandle } from "../../components/searchBox/searchBox";
import { Filter } from "../../components/filter/filter";
import { FacetType } from "../../types/facet";
import { HeaderMenu } from "../../components/headerMenu/headerMenu";
import { FilterButton } from "../../components/filter/showHideFilterButton";

interface HomeProps {
    isSearchResultsPage?: boolean;
}

export function Home({ isSearchResultsPage }: HomeProps) {
    const { t } = useTranslation();
    const [isLoading, setIsLoading] = useState<boolean>(false);
    const [query, setQuery] = useState<string>();
    const [searchParams, setSearchParams] = useSearchParams();
    const [filters, setFilters] = useState<Record<FacetType, string[]>>();
    const [filterOpen, setFilterOpen] = useState<boolean>(true);

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

    function onFilterPress(): void {
        console.log("*** onFilterPress");
        setFilterOpen(!filterOpen);
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
                className="flex flex-col justify-between bg-contain bg-right-bottom bg-no-repeat"
                size={!isSearchResultsPage ? "large" : "medium"}
            >
                <HeaderBar location={NavLocation.Home} />
                <div>
                    <div>
                        {/* <h1 className="max-sm:text-3xl">{t("pages.home.title")}</h1> */}
                        {/* <div className="mb-10 w-full text-lg md:w-1/2">{t("pages.home.subtitle")}</div> */}

                        <SearchBox
                            ref={searchBoxRef}
                            className={`w-full ${
                                // !isSearchResultsPage
                                //     ? "items-center"
                                //     :
                                "mb-10 mt-10 items-baseline justify-center max-sm:items-center"
                            }`}
                            labelClassName={`font-semilight ${
                                // !isSearchResultsPage
                                //     ? "text-[23px] max-sm:text-base"
                                //     :
                                "text-[33px] max-sm:text-base leading-8"
                            }`}
                            inputClassName="max-w-xs flex-grow"
                            onSearchChanged={onSearchChanged}
                            initialValue={query}
                        />
                    </div>
                </div>
            </Header>

            <main className="w-full pt-2">
                <div className="grid grid-cols-5 gap-x-4 gap-y-8 md:grid-cols-5 md:gap-x-8">
                    <div className="col-span-1 col-start-1 px-4 pt-1">
                        <FilterButton className="" onFilterPress={onFilterPress} />
                    </div>

                    <div className="col-span-1 col-start-2">
                        <HeaderMenu className="md:col-span-1" />
                    </div>

                    <div className="absolute left-0 right-0 mt-11 w-full border-b border-b-neutral-300"></div>

                    {filterOpen && (
                        <div className="col-span-1 col-start-1 px-10">
                            <Filter className="" onFilterChanged={onFilterChanged} />
                        </div>
                    )}

                    <div className="col-span-2 px-3 md:col-span-3 ">
                        {isLoading && (
                            <div className="mt-16 w-full">
                                <Spinner size="extra-large" />
                            </div>
                        )}
                        {!isLoading && <>Page content</>}
                    </div>
                </div>
            </main>
        </>
    );
}
