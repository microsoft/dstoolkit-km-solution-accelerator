import { ChatRequest } from "./models";

interface ChatApiResponse {
    body: string;
    status: number;
    headers: {
        "Content-Type": string;
    };
}

export async function chatApi(options: ChatRequest): Promise<ChatApiResponse> {
    console.log("chatApi")

    return {
        body: "{\"answer\": \"Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea Best, Jae\"}",
        status: 200,
        headers: {
            "Content-Type": "application/json"
        },
    };
    // const url = "www.example.com";
    // return await fetch(url, {
    //     method: "POST",
    //     headers: {
    //         "Content-Type": "application/json"
    //     },
    //     body: JSON.stringify({
    //         history: options.history,
    //         overrides: {
                
    //         }
    //     })
    // });
}