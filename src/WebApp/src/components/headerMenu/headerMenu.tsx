import { Button, Tab, TabList, Divider } from "@fluentui/react-components";
import { Search20Regular, Filter20Filled } from "@fluentui/react-icons";

interface HeaderMenuProps {
    className?: string;
}

export function HeaderMenu({ className }: HeaderMenuProps) {
    const tabs = [
        { value: "ALL", label: "ALL", icon: <Search20Regular /> },
        { value: "Documents", label: "Documents" },
        { value: "Images", label: "Images" },
        { value: "Tables", label: "Tables" },
        { value: "Translated Documents", label: "Translated Documents" },
        { value: "Translated Pages", label: "Translated Pages" },
        { value: "Emails", label: "Emails" },
        { value: "Attachments", label: "Attachments" },
        {
            value: "Copilot",
            label: "Copilot",
            icon: <img src="\img\Copilot.png" />,
        },
    ];

    const commonTabClassName = "mr-10";

    return (
        <>
            <div className={`${className || ""} `}>
                <div className="flex justify-between">
                    <TabList defaultSelectedValue={"ALL"} appearance="subtle">
                        {tabs.map((tab) => (
                            <Tab key={tab.value} value={tab.value} icon={tab.icon} className={commonTabClassName}>
                                {tab.label}
                            </Tab>
                        ))}
                    </TabList>
                </div>
            </div>
        </>
    );
}
