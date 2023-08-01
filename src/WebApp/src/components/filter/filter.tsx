import React, { useEffect, useRef } from "react";
import { Checkbox } from "@fluentui/react-checkbox";
import { useTranslation } from "react-i18next";
import { Link, makeStyles } from "@fluentui/react-components";
// import { Facet, FacetType } from "../../types/facet";
import { httpClient } from "../../utils/httpClient/httpClient";
import { useEffectOnce } from "../../utils/react/useEffectOnce";
import { Facet, FacetType } from "../../types/facet";
import { mockFacets } from "./mockFacets";
// import { RunFlags } from "../../types/runFlags";

const useStyles = makeStyles({
    clearAll: { fontSize: "12px", fontWeight: "600" },
});

interface FilterProps {
    className?: string;
    // facetsCounts?: Facet[];
    onFilterChanged: (filters: Record<FacetType, string[]>) => void;
}

export function Filter({ className, /* facetsCounts, */ onFilterChanged }: FilterProps) {
    const { t } = useTranslation();

    const [allInd, setAllInd] = React.useState<string[]>([]);
    const [countByInd, setCountByInd] = React.useState<Record<string, number>>({});
    const [checkedInd, setCheckedInd] = React.useState<string[]>([]);

    const [allTags, setAllTags] = React.useState<string[]>([]);
    const [countByTag, setCountByTag] = React.useState<Record<string, number>>({});
    const [checkedTags, setCheckedTags] = React.useState<string[]>([]);

    const [allTypes, setAllTypes] = React.useState<string[]>([]);
    const [countByType, setCountByType] = React.useState<Record<string, number>>({});
    const [checkedTypes, setCheckedTypes] = React.useState<string[]>([]);

    const initCompleted = useRef<boolean>(false);

    const classes = useStyles();

    // Custom hook that can be used instead of useEffect() with zero dependencies.
    // Avoids a double execution of the effect when in React 18 DEV mode with <React.StrictMode>
    useEffectOnce(() => {
        loadAllFacetsAsync();
    });

    // useEffect(() => {
    //     if (!facetsCounts || facetsCounts.length === 0) return;

    //     const inIndustries = facetsCounts.find((f) => f.name === FacetType.Industries)?.categories || {};
    //     setCountByInd(inIndustries);

    //     const inTags = facetsCounts.find((f) => f.name === FacetType.Tags)?.categories || {};
    //     setCountByTag(inTags);

    //     // const inTypes = facetsCounts.find((f) => f.name === FacetType.AssetTypes)?.categories || {};
    //     // setCountByType(inTypes);
    // }, [facetsCounts]);

    useEffect(() => {
        // If all facets are selected send empty filter list
        if (initCompleted.current)
            onFilterChanged({
                [FacetType.Industries]: checkedInd.length === allInd.length ? [] : checkedInd,
                [FacetType.Tags]: checkedTags.length === allTags.length ? [] : checkedTags,
                // [FacetType.AssetTypes]: checkedTypes.length === allTypes.length ? [] : checkedTypes,
            });
    }, [checkedInd, checkedTags, checkedTypes]);

    async function loadAllFacetsAsync() {
        const facetResult: Facet[] = mockFacets;

        const indCat = facetResult.find((f) => f.name === FacetType.Industries)?.categories || {};
        const tagsCat = facetResult.find((f) => f.name === FacetType.Tags)?.categories || {};
        // const typesCat = facetResult.find((f) => f.name === FacetType.AssetTypes)?.categories || {};

        const industries = Object.entries(indCat)?.map((name) => {
            return name[0];
        });
        const tags = Object.entries(tagsCat)?.map((name) => {
            return name[0];
        });
        // const types = Object.entries(typesCat)?.map((name) => {
        //     return name[0];
        // });
        setAllInd(industries);
        setAllTags(tags);
        // setAllTypes(types);
        initCompleted.current = true;
    }

    function clearAll() {
        setCheckedInd([]);
        setCheckedTags([]);
        
    }

    // We can't make it sticky because there are too many filters and it would not allow us to scroll to the last ones "md:sticky md:top-5 md:self-start"
    return (
        <div className={`${className || ""} flex flex-col`}>
            <div className="flex items-center justify-between border-b border-b-neutral-300 pb-2">
                <span className="text-lg">{t("components.filter.title")}</span>
                <Link className={classes.clearAll} onClick={clearAll}>
                    {t("components.filter.clear-all")}
                </Link>
            </div>
                <>
                    <span className="mt-4 mb-2 text-base font-semibold">{t("components.filter.industry")}</span>
                    {/* <Checkbox
                className="-ml-2"
                checked={checkedInd.length === allInd.length ? true : checkedInd.length > 0 ? "mixed" : false}
                onChange={(_ev, data) => (data.checked ? setCheckedInd([...allInd]) : setCheckedInd([]))}
                label={`${t("common.all")} (${
                    facetsCounts?.find((f) => f.name === FacetType.Industries)?.totalResults || 0
                })`}
            /> */}
                    {allInd.map((item, idx) => {
                        return (
                            <Checkbox
                                key={idx}
                                className="-ml-2"
                                checked={checkedInd.includes(item)}
                                onChange={(_ev, data) =>
                                    data.checked
                                        ? setCheckedInd((o) => [...o, item])
                                        : setCheckedInd((o) => o.filter((o) => o !== item))
                                }
                                label={`${item} (${countByInd[item] || 0})`}
                            />
                        );
                    })}
                    <span className="my-5 border-b border-b-neutral-300" />
                </>
            
            
        </div>
    );
}