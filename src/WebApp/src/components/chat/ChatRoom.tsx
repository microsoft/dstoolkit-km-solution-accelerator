import { useRef, useState } from "react";
import { ChatInput } from "./ChatInput";
import { AskResponse, ChatRequest, ChatMessage } from "../../api/models";
import { chatApi } from "../../api/api";
import { UserChatMessage } from "./UserChatMessage";
import { Answer } from "./Answer";
import { OptionsPanel } from "./OptionsPanel";

export function ChatRoom() {

    const lastQuestionRef = useRef<string>("");

    const [isLoading, setIsLoading] = useState<boolean>(false);
    const [error, setError] = useState<unknown>();
    const [answers, setAnswers] = useState<[prompt: string, response: AskResponse][]>([]);

    const [gptModel, setGptModel] = useState<string>("gpt3");
    const [resourceTarget, setResourceTarget] = useState<string>("LLM");

    const makeApiRequest = async (question: string) => {
        lastQuestionRef.current = question;

        try{
            
            const history: ChatMessage[] = answers.map(a => ({ message: a[0], response: a[1].answer }));
            const request: ChatRequest = { 
                prompt: question,
                history: [...history, { message: question, role: undefined }],
                options: {
                    
                }
            }

            const response = await chatApi(request);

            try{
                if (!response.body) {
                    console.log("No response body");
                    return;
                } else {
                    const parsedResponse = JSON.parse(response.body);
                    console.log("question:", question)
                    console.log("parsedResponse:", parsedResponse);
                    if(response.status != 200){throw Error(parsedResponse.error || "Unknown error")}
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
    }

    const clearChat = () => {
        lastQuestionRef.current = "";
        setAnswers([]);
    }


    return (
        <div>

            {!lastQuestionRef.current ? (
                <div className="min-h-[50vh]">
                    <OptionsPanel />
                </div>
            ) : (
                <div className="overflow-auto min-h-[50vh] max-h-[50vh]">
                
                    {answers.map((answer, index) => (
                        <div key={index}>
                            <UserChatMessage prompt={lastQuestionRef.current} />
                            <div className="flex mb-10">
                                <Answer answer={answer[1]} />
                            </div>
                        </div>
                    ))}

            </div>
            )}

            

            <div className="pt-6 ml-20 mr-20 mb-20 mt-20">
                <ChatInput 
                    onSend={question => makeApiRequest(question)}
                    disabled={isLoading}
                    placeholder="Type your message here.."
                    clearOnSend
                />
            </div>

        </div>

    )

};