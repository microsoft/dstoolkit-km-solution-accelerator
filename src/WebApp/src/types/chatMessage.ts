export enum AuthorRoles {
    // The current user of the chat.
    User = 0,
    // The bot.
    Bot,
}

export interface IChatMessage {
    chatId: string;
    timestamp: number;
    userName: string;
    userId: string;
    content: string;
    id?: string;
    sessionId?: string;
    authorRole: AuthorRoles;
}