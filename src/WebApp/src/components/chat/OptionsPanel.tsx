import { Label, ToggleButton } from "@fluentui/react-components";
import { useState } from "react";
import { ModelSwitch } from "./modelSwitch";

interface OptionsPanelProps {
    onModelChange: (model: string) => void;
    onSourceChange: (source: string) => void;
  }

export function OptionsPanel({ onModelChange, onSourceChange }: OptionsPanelProps) {
    const [model, setModel] = useState("chat_35");
    const [source, setSource] = useState("rag_wor");

    const handleSwitchChange = (activeSwitch: string) => {
        setModel(activeSwitch);
        onModelChange(activeSwitch);
        console.log("activeSwitch", activeSwitch);
    };

    const onChecked = (button: string) => {
        setSource(button);
        onSourceChange(button);
    };

    console.log("source", source);

    return (
        <div className="mx-40 my-10 flex flex-col items-center justify-center rounded-xl bg-neutral-500 bg-opacity-10 shadow-md outline outline-1 outline-transparent">
            <div className="my-5 mr-2">
                <ModelSwitch onSwitchChange={handleSwitchChange} />
            </div>

            <Label className="mb-2 mr-40 mt-1 ">What do you want to chat with?</Label>
            <div className="mb-10 ml-7 flex space-x-2">
                <ToggleButton
                    checked={source === "rag_wor"}
                    onClick={() => onChecked("rag_wor")}
                    appearance={source === "rag_wor" ? "primary" : "outline"}
                    shape="rounded"
                >
                    LLM
                </ToggleButton>
                <ToggleButton
                    checked={source === "Web of Reports"}
                    onClick={() => onChecked("Web of Reports")}
                    appearance={source === "Web of Reports" ? "primary" : "outline"}
                    shape="rounded"
                >
                    Web of Reports
                </ToggleButton>

                <ToggleButton
                    checked={source === "My own documents"}
                    onClick={() => onChecked("My own documents")}
                    appearance={source === "My own documents" ? "primary" : "outline"}
                    shape="rounded"
                >
                    My own documents
                </ToggleButton>
            </div>
        </div>
    );
}
