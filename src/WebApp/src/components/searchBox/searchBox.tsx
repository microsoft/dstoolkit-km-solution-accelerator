import React, { forwardRef, useImperativeHandle, ChangeEvent, KeyboardEvent } from "react";
import { Input } from "@fluentui/react-input";
import { useTranslation } from "react-i18next";
import { Search20Regular } from "@fluentui/react-icons";
import { InputOnChangeData, useId } from "@fluentui/react-components";
import { useDebouncedCallback } from "use-debounce";

export interface SearchBoxHandle {
    reset: () => void;
}

interface SearchBoxProps {
    className?: string;
    labelClassName?: string;
    inputClassName?: string;
    initialValue?: string;
    placeholder?: string;
    onSearchChanged: (searchValue: string) => void;
}

export const SearchBox = forwardRef<SearchBoxHandle, SearchBoxProps>(
    ({ className, labelClassName, inputClassName, initialValue = "", placeholder, onSearchChanged }, ref) => {
        const { t } = useTranslation();
        const [value, setValue] = React.useState(initialValue);
        const inputId = useId("input");

        useImperativeHandle(ref, () => ({
            reset: () => {
                if (value) setValue("");
            },
        }));

        // Debounce callback
        const debounced = useDebouncedCallback(
            (value) => {
                onSearchChanged(value);
            },
            // delay in ms
            1000
        );

        function onChange(_ev: ChangeEvent<HTMLInputElement>, data: InputOnChangeData): void {
            // The controlled input pattern can be used for other purposes besides validation
            if (data.value.length <= 30) {
                setValue(data.value);
                debounced(data.value);
            }
        }

        function onKeyDown(ev: KeyboardEvent<HTMLInputElement>) {
            if (ev.key === "Enter") {
                debounced.cancel();
                setValue((ev.target as HTMLInputElement).value);
                onSearchChanged((ev.target as HTMLInputElement).value);
            }
        }

        return (
            <div className={`${className || ""} flex gap-3`}>
                <label className={labelClassName || ""} htmlFor={inputId}>
                    {t("components.search-box.label")}
                </label>
                <Input
                    className={inputClassName || ""}
                    contentBefore={<Search20Regular />}
                    placeholder={placeholder || t("components.search-box.placeholder")}
                    id={inputId}
                    onChange={onChange}
                    onKeyDown={onKeyDown}
                    value={value}
                    type="search"
                />
            </div>
        );
    }
);

SearchBox.displayName = "SearchBox";
