// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

//
// OPEN AI - COMPLETION 
//
Microsoft.OpenAI = Microsoft.OpenAI || {};
Microsoft.OpenAI = {

    StopSequence: "|||||",

    Complete: function (prompt, history, target_method, targetid) {

        $.postAPIJSON('/api/openai/chat',
            {
                "prompt": prompt,
                "history": history,
                "stop":[Microsoft.OpenAI.StopSequence]
            },
            function (data) {
                Microsoft.Utils.executeFunctionByName(target_method, window, JSON.parse(data), targetid);
            });
    }
}

// export default Microsoft.OpenAI;