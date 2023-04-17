// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// Open AI Chat

Microsoft.Search = Microsoft.Search || {};
Microsoft.Search.Results = Microsoft.Search.Results || {};
Microsoft.Search.Results.Chat = Microsoft.Search.Results.Chat || {};
Microsoft.Search.Results.Chat = {

    //chatContentsTag: "#chat-contents",
    result: null,

    GetTargetChatInput: function (targetid) {
        return targetid + "-chat-input-text";
    },

    GetChatPromptContent: function (targetid) {
        return targetid + "-chat-prompt-content";
    },

    GetTargetChatContent: function (targetid) {
        return targetid + "-chat-contents";
    },

    DocumentChat: function (targetid, prompt = '', promptHtml = true) {
        Microsoft.Search.Results.Chat.Chat(targetid, true, false, prompt, promptHtml);
    },

    TableChat: function (targetid, prompt = '', promptHtml = true) {
        Microsoft.Search.Results.Chat.Chat(targetid, false, true, prompt, promptHtml);
    },

    Chat: function (targetid, chatdocument, chattable, prompt = '', promptHtml = true) {

        if (prompt && prompt.length > 0) {
            prompt.trim();
        }
        else {
            prompt = $("#" +this.GetTargetChatInput(targetid)).val().trim();
        }

        if (prompt && prompt.length > 0) {

            // Retrieve the chat history 
            var chathistory=[];
            var histElts = $(".chat-history")
            for (let index = 0; index < histElts.length; index++) {
                const element = histElts[index];
                chathistory.push({ "role":element["role"], "content":element.getInnerHTML() });
            }
            chathistory = chathistory.reverse();

            if (promptHtml) {
                var chatHTML = '';
                chatHTML += '<div class="d-flex align-items-center border-start border-end border-bottom border-1 border-secondary rounded p-1 mt-2">';
                chatHTML += '   <div class="col-auto text-left pr-1"><img src="/icons/bi-person-square.svg" width="32"/></div>';
                chatHTML += '   <p class="col ms-2 msg chat-history" role="user">' + prompt + '</p>';
                // chatHTML += '   <div class="col-auto d-flex justify-content-end align-content-center text-center ml-2">';
                // chatHTML += '       <button class="btn btn-outline-info mb-3 bi bi-arrow-clockwise" onclick="Microsoft.Search.Results.Chat.Resend(\'' + prompt + '\');"></button>';
                // chatHTML += '   </div>';
                chatHTML += '</div>';

                $("#" + this.GetTargetChatContent(targetid)).prepend(chatHTML);
            }

            if (chatdocument) {
                prompt = "from the below text, " + prompt;

                if (this.result) {
                    if (this.result.translated_text && this.result.translated_text.length > 0) {
                        prompt += "\n" + this.result.translated_text.substring(0, 4000) + "\n" +Microsoft.OpenAI.StopSequence;
                    }
                    else {
                        prompt += "\n" + this.result.content.substring(0, 4000) + "\n" +Microsoft.OpenAI.StopSequence;
                    }
                }
            } else {
                if (chattable) {
                    prompt = "With the below CSV formatted data separated by |, " + prompt;
                    var tablecontent = $("#" + this.GetChatPromptContent(targetid)).html().trim();
                    if (tablecontent) {
                        if (tablecontent.length > 0) {
                            prompt += "\n" + tablecontent + "\n" + Microsoft.OpenAI.StopSequence;
                        }
                    }
                }
            }

            prompt += "\n" + Microsoft.OpenAI.StopSequence;
            
            Microsoft.OpenAI.Complete(prompt, chathistory, "Microsoft.Search.Results.Chat.Response", targetid);

            $("#" +this.GetTargetChatInput(targetid)).val('');
        }
    },

    Response: function (response, targetid) {

        var chatHTML = '';
        chatHTML += '<div class="d-flex align-items-center text-right justify-content-end border-start border-end border-top border-1 border-warning rounded p-1 mt-2">';
        chatHTML += '<div class="">';

        if (response.values.length > 0) {
            var choices = response.values[0].data.response.choices;
            for (let index = 0; index < choices.length; index++) {
                const element = choices[index];
                if (element.message) {
                    chatHTML += '<p class="msg me-2 chat-history" role="assistant">' + element.message.content + '</p>';
                }
            }
        }
        else {
            chatHTML += '<p class="msg"> I wasn\'t able to find any answer to your request...</p>';
        }
        chatHTML += '</div>';
        chatHTML += '<div><img src="/images/logos/ChatGPT.svg" width="32px" class="img1"/></div>';
        chatHTML += '</div>';

        $("#"+this.GetTargetChatContent(targetid)).prepend(chatHTML);
    },

    render_chat_view: function (targetid = "results-container", placeHolder ="Ask me anything...", method="Chat") {

        var containerHTML = '';

        containerHTML += '<div class="d-flex justify-content-center row mt-3 mb-2">';

        // Chat Input
        containerHTML += '<div class="col">';
        containerHTML += '    <textarea id="' + this.GetTargetChatInput(targetid) + '" type="text number" name="text" class="form-control" placeholder="' + placeHolder + '"></textarea>';
        containerHTML += '</div>';

        containerHTML += '<div class="col-auto d-flex justify-content-end align-content-center text-center ml-2">';
        containerHTML += '    <button class="btn btn-secondary mb-3 bi bi-send" onclick="Microsoft.Search.Results.Chat.' + method + '(\'' + targetid + '\',false);"></button>';
        containerHTML += '</div>';

        // Chat Responses 
        containerHTML += '<div id="' + this.GetTargetChatContent(targetid) + '" class="px-2 scroll">';
        containerHTML += '</div>';

        containerHTML += '</div>';

        $("#" + targetid).html(containerHTML);

        return containerHTML;

    },
    render_tab: function (result, tabular, targetid = "chat-viewer", placeHolder = "Ask your document anything...") {

        // Persist the entire document
        this.result = result;

        if (targetid.startsWith('#')) {
            targetid = targetid.substring(1);
        }

        $("#" + targetid).empty();

        var containerHTML = '';

        containerHTML += '<div class="d-flex justify-content-center container mt-5">';
        containerHTML += '<div class="row">';

        containerHTML += '<div class="col">';
        containerHTML += Microsoft.Search.Results.Transcript.RenderTranscriptHTML(result);
        containerHTML += '</div>';

        containerHTML += '<div class="col">';

        // Chat Input
        containerHTML += '<div class="container">';
        containerHTML += '<div class="row">';

        // Chat Actions
        containerHTML += '<div class="d-flex">';
        containerHTML += '    <button class="btn btn-sm btn-outline-secondary mb-2 me-2" onclick="Microsoft.Search.Results.Chat.Summarize(\'' + targetid +'\');">Summarize</button>';
        containerHTML += '    <button class="btn btn-sm btn-outline-secondary mb-2 me-2" onclick="Microsoft.Search.Results.Chat.ExtractEntities(\'' + targetid +'\');">Extract Entities</button>';
        containerHTML += '    <button class="btn btn-sm btn-outline-secondary mb-2 me-2" onclick="Microsoft.Search.Results.Chat.ExtractTimeline(\'' + targetid +'\');">Extract Timeline</button>';
        containerHTML += '</div>';

        containerHTML += '<div class="col">';
        containerHTML += '    <input id="' + this.GetTargetChatInput(targetid) +'" type="text number" name="text" class="form-control" placeholder="'+placeHolder+'">';
        containerHTML += '</div>';

        containerHTML += '<div class="col-auto d-flex justify-content-end align-content-center text-center ml-2">';
        containerHTML += '    <button class="btn btn-warning mb-3 bi bi-send" onclick="Microsoft.Search.Results.Chat.Chat(\''+targetid+'\',true,false);"></button>';
        containerHTML += '</div>';

        containerHTML += '</div>';
        containerHTML += '</div>';

        // Chat Responses 
        containerHTML += '<div id="' + this.GetTargetChatContent(targetid) + '" class="px-2 scroll">';
        containerHTML += '</div>';

        containerHTML += '</div>';

        // containerHTML += '</div>';
        containerHTML += '</div>';
        containerHTML += '</div>';

        $("#" + targetid).html(containerHTML);

        return containerHTML;
    },

    Summarize: function (targetid = "chat-viewer") {
        this.Chat(targetid,true,false,"summarize")
    },

    ExtractEntities: function (targetid = "chat-viewer") {
        this.Chat(targetid,true,false,"extract named entities")
    },

    ExtractTimeline: function (targetid = "chat-viewer") {
        this.Chat(targetid,true,false,"extract timeline")
    }
}
