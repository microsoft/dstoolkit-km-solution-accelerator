
export type AskRequestOptions = {
    model?: string;
    source?: string;
    temperature?: string;
    maxTokens?: string;
};

export type ChatMessage = {
    role?: string;
    content: string;
};

export type ChatRequest = {
    prompt: string;
    history: ChatMessage[];
    options?: AskRequestOptions;
    stop?: string[];
};

export type ChatApiResponse {
    answer: string;
    // body: string;
    // status: number;
    // headers: {
    //     "Content-Type": string;
    // };
}

export type AskResponse = {
    answer: string;
};
