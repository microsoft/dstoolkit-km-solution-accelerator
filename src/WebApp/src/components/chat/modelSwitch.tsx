import { useState } from "react";
import { Text } from "@fluentui/react-components";

interface ModelSwitchProps {
    onSwitchChange: (model: string) => void;
}

export function ModelSwitch({ onSwitchChange }: ModelSwitchProps) {
    const [activeSwitch, setActiveSwitch] = useState("chat_35");

    const handleClick = (model: string) => {
        setActiveSwitch(model);
        onSwitchChange(model);
    };

    return (
        <div className="flex h-[70px] w-[180px] items-center justify-center rounded-lg ">
            <div className="align-center flex h-[60px] w-[150px] items-center justify-center rounded-full shadow-md bg-neutral-300">
                <div
                    onClick={() => handleClick("chat_35")}
                    className={`flex h-[50px] w-[70px] items-center justify-center rounded-full border border-neutral-300 ${
                        activeSwitch === "chat_35" ? "bg-neutral-50 shadow-lg" : "bg-neutral-300"
                    }`}
                >
                    <Text className={`${activeSwitch === "chat_35" ? "font-bold" : ""}`} weight={activeSwitch === "GPT3.5" ? "bold" : "regular"}>
                        GPT3.5
                    </Text>
                </div>
                <div
                    onClick={() => handleClick("chat_4")}
                    className={`flex h-[50px] w-[70px] items-center justify-center rounded-full border border-neutral-300 ${
                        activeSwitch === "chat_4" ? "bg-neutral-50 shadow-lg" : "bg-neutral-300"
                    }`}
                >
                    <Text className={`${activeSwitch === "chat_4" ? "font-bold" : ""}`} weight={activeSwitch === "GPT4" ? "bold" : "regular"}>
                        GPT4
                    </Text>
                </div>
            </div>
        </div>
    );
}
