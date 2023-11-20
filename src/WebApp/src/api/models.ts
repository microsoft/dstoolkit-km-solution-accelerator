
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

export type ChatApiResponse = {
    answer: string;
    followUpQs: string[];
    references: Reference[];
}

export type Reference = {
    name: string;
    page: number;
    parentId: string;
    chunkId: string;
    url: string;
    isAbsoluteUrl: boolean;
};

export type AskResponse = {
    answer: string;
};


