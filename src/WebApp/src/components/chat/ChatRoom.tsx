import { useRef, useState } from "react";
import { ChatInput } from "./ChatInput";
import { AskResponse, ChatRequest, ChatMessage, ChatApiResponse } from "../../api/models";
import { chatApi } from "../../api/api";
import { UserChatMessage } from "./UserChatMessage";
import { Answer } from "./Answer";
import { OptionsPanel } from "./OptionsPanel";
import { httpClient } from "../../utils/httpClient/httpClient";

export function ChatRoom() {
    const lastQuestionRef = useRef<string>("");

    const [isLoading, setIsLoading] = useState<boolean>(false);
    const [error, setError] = useState<unknown>();
    const [answers, setAnswers] = useState<[prompt: string, response: AskResponse][]>([]);

    const [model, setModel] = useState<string>("chat_35");
    const [source, setSource] = useState<string>("rag_wor");

    const makeApiRequest = async (question: string) => {
        //set userInput as last question
        lastQuestionRef.current = question;

        try {
            // const history: ChatMessage[] = answers.map(a => ({ role: a[0], content: a[1].answer }));
            const request: ChatRequest = {
                prompt: question,
                history: [],
                options: {
                    model: model,
                    source: source,
                    temperature: "",
                    maxTokens: "",
                },
                stop: [],
            };

            console.log(`${window.ENV.API_URL}/api/Chat/Completion`);
            console.log("request: ", JSON.stringify(request));

            const response: string = await httpClient.post(
                `${window.ENV.API_URL}/api/Chat/Completion`,
                request
            );

            // const response: ChatApiResponse = await chatApi(request);

            console.log("response: ", response);

            try {
                if (!response) {
                    console.log("No response body");
                    return;
                } else {
                    const parsedResponse: AskResponse = JSON.parse(response);
                    console.log("question:", question);
                    console.log("parsedResponse:", parsedResponse);
                    // if (response.status != 200) {
                    //     throw Error(parsedResponse.error || "Unknown error");
                    // }
                    //add question and response to answers
                    setAnswers([...answers, [question, parsedResponse]]);
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


    return (
        <div>
            {!lastQuestionRef.current ? (
                <div className="min-h-[50vh]">
                    <OptionsPanel onModelChange={handleModelChange} onSourceChange={handleSourceChange} />
                </div>
            ) : (
                <div className="max-h-[50vh] min-h-[50vh] overflow-auto">
                    {answers.map((answer, index) => (
                        <div key={index}>
                            <UserChatMessage prompt={lastQuestionRef.current} />
                            <div className="mb-10 flex">
                                <Answer answer={answer[1]} />
                            </div>
                        </div>
                    ))}
                </div>
            )}

            <div className="mb-20 ml-20 mr-20 mt-20 pt-6">
                <ChatInput
                    onSend={(question) => makeApiRequest(question)}
                    disabled={isLoading}
                    placeholder="Type your message here.."
                    clearOnSend
                />
            </div>
        </div>
    );
}
