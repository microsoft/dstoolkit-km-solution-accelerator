import { AskResponse } from "../../api/models";

interface Props {
    answer: AskResponse;
}

export function Answer({ answer }: Props) {

    return (
        <div className="mr-auto  flex justify-start">
            <div className="rounded-md bg-[#FFFFFF] px-10 py-5 ml-5 shadow-sm outline outline-1 outline-transparent max-w-[900px] overflow-auto break-words">
                {answer.answer}
            </div>
        </div>
    );
}
