import { Spinner } from "@fluentui/react-components";
import { ChatApiResponse } from "../../api/models";
import { InteractionTag, InteractionTagPrimary } from "@fluentui/react-tags-preview";

interface AnswerProps {
    answer: ChatApiResponse;
    loading: boolean;
    onFollowUpQuestion: (question: string) => void;
}

export function Answer({ answer, loading, onFollowUpQuestion }: AnswerProps) {

    const onFollowUpQClick = (question: string): void => {
        onFollowUpQuestion(question);
    }

    return (
        <div className="mr-auto  flex justify-start">
            <div className="ml-5 max-w-[900px] overflow-auto break-words rounded-md bg-[#FFFFFF] px-10 py-5 shadow-sm outline outline-1 outline-transparent">
                {loading && <Spinner />}
                {answer.answer}

                {/* <div>
                    {!loading && answer.followUpQs && answer.followUpQs.length > 0 && <div>Follow Up Questions: {answer.followUpQs.map(txt => <p>{txt}</p>)}</div>}
                </div> */}

                <div className="mt-5">
                    {answer.references.map((reference, index) => (
                        <InteractionTag size="small">
                            <InteractionTagPrimary>
                                <p key={index}>
                                    [{index + 1}] {reference.chunkId.substring(0, 80)}...
                                </p>
                            </InteractionTagPrimary>
                        </InteractionTag>
                    ))}
                </div>

                <div className="mt-5">
                    {answer.followUpQs.map((question, index) => (
                        <InteractionTag size="small" onClick={() => onFollowUpQClick(question)}>
                            <p className="mt-2">
                                <InteractionTagPrimary>
                                    <p key={index}>{question}</p>
                                </InteractionTagPrimary>
                            </p>
                        </InteractionTag>
                    ))}
                </div>
            </div>
        </div>
    );
}
