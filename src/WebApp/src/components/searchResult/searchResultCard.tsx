import { Button, makeStyles } from "@fluentui/react-components";
import { Tag, TagGroup } from "@fluentui/react-tags-preview";
import { Sparkle24Regular } from "@fluentui/react-icons";

const useStyles = makeStyles({
    wrapper: {
      columnGap: "6px",
      display: "flex",
      marginTop: "10px",
      
    },
  });

export function SearchResultCard() {
    const styles = useStyles();
    return (
        <div className="min-h-80 flex min-w-[278px] flex-grow flex-col overflow-hidden rounded-b-xl bg-white py-5 pl-5 pr-4 shadow-lg">
            <div className="-ml-5 -mr-4 -mt-5 h-1" />
            <div className="flex flex-row ">
                <div className="flex items-center">imggggggggggg</div>
                <div className="ml-5 flex-col">
                    <div className="flex-1 flex-row ">icon + citation format</div>
                    <div className="flex-1 flex-row mt-2">Title - exampleeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee</div>
                    <div className="flex-1 flex-row mt-2">Publish date</div>
                    <div className={styles.wrapper}>
                        <Button appearance="outline" >See More</Button>
                        <Button appearance="outline" >Download</Button>
                        <Button appearance="outline" ><Sparkle24Regular/></Button>
                    </div>
                </div>
            </div>

            <h6 className="border-b border-b-neutral-300 pb-5"></h6>
            <div className="flex flex-1 min-h-[80px] text-ellipsis pt-2 ">
                Fabric integrates proven technologies like Azure Data Factory, Azure Synapse, and Microsoft Power BI
                into a single unified product, empowering data and business professionals alike to unlock the potential
                of data and lay the foundation for the era of AI. ... Introducing Microsoft Fabric – a unified analytics
                solution for the era of AI 9 ... Unlocking Transformative Data Value with Microsoft Fabric A
                three-phased approach
            </div>

            <div className="flex items-center pt-5 text-[12px]">
                <TagGroup role="list">
                    <Tag role="listitem" className="bg-blue-100 text-blue-600">
                        abc
                    </Tag>
                    <Tag role="listitem" className="bg-blue-100 text-blue-600">
                        abc
                    </Tag>
                    <Tag role="listitem" className="bg-blue-100 text-blue-600">
                        abc
                    </Tag>
                </TagGroup>
            </div>
        </div>
    );
}
