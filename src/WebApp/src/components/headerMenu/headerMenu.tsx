import { Button, Tab, TabList, Divider } from "@fluentui/react-components";
import { Search20Regular, Filter20Filled } from "@fluentui/react-icons";

interface HeaderMenuProps {
    className?: string;
}

export function HeaderMenu({ className }: HeaderMenuProps) {
    return (
    <>
        <div className={`${className || ""} `}>
            {/* <div className=" ">
                <Button className="" icon={<Filter20Filled />} appearance="subtle">
                    Filter
                </Button>
            </div> */}

            <div className="">
                <TabList defaultSelectedValue={"ALL"} appearance="subtle">
                    <Tab icon={<Search20Regular />} value="ALL">
                        ALL
                    </Tab>
                    <Tab value="Documents">Documents</Tab>
                    <Tab value="Images">Images</Tab>
                    <Tab value="Tables">Tables</Tab>
                    <Tab value="Translated Documents">Translated Documents</Tab>
                    <Tab value="Translated Pages">Translated Pages</Tab>
                    <Tab value="Emails">Emails</Tab>
                    <Tab value="Attachments">Attachments</Tab>
                    <Tab icon={<img src="\img\Copilot.png" />} value="Copilot">
                        Copilot
                    </Tab>
                </TabList>
                
            </div>
        </div>
        
       
    </>
    );
}
