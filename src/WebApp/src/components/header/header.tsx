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
    ["small"]: "h-[180px] md:h-[180px]",
    ["medium"]: "h-[180px] bg-black",
    ["large"]: "h-[240] bg-black",
};

export function Header({ children, className, size }: HeaderProps) {
    return (
        <header className={`w-full ${containerStyles[size || "small"]}`}>
            <div
                className={`_max-content-width mx-auto flex h-full flex-col justify-between pt-8 text-left md:pt-16 ${
                    styles[size || "small"]
                } ${className || ""}`}
            >
                {children}
            </div>
        </header>
    );
}
