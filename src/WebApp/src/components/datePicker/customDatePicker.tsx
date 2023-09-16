import React from "react";
import { Field, makeStyles } from "@fluentui/react-components";
import { DatePicker, DatePickerProps } from "@fluentui/react-datepicker-compat";

const useStyles = makeStyles({
    control: {
        maxWidth: "90px",
    },
    
    
    
});

const onFormatDate = (date?: Date): string => {
    return !date ? "" : date.getDate() + "/" + (date.getMonth() + 1) + "/" + (date.getFullYear());
};

export function CustomDatePicker(props: Partial<DatePickerProps>) {
    const styles = useStyles();

    const [value, setValue] = React.useState<Date | null | undefined>(null);
    const datePickerRef = React.useRef<HTMLInputElement>(null);

    const onParseDateFromString = React.useCallback(
        (newValue: string): Date => {
          const previousValue = value || new Date();
          const newValueParts = (newValue || "").trim().split("/");
          const day =
            newValueParts.length > 0
              ? Math.max(1, Math.min(31, parseInt(newValueParts[0], 10)))
              : previousValue.getDate();
          const month =
            newValueParts.length > 1
              ? Math.max(1, Math.min(12, parseInt(newValueParts[1], 10))) - 1
              : previousValue.getMonth();
          let year =
            newValueParts.length > 2
              ? parseInt(newValueParts[2], 10)
              : previousValue.getFullYear();
          if (year < 100) {
            year +=
              previousValue.getFullYear() - (previousValue.getFullYear() % 100);
          }
          return new Date(year, month, day);
        },
        [value]
      );

    return (
        <Field label="">
            <DatePicker 
                className={styles.control} 
                placeholder="Select a date" 
                value={value}
                onSelectDate={setValue as (date?: Date | null) => void}
                formatDate={onFormatDate}
                parseDateFromString={onParseDateFromString} 
                size="small"
                {...props}
            />
        </Field>
    );
}
