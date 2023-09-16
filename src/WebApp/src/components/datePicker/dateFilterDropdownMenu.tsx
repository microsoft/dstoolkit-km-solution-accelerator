import { Divider, Dropdown, Option, Button, makeStyles } from "@fluentui/react-components";
import { CustomDatePicker } from "./customDatePicker";

const useStyles = makeStyles({
        root: {
            maxWidth: "100px",
            
        },
        }
    );

export function DateFilterDropdownMenu(props: any) {

    const styles = useStyles();
    

    return (
        <div className={styles.root}>
            <Dropdown
                aria-labelledby="dropdown-label"
                placeholder="Anytime"
                appearance="outline"
                
                {...props}
                >
                <Option key="1" value="1">
                    All
                </Option>
                <Option key="2" value="2">
                    Past 24 hours
                </Option>
                <Option key="3" value="3">
                    Past week
                </Option>
                <Option key="4" value="4">
                    Past month
                </Option>
                <Option key="5" value="5">
                    Past year
                </Option>
                <Divider />
                    <div className="mt-2 mb-2 ml-7">
                        Custom Date
                        <div className="flex flex-row flex-nowrap justify-around gap-2 mr-7 ">
                            <CustomDatePicker />
                            <CustomDatePicker />
                        </div>
                    </div>
                    <div className="flex justify-end mr-5 mb-2">
                        <Button appearance="outline" size="small" >Apply</Button>
                    </div>
            </Dropdown>
        </div>
    );

};