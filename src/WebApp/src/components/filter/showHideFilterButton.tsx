import React from "react";
import { Button } from "@fluentui/react-components";
import { Filter20Filled } from "@fluentui/react-icons";

interface FilterButtonProps {
    className?: string;
    onFilterPress?: () => void;
}

export function FilterButton({ className, onFilterPress }: FilterButtonProps) {
    return (
        <>
            <Button className="" onClick={onFilterPress} icon={<Filter20Filled />} appearance="subtle">
                Filter
            </Button>
        </>
    );
}
