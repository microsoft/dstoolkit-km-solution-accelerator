import {
    Button,
    Textarea,
} from "@fluentui/react-components";
import { AttachRegular, SendRegular } from "@fluentui/react-icons";
import { useEffect, useRef, useState } from "react";

interface Props {
    onSend: (question: string) => void;
    disabled: boolean;
    placeholder?: string;
    clearOnSend?: boolean;
}

export function ChatInput({ onSend, disabled, placeholder, clearOnSend }: Props) {
    const [question, setQuestion] = useState<string>("");

    const textAreaRef = useRef(null);

    const sendQuestion = () => {
        if (disabled) {
            console.log("disabled or empty question");
            return;
        }

        onSend(question);

        if (clearOnSend) {
            setQuestion("");
        }
    };

    const onEnterPress = (event: React.KeyboardEvent<HTMLTextAreaElement>) => {
        if (event.key === "Enter" && !event.shiftKey) {
            event.preventDefault();
            sendQuestion();
        }
    };

    const onQuestionChange = (event: React.ChangeEvent<HTMLTextAreaElement>) => {
        const newValue = event.target.value;
        if (!newValue) {
            setQuestion("");
        } else if (newValue.length <= 500) {
            setQuestion(newValue);
        }
    };

    const sendQuestionDisabled = disabled || !question.trim();

    return (
        <div className="flex items-center justify-center">
            <div className="max-w-xxl w-full">
                <Textarea
                    title="Chat input"
                    aria-label="Chat input field. Click enter to submit input."
                    placeholder="Type your message here.."
                    ref={textAreaRef}
                    id="chat-input"
                    textarea={{
                        className: "w-full h-80",
                    }}
                    className="w-full"
                    value={question}
                    onChange={onQuestionChange}
                    onKeyDown={onEnterPress}
                />
                <div className="flex flex-row justify-between">
                    <Button
                        disabled={false}
                        appearance="transparent"
                        icon={<AttachRegular />}
                        onClick={() => console.log("click")}
                        title="Attach file"
                        aria-label="Attach file button"
                    />
                    <div className="flex flex-row-reverse">
                        <Button
                            className="flex flex-row"
                            title="Submit"
                            aria-label="Submit message"
                            appearance="transparent"
                            icon={<SendRegular />}
                            onClick={sendQuestion}
                            disabled={false}
                        />
                    </div>
                </div>
            </div>
        </div>
    );
}
