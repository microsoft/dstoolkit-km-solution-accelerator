import { useState } from "react";
import { Text } from "@fluentui/react-components";

interface ModelSwitchProps {
    onSwitchChange: (model: string) => void;
}

export function ModelSwitch({ onSwitchChange }: ModelSwitchProps) {

    const GPT35 = "chat_35";
    const GPT4 = "chat_4";

    const [activeSwitch, setActiveSwitch] = useState(GPT35);

    const handleClick = (model: string) => {
        setActiveSwitch(model);
        onSwitchChange(model);
    };

    return (
        <div className="flex h-[70px] w-[180px] items-center justify-center rounded-lg ">
            <div className="align-center flex h-[60px] w-[150px] items-center justify-center rounded-full shadow-md bg-neutral-300">
                <div
                    onClick={() => handleClick(GPT35)}
                    className={`flex h-[50px] w-[70px] items-center justify-center rounded-full border border-neutral-300 ${
                        activeSwitch === GPT35 ? "bg-neutral-50 shadow-lg" : "bg-neutral-300"
                    }`}
                >
                    <Text className={`${activeSwitch === GPT35 ? "font-bold" : ""}`} weight={activeSwitch === GPT35 ? "bold" : "regular"}>
                        GPT3.5
                    </Text>
                </div>
                <div
                    onClick={() => handleClick(GPT4)}
                    className={`flex h-[50px] w-[70px] items-center justify-center rounded-full border border-neutral-300 ${
                        activeSwitch === GPT4 ? "bg-neutral-50 shadow-lg" : "bg-neutral-300"
                    }`}
                >
                    <Text className={`${activeSwitch === GPT4 ? "font-bold" : ""}`} weight={activeSwitch === GPT4 ? "bold" : "regular"}>
                        GPT4
                    </Text>
                </div>
            </div>
        </div>
    );
}
