import { makeStyles, mergeClasses, shorthands } from "@fluentui/react-components";
import { AuthorRoles, IChatMessage } from "../../types/chatMessage";
import { customTokens } from "../../styles";
import { getActiveUser } from "../../utils/auth/roles";
import { AccountInfo } from "@azure/msal-browser";
import { useMsal } from "@azure/msal-react";

const useClasses = makeStyles({
    root: {
        display: 'flex',
        flexDirection: 'row',
        maxWidth: '75%',
        ...shorthands.borderRadius(customTokens.borderRadiusMedium),
        // ...Breakpoints.small({
        //     maxWidth: '100%',
        // }),
        ...shorthands.gap(customTokens.spacingHorizontalXS),
    },
    alignEnd: {
        alignSelf: 'flex-end',
    },
    item: {
        backgroundColor: customTokens.colorNeutralBackground1,
        ...shorthands.borderRadius(customTokens.borderRadiusMedium),
        ...shorthands.padding(customTokens.spacingVerticalS, customTokens.spacingHorizontalL),
    },
    me: {
        backgroundColor: customTokens.colorMeBackground,
    },

});

interface ChatHistoryItemProps {
    message: IChatMessage;
    messageIndex: number;
}

export function ChatHistoryItem({ message, messageIndex }: ChatHistoryItemProps) {
    const classes = useClasses();
    const { accounts } = useMsal();

    const activeUserInfo = getActiveUser(accounts);

    //What is the userId going to be? Will it be a property from the users AccountInfo object?
    const isMe = (message.authorRole === AuthorRoles.User && message.userId === activeUserInfo?.localAccountId);
    const isBot = message.authorRole === AuthorRoles.Bot;

    return (
        <div className={isMe ? mergeClasses(classes.root, classes.alignEnd) : classes.root}>
            <div className={isMe ? mergeClasses(classes.item, classes.me) : classes.item}>
                {message.content}
            </div>
        </div>


    )

}