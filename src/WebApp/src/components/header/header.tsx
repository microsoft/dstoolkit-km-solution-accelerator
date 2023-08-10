import React from "react";

interface HeaderProps {
    children: React.ReactNode;
    className?: string;
    size?: "tiny" | "extra-small" | "small" | "medium" | "large" | "extra-large" | "huge";
}

const styles: Readonly<Record<string, string>> = {
    ["small"]: "pb-8",
    ["medium"]: "pb-8 md:pb-16",
    ["large"]: "pb-8 md:pb-16",
};
const containerStyles: Readonly<Record<string, string>> = {
    ["small"]: "h-[180px] md:h-[240px]",
    ["medium"]: "h-[312px] bg-neutral-200",
    ["large"]: "h-[344px] bg-neutral-200",
};

export function Header({ children, className, size }: HeaderProps) {
    return (
        <header className={`w-full ${containerStyles[size || "small"]}`}>
            <div
                className={`_max-content-width mx-auto flex h-full flex-col justify-between px-8 pt-8 text-left md:px-24 md:pt-16 ${
                    styles[size || "small"]
                } ${className || ""}`}
            >
                {children}
            </div>
        </header>
    );
}
