interface Props {
    prompt: string;
}

export function UserChatMessage({ prompt }: Props) {
    return (
        <div className="flex justify-end mb-10">
            <div className="mr-5 rounded-md bg-[#e8ebfa] px-10 py-5 shadow-sm outline outline-1 outline-transparent max-w-[900px] overflow-auto break-words">
                {prompt}
            </div>
        </div>
    );
}
