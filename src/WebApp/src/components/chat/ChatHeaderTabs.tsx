import { Button, Tab, TabList, Divider } from "@fluentui/react-components";
import { Search20Regular, Filter20Filled } from "@fluentui/react-icons";
import { useNavigate } from "react-router-dom";

interface ChatHeaderTabsProps {
    className?: string;
}

export function ChatHeaderTabs({ className }: ChatHeaderTabsProps) {
    const navigate = useNavigate();

    const tabs = [
        { value: "Search", label: "Search", icon: <Search20Regular /> },
        { value: "Chat", label: "Chat" },
    ];

    const commonTabClassName = "mr-2";

    const handleClick = (path: string) => {
        console.log(path)
        navigate(path);
    };

    return (
        <>
            <div className={`${className || ""} `}>
                <div className="flex ">
                    <TabList defaultSelectedValue={"Chat"} appearance="subtle">
                        {tabs.map((tab) => (
                            <Tab key={tab.value} value={tab.value} icon={tab.icon} className={commonTabClassName} onClick={() => handleClick("/")}>
                                {tab.label}
                            </Tab>
                        ))}
                    </TabList>
                </div>
            </div>
        </>
    );
}