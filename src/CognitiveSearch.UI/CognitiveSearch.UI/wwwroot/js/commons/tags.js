// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

//
// TAGS
//
Microsoft.Tags = Microsoft.Tags || {};
Microsoft.Tags = {
    MaxTagsToDisplay: 5,
    MaxTagValueLengthToDisplay: 50,
    load: function (data) {
        this.tags = data;
    },
    renderTagsAsTable: function (result, limitTagsToDisplay, highlights, selectTags = []) {
        var tagsHTML = this.renderCoreTags(result, limitTagsToDisplay, highlights, selectTags, '<tr>', '</tr>', 'td');

        if (tagsHTML.length > 0) {
            return "<table class='tabletags'>" + tagsHTML + "</table>";
        }
        else {
            return tagsHTML;
        }
    },

    renderTagsAsList: function (result, limitTagsToDisplay, highlights, selectTags = [], title) {

        var tagsHTML = this.renderCoreTags(result, limitTagsToDisplay, highlights, selectTags, '', '', 'div');

        if (tagsHTML.length > 0) {
            var outHtml = "<div class='col-md-12 mt-1 d-flex align-items-center'>";
            if (title != null) {
                outHtml += '<span class="modification-line-label me-2 text-decoration-underline" title="'+title+'">'+title+'</span>';
            }
            outHtml += tagsHTML;
            outHtml += "</div>";

            return outHtml;
        }
        else {
            return tagsHTML;
        }
    },

    renderTagsAsRow: function (result, limitTagsToDisplay, highlights, selectTags = []) {
        var tagsHTML = this.renderCoreTags(result, limitTagsToDisplay, highlights, selectTags, '', '', 'div');

        if (tagsHTML.length > 0) {
            return "<div class='col-md-12 mt-1 d-flex'>" + tagsHTML + "</div>";
        }
        else {
            return tagsHTML;
        }
    },

    renderCoreTags: function (result, limitTagsToDisplay, highlights, selectTags = [], startTagGroup, endTagGroup, htmlTagElt) {

        var tagsHTML = '';
        var isDocument = ! result.document_embedded;

        if (Microsoft.View.config.tags) {

            // var targeted_tags = Array.from(Object.keys(Microsoft.Facets.Facets));
            var targeted_tags = Microsoft.View.config.tags.split(',');

            if (selectTags.length > 0) {
                targeted_tags = selectTags;
            }

            for (var item in targeted_tags) {

                var name = targeted_tags[item];

                var displayName = Microsoft.Utils.GetFacetDisplayTitle(name);
                var dedupedEntities = [];

                var tagEntry = result; 

                // Support for complex type facets...
                var tokTag = name.split('/');
                for (let index = 0; index < tokTag.length; index++) {
                    if ( tagEntry) {
                        tagEntry = tagEntry[tokTag[index]];
                    }
                }

                if (tagEntry) {
                    if (tagEntry.length > 0) {
                        if ((name.indexOf("image") === 0) && isDocument) {
                            //do nothing
                        }
                        else {
                            tagsHTML += startTagGroup;

                            if (Array.isArray(tagEntry)) {

                                if ((tagEntry.length > 0) && (tagEntry[0].length > 0)) {

                                    tagsHTML += "<" + htmlTagElt +" class='tdcolumn tdcolumn-" + Microsoft.Utils.jqclass(name) + "'>";

                                    tagEntry.forEach(function (tagValue, i, tagArray) {
                                        var eligible = true;

                                        if (limitTagsToDisplay && i > Microsoft.Tags.MaxTagsToDisplay) {
                                            eligible = false;
                                        }

                                        if (eligible) {
                                            if (tagValue.length > 0) {
                                                if ($.inArray(tagValue, dedupedEntities) === -1) { //! in array
                                                    dedupedEntities.push(tagValue);
                                                    var tagDisplayValue = tagValue;
                                                    if (tagValue.length > Microsoft.Tags.MaxTagValueLengthToDisplay) { // check tag name length
                                                        // create substring of tag name length if too long
                                                        tagDisplayValue = tagValue.substring(0, Microsoft.Tags.MaxTagValueLengthToDisplay) + '...';
                                                    }

                                                    var tagclasses = "tag tag-" + Microsoft.Utils.jqclass(name);

                                                    //if (highlights) {
                                                    //    if (highlights[name]) {
                                                    //        for (var i = 0; i < highlights[name].length; i++) {
                                                    //            var tagh = highlights[name][i].split('<span class="highlight">').join('');
                                                    //            tagh = tagh.split('</span>').join('');
                                                    //            if (tagValue.indexOf(tagh) > -1) {
                                                    //                tagclasses += " tag-highlighted";
                                                    //            }
                                                    //        }
                                                    //    }
                                                    //}

                                                    if (name === "celebrities") {
                                                        tagsHTML += '<button value="'+ tagValue +'" title="' + displayName + ' : ' + tagValue + '" class="' + tagclasses + '" onclick="Microsoft.Tags.HighlightTag(event)">' + tagDisplayValue + '</button>';
                                                    }
                                                    else {
                                                        tagsHTML += '<button value="'+ tagValue +'" title="' + displayName + ' : ' + tagValue + '" class="' + tagclasses + '" onclick="Microsoft.Tags.FacetTag(event,\'' + name + '\');">' + tagDisplayValue + '</button>';
                                                    }
                                                    i++;
                                                }
                                            }
                                        }
                                    });

                                    if (limitTagsToDisplay && tagEntry.length > Microsoft.Tags.MaxTagsToDisplay) {
                                        tagsHTML += "<strong title='More than " + Microsoft.Tags.MaxTagsToDisplay + " values'> ...</strong>";
                                    }

                                    tagsHTML += "</" + htmlTagElt + ">";

                                }
                            }
                            else {
                                tagsHTML += '<' + htmlTagElt + ' class="tdcolumn">'
                                var tagDisplayValue = tagEntry;
                                if (tagEntry.length > Microsoft.Tags.MaxTagValueLengthToDisplay) {
                                    tagDisplayValue = tagEntry.substring(0, Microsoft.Tags.MaxTagValueLengthToDisplay) + '...';
                                }
                                tagsHTML += '<button value="'+ tagEntry +'" title="' + displayName + ' : ' + tagEntry + '" class="tag tag-' + name + '" onclick="Microsoft.Tags.FacetTag(event,\'' + name + '\');">' + tagDisplayValue + '</button>';

                                tagsHTML += '</' + htmlTagElt + '>';
                            }

                            tagsHTML += endTagGroup;
                        }
                    }
                }
            }
        }

        return tagsHTML;
    },

    FacetTag: function (event, name) {
        var tagValue = $(event.target).attr('value');

        if ($(event.target).parents('#tags-card').length) {
            GetReferences(tagValue, false);
        }
        else {
            event.stopPropagation();
            Microsoft.Facets.ChooseFacet(name, Base64.encode(tagValue));
        }
    },

    HighlightTag: function (event) {
        var searchText = $(event.target).attr('value');

        if ($(event.target).parents('#tags-card').length) {
            GetReferences(searchText, false);
        }
        else {
            event.stopPropagation();
            var query = searchText.replace("-", " ");
            $('#q').val(query);
            Search();
        }
    },

    renderTags: function (id, tags, collapse = true) {
        var htmlDiv = ''

        if (tags.length > 0) {
            // Tags
            if (collapse) {
                htmlDiv += '        <div class="row detailedView collapse" id="collapsable-' + id + '">';
            }
            else {
                htmlDiv += '        <div class="row detailedView" id="collapsable-' + id + '">';
            }
            htmlDiv += '            <div class="col-md-12" >';
            htmlDiv += '                <div class="tagshead" id="tags-' + id + '">' + tags + '</div>';
            htmlDiv += '            </div>';
            htmlDiv += '        </div>';
        }

        return htmlDiv;
    }
}

// export default Microsoft.Tags;