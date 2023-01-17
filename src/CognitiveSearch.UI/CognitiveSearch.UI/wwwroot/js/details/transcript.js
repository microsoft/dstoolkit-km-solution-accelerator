// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// TRANSCRIPT

Microsoft.Search = Microsoft.Search || {};
Microsoft.Search.Results = Microsoft.Search.Results || {};
Microsoft.Search.Results.Transcript = Microsoft.Search.Results.Transcript || {};
Microsoft.Search.Results.Transcript = {

    // All functions about Transcript 

    RenderTranscriptHTML: function (result, tabular, targetid="#transcript-viewer") {

        var transcriptContainerHTML = '';

        var full_content = "";

        // If we have merged content, let's use it.
        if (result.merged_content) {
            if (result.merged_content.length > 0) {
                full_content = this.RenderTranscript(result.merged_content);
            }
        }
        else {
            if (result.content) {
                if (result.content.length > 0) {
                    full_content = this.RenderTranscript(result.content);
                }
            }
        }

        if (full_content === null || full_content === "") {
            // not much to display
            return '';
        }

        var full_translated_text = "";
        if (!!result.translated_text && result.translated_text !== null && result.language !== "en") {
            if (result.translated_text.length > 0) {
                full_translated_text = this.RenderTranscript(result.translated_text);
            }
        }

        if (full_translated_text.length > 0) {
            transcriptContainerHTML += '<div style="overflow-x:initial;"><table class="table"><thead><tr><th>Original Content ('+result.language+')</th><th>Translated ('+result.translated_language+')</th></tr></thead>';
            transcriptContainerHTML += '<tbody>';
            transcriptContainerHTML += '<tr><td class="wrapword text-break" style="width:50%"><div id="transcript-viewer-pre">' + full_content + '</div></td><td class="wrapword text-break"><div id="translated-transcript-viewer-pre">' + full_translated_text + '</div></td></tr>';
            transcriptContainerHTML += '</tbody>';
            transcriptContainerHTML += '</table></div>';
        }
        else {
            transcriptContainerHTML += '<div style="overflow-x:initial;"><table class="table"><thead><tr><th>Original Content ('+result.language+')</th></tr></thead>';
            transcriptContainerHTML += '<tbody>';
            transcriptContainerHTML += '<tr><td class="wrapword text-break"><div id="transcript-viewer-pre">' + full_content + '</div></td></tr>';
            transcriptContainerHTML += '</tbody>';
            transcriptContainerHTML += '</table></div>';
        }

        return transcriptContainerHTML;
    },

    RenderTranscript: function (content) {
        var full_content = '';
        var lines = content.trim().split('\n');
        lines.forEach((line, idx) => {
            var classname = "p-highlight-" + idx;
            full_content += '<p class="p-highlight ' + classname + ' text-break" onmouseover="Microsoft.Results.Details.pMouseOver(\'' + classname + '\');" onmouseout="Microsoft.Results.Details.pMouseOut(\'' + classname + '\');" >' + Microsoft.Utils.htmlDecode(line).trim() + '</p>';
        });

        return full_content;
    },

    FindMatches: function (regex, transcriptText) {
        var i = -1;
        var response = transcriptText.replace(regex, function (str) {
            i++;
            var shortname = str.slice(0, 20).replace(/[^a-zA-Z ]/g, " ").replace(new RegExp(" ", 'g'), "_");
            return '<span id=\'' + i + '_' + shortname + '\' class="highlight">' + str + '</span>';
        });

        return { html: response, count: i + 1 };
    },

    GetReferences: function (searchText, targetTranslatedText) {

        if (searchText === '*')
            return;

        var targetTag = '#transcript-viewer-pre';

        var transcriptText;
        var lines = [];
        var ptags = $(targetTag).find('p');

        for (var i = 0; i < ptags.length; i++) {
            lines.push(ptags[i].textContent);
        }

        if (!targetTranslatedText) {
            $('#reference-viewer').empty();
            transcriptText = $(targetTag).text();
        }
        else {
            transcriptText = $(targetTag).html();
        }

        var phraseSearch = searchText;
        //var phraseSearch = searchText.trim();
        //phraseSearch += "\b";
        //phraseSearch = phraseSearch.replace(" ", "\b ");

        // find all matches in transcript
        var regex = new RegExp(phraseSearch, 'gi');

        var response = [];

        // Round #1
        // for each line try to find PhraseSearch reference first
        lines.forEach((line, lineidx) => {
            var line_response = this.FindMatches(regex, line);
            if (line_response.count > 0) response.push(line_response);
        });

        // Round #2
        if (response.count === 0) {
            // for each line try to find Tokens references
            lines.forEach((line, lineidx) => {
                var tokens = searchText.split(' ');
                if (tokens.length > 1) {
                    regex = new RegExp("(" + tokens.join('|') + ")", 'gi');
                    var line_response = this.FindMatches(regex, line);
                    if (line_response.count > 0) response.push(line_response);
                }
            });
        }

        //var response = this.FindMatches(regex, transcriptText);

        //// if the phrase search doesn't return anything let's try tokens
        //if (response.count === 0) {
        //    var tokens = searchText.split(' ');
        //    if (tokens.length > 1) {
        //        regex = new RegExp("(" + tokens.join('|') + ")", 'gi');
        //        response = this.FindMatches(regex, transcriptText);
        //    }
        //}

        // Round #3 
        // If no response found in the original text, go to the translated text
        if (response.count === 0) {
            regex = new RegExp(phraseSearch, 'gi');

            // Do another round on the translated text
            targetTag = '#translated-transcript-viewer-pre';
            transcriptText = $(targetTag).text();
            if (transcriptText.length > 0) {
                lines.forEach((line, lineidx) => {
                    var line_response = this.FindMatches(regex, line);
                    if (line_response.count > 0) response.push(line_response);
                });
            }
        }

        if (response.count > 0) {
            $(targetTag).html(response.html);
        }

        // for each match, select prev 50 and following 50 characters and add selections to list
        var transcriptCopy = transcriptText;

        // Calc height of reference viewer
        var contentHeight = $('.ms-Pivot-content').innerHeight();
        var tagViewerHeight = $('#tag-viewer').innerHeight();
        var detailsViewerHeight = $('#details-viewer').innerHeight();

        $('#reference-viewer').css("height", contentHeight - tagViewerHeight - detailsViewerHeight - 110);

        $.each(transcriptCopy.match(regex), function (index, value) {

            var startIdx;
            var ln = 400;

            if (value.length > 150) {
                startIdx = transcriptCopy.indexOf(value);
                ln = value.length;
            }
            else {
                if (transcriptCopy.indexOf(value) < 200) {
                    startIdx = 0;
                }
                else {
                    startIdx = transcriptCopy.indexOf(value) - 200;
                }

                ln = 400 + value.length;
            }

            var reference = transcriptCopy.substr(startIdx, ln);
            transcriptCopy = transcriptCopy.replace(value, "");

            reference = reference.replace(value, function (str) {
                return '<span class="highlight">' + str + '</span>';
            });

            var shortName = value.slice(0, 20).replace(/[^a-zA-Z ]/g, " ").replace(new RegExp(" ", 'g'), "_");

            $('#reference-viewer').append('<li class=\'reference list-group-item\' onclick=\'Microsoft.Search.Transcript.GoToReference("' + index + '_' + shortName + '")\'>...' + reference + '...</li>');
        });
    },

    GoToReference: function (selector) {

        var triggerEl = document.querySelector('#details-pivot-links a[href="#transcript-pivot"]')

        var tabinstance = bootstrap.Tab.getInstance(triggerEl);

        if (tabinstance) {
            tabinstance.show();
        }
        else {
            var tab = new bootstrap.Tab(triggerEl);
            tab.show();
        }

        var container = $('#transcript-viewer');
        var scrollTo = $("#" + selector);

        container.animate({
            scrollTop: scrollTo.offset().top - container.offset().top + container.scrollTop()
        });
    },

    GetMatches: function (string, regex, index) {
        var matches = [];
        var match;
        while (match === regex.exec(string)) {
            matches.push(match[index]);
        }
        return matches;
    },

    GetSearchReferences: function (q) {
        var copy = q;

        copy = copy.replace(/~\d+/gi, "");
        var matches = this.GetMatches(copy, /\w+/gi, 0);

        matches.forEach(function (match) {
            Microsoft.Search.Transcript.GetReferences(match, true);
        });
    },

    SearchTranscript: function (searchText) {
        $('#reference-viewer').empty();

        if (searchText !== "") {
            // get whole phrase
            Microsoft.Search.Results.Transcript.GetReferences(searchText, false);
        }
    }

}
