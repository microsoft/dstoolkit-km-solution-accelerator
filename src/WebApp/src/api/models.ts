export type AskRequestOptions = {
    //gptModel?: string;
    //gptTemperature?: number;
    //gptMaxTokens?: number;
};

export type AskResponse = {
    answer: string;
    error?: string | undefined;
};

export type ChatMessage = {
    message: string;
    role?: string;
};

export type ChatRequest = {
    prompt: string;
    history: ChatMessage[];
    overrides?: AskRequestOptions;
};