import { httpClient } from "../utils/httpClient/httpClient";
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
    console.log(options)

    return {
        body: "{\"answer\":\"Bob Marley is not mentioned in the content provided. \\n\\nFollow up Questions:\\n1. What does the torque data in Figure 12 reveal?\\n2. What issue was encountered with the Geoff mixer in module 2?\\n3. What is the purpose of the salt curve samples in this study? \\n\\nchunk_id: 4373ef0a596f_aHR0cHM6Ly9rbXVubHZyN2RhdGEuYmxvYi5jb3JlLndpbmRvd3MubmV0L2RvY3VtZW50cy9VTDIwMTcwMTU3LnBkZg2_chunks_25\"}\n",
        status: 200,
        headers: {
            "Content-Type": "application/json-patch+json"
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