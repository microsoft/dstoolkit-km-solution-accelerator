import React from "react";
import { useTranslation } from "react-i18next";

export function Footer() {
    const { t } = useTranslation();

    return (
        <footer className="mt-20 w-full bg-neutral-50">
            {/* Parent is a full width container */}
            {/* Child is centered and has max width */}
            <ul className="_max-content-width mx-auto flex flex-wrap justify-end gap-6 whitespace-nowrap p-8 text-[12px] text-neutral-700 md:px-24">
                <li>
                    <a href="https://" target="_blank" rel="noreferrer">
                        {t("components.footer.contact")}
                    </a>
                </li>
                <li>
                    <a href="https://" target="_blank" rel="noreferrer">
                        {t("components.footer.privacy")}
                    </a>
                </li>
                <li>
                    <span
                        role="link"
                        onClick={() => console.log("TO DO")}
                        className="cursor-pointer hover:underline"
                        id="manage-cookies-link"
                    >
                        {t("components.footer.manage-cookies")}
                    </span>
                </li>
                <li>
                    <a href="https://" target="_blank" rel="noreferrer">
                        {t("components.footer.terms-of-use")}
                    </a>
                </li>
                <li>{t("components.footer.copyright")}</li>
            </ul>
        </footer>
    );
}
