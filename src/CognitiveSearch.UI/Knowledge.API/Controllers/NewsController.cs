// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Knowledge.Services;
using Knowledge.Services.Configuration;
using Knowledge.Services.News;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Xml;

namespace Knowledge.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class NewsController : AbstractApiController
    {
        public NewsController(TelemetryClient telemetry, IQueryService client, SearchServiceConfig svcconfig)
        {
            telemetryClient = telemetry;
            _queryService = client;
            _config = svcconfig;
        }

        // CREDITS : http://www.binaryintellect.net/articles/05fc3052-bf5c-4ab9-b8ab-a7fd6974b977.aspx
        [HttpPost("getliveaggregatedfeed")]
        public IActionResult GetLiveAggregatedFeed(FeedFacet[] feeds)
        {
            List<SyndicationItem> finalItems = new List<SyndicationItem>();

            try
            {
                foreach (FeedFacet feed in feeds)
                {
                    XmlReaderSettings settings = new XmlReaderSettings
                    {
                        DtdProcessing = DtdProcessing.Ignore
                    };

                    XmlReader reader = XmlReader.Create(feed.RSSFeedURL, settings);
                    Rss20FeedFormatter formatter = new Rss20FeedFormatter();
                    formatter.ReadFrom(reader);
                    reader.Close();

                    //foreach (SyndicationItem item in formatter.Feed.Items)
                    //{
                    //    finalItems.Add(AddFeedSource(item, formatter.Feed));
                    //}
                    finalItems.AddRange((IEnumerable<SyndicationItem>)formatter.Feed.Items.Select(selector => AddFeedSource(selector, formatter.Feed)));
                }
                // Sort the feed items by date
                finalItems.Sort(CompareDates);
            }
            catch (Exception ex)
            {
                this.telemetryClient.TrackException(ex);
            }

            // Create the feeds aggregated feed ...
            SyndicationFeed finalFeed = new SyndicationFeed
            {
                Title = new TextSyndicationContent("Aggregated Feed"),
                Copyright = new TextSyndicationContent("Copyright (C) 2021. All rights reserved."),
                Description = new TextSyndicationContent("RSS Feed Generated .NET Syndication Classes"),
                Generator = "Aggregated Feed Generator",
                Items = finalItems
            };

            StringWriter sw = new StringWriter();
            XmlWriter writer = XmlWriter.Create(sw);
            Rss20FeedFormatter finalFormatter = new Rss20FeedFormatter(finalFeed);
            finalFormatter.WriteTo(writer);
            writer.Close();
            sw.Flush();

            return new ContentResult
            {
                Content = sw.ToString(),
                ContentType = "text/xml",
                StatusCode = 200
            };
        }

        private SyndicationItem AddFeedSource(SyndicationItem selector, SyndicationFeed feed)
        {
            selector.SourceFeed = feed;

            //SyndicationElementExtension = new SyndicationElementExtension()
            //selector.ElementExtensions.Add(new SyndicationElementExtension());
            return selector;
        }

        [HttpPost("getlivefeed")]
        public IActionResult GetLiveFeed(FeedFacet[] feeds)
        {
            List<SyndicationItem> finalItems = new List<SyndicationItem>();

            try
            {
                foreach (FeedFacet feed in feeds)
                {
                    XmlReaderSettings settings = new XmlReaderSettings
                    {
                        DtdProcessing = DtdProcessing.Ignore
                    };

                    XmlReader reader = XmlReader.Create(feed.RSSFeedURL, settings);
                    Rss20FeedFormatter formatter = new Rss20FeedFormatter();
                    formatter.ReadFrom(reader);
                    reader.Close();

                    finalItems.AddRange(formatter.Feed.Items);
                }
                // Sort the feed items by date
                finalItems.Sort(CompareDates);
            }
            catch (Exception ex)
            {
                this.telemetryClient.TrackException(ex);
            }

            // Create the feeds aggregated feed ...
            SyndicationFeed finalFeed = new SyndicationFeed
            {
                Title = new TextSyndicationContent("Aggregated Feed"),
                Copyright = new TextSyndicationContent("Copyright (C) 2021. All rights reserved."),
                Description = new TextSyndicationContent("RSS Feed Generated .NET Syndication Classes"),
                Generator = "Aggregated Feed Generator",
                Items = finalItems
            };

            StringWriter sw = new StringWriter();
            XmlWriter writer = XmlWriter.Create(sw);
            Rss20FeedFormatter finalFormatter = new Rss20FeedFormatter(finalFeed);
            finalFormatter.WriteTo(writer);
            writer.Close();
            sw.Flush();

            return new ContentResult
            {
                Content = sw.ToString(),
                ContentType = "text/xml",
                StatusCode = 200
            };
        }

        private int CompareDates(SyndicationItem x, SyndicationItem y)
        {
            return y.PublishDate.CompareTo(x.PublishDate);
        }

    }
}
