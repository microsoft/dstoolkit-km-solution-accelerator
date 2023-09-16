import React from 'react';
import {Field, makeStyles } from "@fluentui/react-components";
import { DatePicker } from "@fluentui/react-datepicker-compat";

const useStyles = makeStyles({
        control: {
          maxWidth: "100px",
        },
      });
      
export function CustomDatePicker() {

    const styles = useStyles();






    return (
        <Field label="" >
            <DatePicker
                className={styles.control}
                placeholder="Select a date"
                
            />
        </Field>

    );

};