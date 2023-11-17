import { Header } from "../../components/header/header";
import { HeaderBar, NavLocation } from "../../components/headerBar/headerBar";
import { ChatHeaderTabs } from "../../components/chat/ChatHeaderTabs";
import { ChatRoom } from "../../components/chat/ChatRoom";

export function ChatWindow() {

    return (
        <div className="w-full">
            <Header className="flex flex-col justify-between bg-contain bg-right-bottom bg-no-repeat" size={"medium"}>
                <div className="-ml-8">
                    <HeaderBar location={NavLocation.Home} />
                </div>
            </Header>

            <main className="min-w-[100vh] bg-[#f2f2f2] ">
                <div className="grid grid-cols-7">
                    <div className="col-span-2 col-start-2">
                        <ChatHeaderTabs />
                    </div>
                    <div className="col-span-5 col-start-2 ml-8 mt-14 h-full">
                        <div className="flex h-full flex-col overflow-auto">
                            <div className="">
                                <ChatRoom />
                            </div>
                        </div>
                    </div>
                </div>
            </main>
        </div>
    );
}
