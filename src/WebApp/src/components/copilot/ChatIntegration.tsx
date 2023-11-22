import {
    AttachmentMenu,
    AttachmentMenuTrigger,
    CopilotProvider,
    OutputCard,
    PromptStarter,
    Suggestion,
    SuggestionList,
} from "@fluentai/react-copilot";
import {
    makeStyles,
    tokens,
    shorthands,
    Body1,
    Image,
    Subtitle2,
    Button,
    MenuButton,
} from "@fluentui/react-components";
import { Chat, ChatMessage, ChatMyMessage } from "@fluentui-contrib/react-chat";
import { useEffect, useRef, useState } from "react";
import { AppFolder16Regular, Attach16Regular, Sparkle16Regular } from "@fluentui/react-icons";
import { Textarea } from "@fluentai/textarea";

const useStyles = makeStyles({
    provider: {
        maxWidth: "900px",
        backgroundColor: tokens.colorNeutralBackground3,
        ...shorthands.padding("16px"),
        display: "flex",
        columnGap: "24px",
        flexDirection: "column",
        height: "600px",
    },
    header: {
        backgroundColor: tokens.colorNeutralBackground3,
        height: "48px",
        width: "100%",
    },
    chat: {
        backgroundColor: tokens.colorNeutralBackground3,
        ...shorthands.padding(0, "16px", "16px"),
        overflowY: "scroll",
        height: "100%",
        "&::-webkit-scrollbar": {
            width: tokens.spacingHorizontalS,
        },
    },
    card: {
        rowGap: tokens.spacingHorizontalM,
        backgroundColor: tokens.colorNeutralBackground1,
    },
    chatMessage: {
        display: "block",
        marginLeft: 0,
    },
    chatMessageBody: {
        backgroundColor: tokens.colorNeutralBackground1,
        boxShadow: tokens.shadow4,
        boxSizing: "content-box",
        display: "block",
        maxWidth: "100%",
    },
    chatMyMessage: {
        gridTemplateAreas: "unset",
        marginLeft: 0,
    },
    chatMyMessageBody: {
        backgroundColor: "#E0E7FF",
    },
    inputArea: {
        paddingTop: "16px",
    },
    prompts: {},
    promptHighlight: {},
});

export const ChatIntegration = () => {
    const [loadingState, setLoadingState] = useState<"latency" | "loading" | "done" | undefined>(undefined);
    const [text, setText] = useState<string | undefined>("");
    const [latencyMessage, setLatencyMessage] = useState<string>("");
    const [cardContent, setCardContent] = useState<React.ReactNode | undefined>(undefined);

    const scrollDiv = useRef<HTMLDivElement>(null);
    useEffect(() => {
        scrollDiv.current?.scrollTo({ top: scrollDiv.current.scrollHeight });
    });

    const menuButtonRef = useRef<HTMLButtonElement>(null);

    const handleReload = (e: React.MouseEvent<HTMLButtonElement>) => {
        console.log("Reload");
    };

    const handleSubmit = () => {
        setText("");
        setCardContent("");
        setLatencyMessage("Reading emails");
        setLoadingState("latency");
        setTimeout(() => {
            setLatencyMessage("Thinking about it...");
        }, 1500);
        setTimeout(() => {
            setLatencyMessage("Almost there...");
        }, 3000);
        setTimeout(() => {
            setLoadingState("loading");
        }, 6000);
    };

    const styles = useStyles();

    return (
        <>
            <div className={styles.header}>
                <div className="mb-4 ml-4 mr-4 mt-4 flex h-8 ">
                    <Image className="" src="\img\Copilot.png" fit="center" />
                    <Subtitle2 className="ml-1 mt-1">Copilot</Subtitle2>
                </div>
            </div>

            <CopilotProvider className={styles.provider} mode="sidecar">
                <Chat ref={scrollDiv} className={styles.chat}>
                    <OutputCard className={styles.card}>
                        <Body1>Hi Kat,</Body1>

                        <Body1>Ready to explore? Select one of the suggestions below to get started...</Body1>

                        <div className={styles.prompts}>
                            <PromptStarter
                                icon={<AppFolder16Regular />}
                                category="Summarize"
                                prompt={
                                    <Body1>
                                        Review key points in <span className={styles.promptHighlight}>file</span>
                                    </Body1>
                                }
                            />

                            <PromptStarter
                                icon={<AppFolder16Regular />}
                                category="Create"
                                prompt={<Body1>Write more about...</Body1>}
                            />

                            <PromptStarter
                                icon={<AppFolder16Regular />}
                                category="Ask"
                                prompt={<Body1>Tell me about my day</Body1>}
                                badge="NEW"
                            />
                        </div>
                        <Body1>
                            You can use the prompt guide for suggestions by selecting this button <Sparkle16Regular />
                        </Body1>
                    </OutputCard>
                    <ChatMyMessage
                        body={{ className: styles.chatMyMessageBody }}
                        root={{ className: styles.chatMyMessage }}
                    >
                        Tell me about my day
                    </ChatMyMessage>
                    <ChatMessage body={{ className: styles.chatMessageBody }} root={{ className: styles.chatMessage }}>
                        You have 2 new messages from Chris, and 3 meetings today
                    </ChatMessage>
                </Chat>

                <div className={styles.inputArea}>
                    <SuggestionList reload={{ onClick: handleReload }}>
                        <Suggestion onClick={handleSubmit}>Catch me up on the meeting I missed this morning</Suggestion>
                        <Suggestion>WHat are the OKRs this quarter</Suggestion>
                    </SuggestionList>
                    <Textarea
                        aria-label="Copilot Chat"
                        placeholder="Ask a question or request, or type '/' for suggestions"
                        contentAfter={
                            <>
                                <Button
                                    aria-label="Copilot guide"
                                    appearance="transparent"
                                    icon={<Sparkle16Regular />}
                                />
                            </>
                        }
                    />
                </div>
            </CopilotProvider>
        </>
    );
};
