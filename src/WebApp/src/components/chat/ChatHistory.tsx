import { makeStyles, shorthands, tokens } from '@fluentui/react-components';
import { IChatMessage } from '../../types/chatMessage';
import { ChatHistoryItem } from './ChatHistoryItem';


const useClasses = makeStyles({
    root: {
        ...shorthands.gap(tokens.spacingVerticalM),
        display: 'flex',
        flexDirection: 'column',
        maxWidth: '900px',
        width: '100%',
        justifySelf: 'center',
    },
    item: {
        display: 'flex',
        flexDirection: 'column',
    },
});

interface ChatHistoryProps {
    messages: IChatMessage[];
}

export function ChatHistory({messages}: ChatHistoryProps) {
    const classes = useClasses();


    return (

        <div className={classes.root}>
            {messages.map((message, index) => (
                <ChatHistoryItem key={message.timestamp} message={message} messageIndex={index} />
            ))}
        </div>


    );

}