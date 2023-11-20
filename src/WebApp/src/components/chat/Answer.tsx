import { Spinner } from "@fluentui/react-components";
import { AskResponse, ChatApiResponse } from "../../api/models";
import { InteractionTag, InteractionTagPrimary } from "@fluentui/react-tags-preview";

interface Props {
    answer: ChatApiResponse;
    loading: boolean;
}

export function Answer({ answer, loading }: Props) {
    return (
        <div className="mr-auto  flex justify-start">
            <div className="ml-5 max-w-[900px] overflow-auto break-words rounded-md bg-[#FFFFFF] px-10 py-5 shadow-sm outline outline-1 outline-transparent">
                {loading && <Spinner />}
                {answer.answer}
                {!loading && answer.followUpQs && <p>Follow Up Questions: {answer.followUpQs}</p>}

                <div className="mt-10">
                    <InteractionTag>
                        <InteractionTagPrimary>
                    {!loading && answer.references &&
                        answer.references.map((reference, index) => <p key={index}>Reference: {reference.chunkId.substring(0, 50)}...</p>)}
                        </InteractionTagPrimary>
                    </InteractionTag>
                </div>
            </div>
        </div>
    );
}
