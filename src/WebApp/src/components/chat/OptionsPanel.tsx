import { Label, ToggleButton } from "@fluentui/react-components";
import { useState } from "react";
import { ModelSwitch } from "./modelSwitch";

interface OptionsPanelProps {
    onModelChange: (model: string) => void;
    onSourceChange: (source: string) => void;
    disabled: boolean;
}

export function OptionsPanel({ onModelChange, onSourceChange, disabled }: OptionsPanelProps) {
    const GPT35 = "chat_35";
    const GPT4 = "chat_4";
    const LLM = "gptchat";
    const WoR = "rag_wor";

    const [model, setModel] = useState(GPT4);
    const [source, setSource] = useState(WoR);

    const handleSwitchChange = (activeSwitch: string) => {
        setModel(activeSwitch);
        onModelChange(activeSwitch);
    };

    const onChecked = (button: string) => {
        setSource(button);
        onSourceChange(button);
    };

    return (
        <div className="mx-40 my-10 flex flex-col items-center justify-center rounded-xl bg-neutral-500 bg-opacity-10 shadow-md outline outline-1 outline-transparent">
            <div className="my-5 mr-2">
                <ModelSwitch onSwitchChange={handleSwitchChange} disabled={disabled} />
            </div>

            <Label className="mb-2 mr-40 mt-1 ">What do you want to chat with?</Label>
            <div className="mb-10 ml-7 flex space-x-2">
                <ToggleButton
                    checked={source === LLM}
                    onClick={() => onChecked(LLM)}
                    appearance={source === LLM ? "primary" : "outline"}
                    shape="rounded"
                    disabledFocusable={disabled && source !== LLM}
                >
                    LLM
                </ToggleButton>
                <ToggleButton
                    checked={source === WoR}
                    onClick={() => onChecked(WoR)}
                    appearance={source === WoR ? "primary" : "outline"}
                    shape="rounded"
                    disabledFocusable={disabled && source !== WoR}
                >
                    Web of Reports
                </ToggleButton>

                <ToggleButton
                    checked={source === "My own documents"}
                    onClick={() => onChecked("My own documents")}
                    appearance={source === "My own documents" ? "primary" : "outline"}
                    shape="rounded"
                    disabledFocusable={disabled && source !== "My own documents"}
                >
                    My own documents
                </ToggleButton>
            </div>
        </div>
    );
}
