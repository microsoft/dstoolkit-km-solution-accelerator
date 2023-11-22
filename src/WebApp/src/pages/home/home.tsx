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
import { DateFilterDropdownMenu } from "../../components/datePicker/dateFilterDropdownMenu";
import { SearchResultCard } from "../../components/searchResult/searchResultCard";
import { ChatIntegration } from "../../components/copilot/ChatIntegration";
import { useEffectOnce } from "../../utils/react/useEffectOnce";
import { SearchRequest } from "../../types/searchRequest";

interface HomeProps {
    isSearchResultsPage?: boolean;
}

export function Home({ isSearchResultsPage }: HomeProps) {
    const { t } = useTranslation();
    const [isLoading, setIsLoading] = useState<boolean>(false);
    // const [query, setQuery] = useState<string>();
    const [searchParams, setSearchParams] = useSearchParams();
    const [filters, setFilters] = useState<Record<FacetType, string[]>>();
    const [filterOpen, setFilterOpen] = useState<boolean>(true);
    const [showCopilot, setShowCopilot] = useState<boolean>(false);

    const [query, setQuery] = useState(""); // State for queryText
    const [incomingFilter, setIncomingFilter] = useState(""); // State for incomingFilter
    const [scoringProfile, setScoringProfile] = useState(""); // State for scoringProfile
    const [isSemanticSearch, setIsSemanticSearch] = useState(false); // State for isSemanticSearch
    const [isQueryTranslation, setIsQueryTranslation] = useState(true); // State for isQueryTranslation
    const [isQuerySpellCheck, setIsQuerySpellCheck] = useState(true); // State for isQuerySpellCheck
    const [suggestionsAsFilter, setSuggestionsAsFilter] = useState(true); // State for suggestionsAsFilter
    const [orMVRefinerOperator, setOrMVRefinerOperator] = useState(false);

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

    function toggleCopilot(): void {
        console.log("*** toggleCopilot");
        setShowCopilot(!showCopilot);
    }

    // Custom hook that can be used instead of useEffect() with zero dependencies.
    // Avoids a double execution of the effect when in React 18 DEV mode with <React.StrictMode>
    useEffectOnce(() => {});

    async function loadDataAsync() {
        setIsLoading(true);

        const payload: SearchRequest = {
            queryText: query || "*",
            searchFacets: [],
            currentPage: 1,
            incomingFilter: incomingFilter,
            parameters: {
                scoringProfile: scoringProfile,
                inOrderBy: [],
            },
            options: {
                isSemanticSearch: isSemanticSearch,
                isQueryTranslation: isQueryTranslation,
                isQuerySpellCheck: isQuerySpellCheck,
                suggestionsAsFilter: suggestionsAsFilter,
                orMVRefinerOperator: orMVRefinerOperator,
            },
        };

        // const result: Paged<DataType> = await httpClient.post(`${window.ENV.API_URL}/something`, payload);
        // setData(result);
        setIsLoading(false);
    }

    return (
        <>
            <Header className="flex flex-col justify-between bg-contain bg-right-bottom bg-no-repeat" size={"large"}>
                <div className="-ml-8">
                    <HeaderBar location={NavLocation.Home} />
                </div>
                <div>
                    <div>
                        {/* <h1 className="max-sm:text-3xl">{t("pages.home.title")}</h1> */}
                        {/* <div className="mb-10 w-full text-lg md:w-1/2">{t("pages.home.subtitle")}</div> */}

                        <SearchBox
                            ref={searchBoxRef}
                            className={`flex w-full ${
                                // !isSearchResultsPage
                                //     ? "items-center"
                                //     :
                                "-mb-5 mt-10 justify-center justify-items-center pb-5 pt-5 max-sm:items-center"
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

            <main className="w-full">
                <div className="grid grid-cols-5 gap-x-2 gap-y-8">
                    <div className="col-span-1 col-start-1 ml-8 pt-1">
                        <FilterButton className="" onFilterPress={onFilterPress} />
                    </div>

                    <div className="col-span-1 col-start-2  flex md:col-span-3 md:col-start-2">
                        <HeaderMenu className="" />
                        <Button
                            className=""
                            onClick={() => toggleCopilot()}
                            icon={<img src="\img\Copilot.png"></img>}
                            appearance="subtle"
                        >
                            Copilot
                        </Button>
                    </div>

                    {/* <div className="col-start-2 col-span-2 md:col-start-4 md:col-span-1 md:mt-2">
                            <Button className="" onClick={() => console.log("click")} icon={<img src="\img\Copilot.png"></img> } appearance="subtle">
                                Copilot
                            </Button>
                        </div> */}

                    <div className="absolute left-0 right-0 mt-11 w-full border-b border-b-neutral-300"></div>

                    {filterOpen && (
                        <div className="col-span-1 col-start-1 md:block">
                            <Filter className="" onFilterChanged={onFilterChanged} />
                        </div>
                    )}

                    <div className="col-span-3 col-start-2 ">
                        <div className="flex justify-between">
                            <div className="ml-5 flex">About X Results</div>
                            <div className="mr-40 flex ">
                                <DateFilterDropdownMenu />
                            </div>
                        </div>

                        <div className="flex flex-col">
                            {isLoading && (
                                <div className="mt-16 w-full">
                                    <Spinner size="extra-large" />
                                </div>
                            )}
                            {!isLoading && (
                                <div className="ml-5 mt-5">
                                    <SearchResultCard />
                                </div>
                            )}
                        </div>
                    </div>

                    {showCopilot && (
                        <div className="col-span-1 col-start-5 -mt-8">
                            <div className="flex flex-col">
                                <ChatIntegration />
                            </div>
                        </div>
                    )}
                </div>
            </main>
        </>
    );
}
