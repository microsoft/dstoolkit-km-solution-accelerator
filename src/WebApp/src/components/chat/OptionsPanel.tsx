import { Label, Switch, ToggleButton, Title1 } from "@fluentui/react-components";
import { set } from "date-fns";
import { useCallback, useState } from "react";

export function OptionsPanel() {
    const [gpt4, setGpt4] = useState(false);
    const [selectedButton, setSelectedButton] = useState("LLM");

    const onChecked = (button: string) => {
        setSelectedButton(button);
    };

    const onChange = useCallback(
        (ev) => {
            setGpt4(ev.target.checked);
        },
        [setGpt4]

    );
    
    console.log("gpt4", gpt4);
    console.log("selectedButton", selectedButton);


    return (
        <div className="my-10 flex flex-col items-center justify-center mx-40 shadow-md rounded-xl outline outline-1 outline-transparent bg-opacity-10 bg-neutral-500">
            <div className="flex w-full justify-start mt-5 ml-[135px]">
                <Title1 italic
                >Options</Title1>
            </div>
            <div className="my-5 mr-2">
                <Label className="mb-0.5 mr-2 ">GPT-3.5 </Label>
                <Switch checked={gpt4} onChange={onChange} />
                <Label className="mb-0.5 ml-2 ">GPT-4 </Label>
            </div>

            <Label className="mt-1 mr-40 mb-2 ">What do you want to chat with?</Label>
            <div className="flex space-x-2 mb-10 ml-7">
                <ToggleButton
                    checked={selectedButton === 'LLM'}
                    onClick={() => onChecked('LLM')}
                    appearance={selectedButton === 'LLM' ? "primary" : "outline"}
                    shape="rounded"
                >
                    LLM
                </ToggleButton>
                <ToggleButton
                    checked={selectedButton === 'Web of Reports'}
                    onClick={() => onChecked('Web of Reports')}
                    appearance={selectedButton === 'Web of Reports' ? "primary" : "outline"}
                    shape="rounded"
                >
                    Web of Reports
                </ToggleButton>

                <ToggleButton
                    checked={selectedButton === 'My own documents'}
                    onClick={() => onChecked('My own documents')}
                    appearance={selectedButton === 'My own documents' ? "primary" : "outline"}
                    shape="rounded"
                >
                    My own documents
                </ToggleButton>
            </div>
        </div>
    );
}
