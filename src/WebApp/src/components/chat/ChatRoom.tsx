import { useRef, useState } from "react";
import { ChatInput } from "./ChatInput";
import { ChatApiResponse, ChatRequest } from "../../api/models";
import { UserChatMessage } from "./UserChatMessage";
import { Answer } from "./Answer";
import { OptionsPanel } from "./OptionsPanel";
import { httpClient } from "../../utils/httpClient/httpClient";
import { Button } from "@fluentui/react-components";
import { ChatAdd24Regular } from "@fluentui/react-icons";

export function ChatRoom() {
    const lastQuestionRef = useRef<string>("");

    const [isLoading, setIsLoading] = useState<boolean>(false);
    const [isDisbaled, setIsDisabled] = useState<boolean>(false);
    const [error, setError] = useState<unknown>();

    const [answers, setAnswers] = useState<[prompt: string, response: ChatApiResponse][]>([]);

    const [model, setModel] = useState<string>("chat_35");
    const [source, setSource] = useState<string>("gptchat");
    const [loading, setLoading] = useState<boolean>(false);

    const history = answers
        .map(([prompt, response]) => [
            { role: "user", content: prompt },
            { role: "assistant", content: response.answer },
        ])
        .flat();

    const makeApiRequest = async (question: string) => {
        setIsDisabled(true);
        setLoading(true);
        //set userInput as last question
        lastQuestionRef.current = question;

        setAnswers([...answers, [question, { answer: "Loading...", followUpQs: [], references: [] }]]);

        try {
            const request: ChatRequest = {
                prompt: question,
                history: history,
                options: {
                    model: model,
                    source: source,
                    temperature: "",
                    maxTokens: "",
                },
                stop: [],
            };

            const response: ChatApiResponse = await httpClient.post(
                `${window.ENV.API_URL}/api/Chat/Completion`,
                request
            );

            console.log("response", response);
            setLoading(false);

            try {
                if (!response || !response.answer) {
                    console.log("No response body");
                    return;
                } else {
                    //add question and response to answers - set 'Loading...' to response, then remove it and add the actual response
                    setAnswers((answers) =>
                        answers.map((item, index) => (index === answers.length - 1 ? [question, response] : item))
                    );
                }
            } catch (error) {
                console.error("Error parsing response body:", error);
            }
        } catch (error) {
            setError(error);
        } finally {
            setIsLoading(false);
        }
    };

    const clearChat = () => {
        lastQuestionRef.current = "";
        setAnswers([]);
    };

    const handleModelChange = (model: string) => {
        setModel(model);
    };

    const handleSourceChange = (source: string) => {
        setSource(source);
    };

    const handleFollowUpQuestion = async (question: string) => {
        await makeApiRequest(question);
    };

    return (
        <div className="">
            <div className="max-h-[50vh] min-h-[50vh] overflow-auto ">
                <OptionsPanel
                    onModelChange={handleModelChange}
                    onSourceChange={handleSourceChange}
                    disabled={isDisbaled}
                />

                {answers.map(([prompt, response], index) => (
                    <div key={index}>
                        <UserChatMessage prompt={prompt} />
                        <div className="mb-10 flex">
                            <Answer loading={index === answers.length - 1 && loading} answer={response} onFollowUpQuestion={handleFollowUpQuestion} />
                        </div>
                    </div>
                ))}
            </div>

            <div className="flex justify-start">
                <div className="ml-5 mt-20 pt-6">
                    <div className="group">
                        <Button
                            className="flex h-12 w-auto items-center justify-center"
                            shape="circular"
                            icon={<ChatAdd24Regular />}
                            onClick={() => {
                                clearChat();
                                setIsDisabled(false);
                            }}
                        >
                            <span className="whitespace-nowrap">New Topic</span>
                        </Button>
                    </div>
                </div>

                <div className="mb-20 ml-2 mr-10 mt-20 w-full pt-6">
                    <ChatInput
                        onSend={(question) => makeApiRequest(question)}
                        disabled={isLoading}
                        placeholder="Type your message here.."
                        clearOnSend
                    />
                </div>
            </div>
        </div>
    );
}
