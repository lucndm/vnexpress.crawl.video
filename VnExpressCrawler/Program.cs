using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsQuery.ExtensionMethods.Internal;
using HtmlAgilityPack;

namespace VnExpressCrawler
{
    internal class Program
    {
        public static string AddressCrawl { set; get; }
        private const string PathSave = "D:\\video_express.txt";
        private static void Main(string[] args)
        {
            Console.WriteLine("Nhap Address :");
            var readLine = Console.ReadLine();
            while (!readLine.Contains("video.vnexpress.net"))
            {
                Console.WriteLine("Nhap lai address chi crawl duoc trang vnexpress.");
                readLine = Console.ReadLine();
            }
            AddressCrawl = readLine;
            UseAbot();
        }

        private static void RunAbot(Abot.Crawler.PoliteWebCrawler crawler)
        {
            Abot.Poco.CrawlResult result = crawler.Crawl(new Uri(AddressCrawl));
//            Abot.Poco.CrawlResult result = crawler.Crawl(new Uri("http://video.vnexpress.net/"));
//            Abot.Poco.CrawlResult result = crawler.Crawl(new Uri("http://video.vnexpress.net/"));
                //This is synchronous, it will not go to the next line until the crawl has completed

            if (result.ErrorOccurred)
                Console.WriteLine("Crawl of {0} completed with error: {1}", result.RootUri.AbsoluteUri,
                    result.ErrorException.Message);
            else
                Console.WriteLine("Crawl of {0} completed without error.", result.RootUri.AbsoluteUri);
            System.Diagnostics.Debug.WriteLine(Videos.Count);
            System.Diagnostics.Debug.WriteLine("STOP");
            var videosOrder = Videos.OrderByDescending(o=>o.Id);
            var serializeObject = Newtonsoft.Json.JsonConvert.SerializeObject(videosOrder,
                Newtonsoft.Json.Formatting.Indented);
            System.IO.File.WriteAllText(PathSave, serializeObject);
            System.Diagnostics.Debug.Write($"EXPORT: {Videos.Count} Videos at {PathSave}");
        }

        private static void UseAbot()
        {
            Abot.Poco.CrawlConfiguration configuration = new Abot.Poco.CrawlConfiguration();
            configuration.CrawlTimeoutSeconds = 100;
            configuration.MaxConcurrentThreads = 10;
            configuration.MaxPagesToCrawl = 1000;
            configuration.UserAgentString = "abot v1.0 http://code.google.com/p/abot";

            Abot.Crawler.PoliteWebCrawler crawler = new Abot.Crawler.PoliteWebCrawler(configuration);
            crawler.PageCrawlStartingAsync += Crawler_PageCrawlStartingAsync;
            crawler.PageCrawlCompletedAsync += Crawler_PageCrawlCompletedAsync;
            RunAbot(crawler);
        }

        private static object objlock = new object();
        private static readonly List<CrawlVideo> Videos = new List<CrawlVideo>();

        private static void Crawler_PageCrawlCompletedAsync(object sender, Abot.Crawler.PageCrawlCompletedArgs e)
        {
            Abot.Poco.CrawledPage page = e.CrawledPage;
            if (page.WebException != null || page.HttpWebResponse.StatusCode != System.Net.HttpStatusCode.OK)
            {
                Console.WriteLine("Crawl of page failed {0}", page.Uri.AbsoluteUri);
            }
            else
                Console.WriteLine("Crawl of page succeeded {0}", page.Uri.AbsoluteUri);

            if (string.IsNullOrEmpty(page.Content.Text))
            {
                Console.WriteLine("Page had no content {0}", page.Uri.AbsoluteUri);
            }
            else
            {
                var infomationVideo = GetInfomationVideo(page.Content.Text);
                lock (objlock)
                {
                    if (!Videos.Any(a => a.Id.Equals(infomationVideo.Id)))
                    {
                        Videos.Add(infomationVideo);
                    }
                }
            }
        }

        private static CrawlVideo GetInfomationVideo(string html)
        {
            HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument()
            {
                OptionAutoCloseOnEnd = true,
                OptionFixNestedTags = true
            };
            document.LoadHtml(html);
            if (document.DocumentNode != null)
            {
                CrawlVideo crawlVideo = new CrawlVideo
                {
                    Title = GetTitle(document.DocumentNode),
                    Tags = GetTags(document.DocumentNode),
                    Description = GetDescription(document.DocumentNode),
                    LinkImage = GetLinkImage(document.DocumentNode),
                    LinkOrigin = GetLinkOrigin(document.DocumentNode),
                    LinkStream = GetLinkStream(document.DocumentNode),
                    Id = GetArticleId(document.DocumentNode)
                };
                return crawlVideo;
            }
            return new CrawlVideo();
        }

        private static int? GetArticleId(HtmlNode documentNode)
        {
            int? id = null;
            var node = documentNode
                .SelectNodes("//meta")
                .SingleOrDefault(
                    a =>
                        a.Attributes["name"] != null && a.Attributes["name"].Value.Equals("tt_article_id") &&
                        a.Attributes["content"] != null);
            if (node != null)
            {
                var strId = node.Attributes["content"].Value;
                id = int.Parse(strId);
            }
            return id;
        }

        private static HtmlNode GetFlashvars(HtmlNode documentNode)
        {
            var node = documentNode.SelectNodes("//div")
                .SingleOrDefault(a => a.Attributes["class"] != null && a.Attributes["class"].Value.Equals("embed-video"));
            var nodeParam = node?.SelectNodes(".//param")
                .SingleOrDefault(a => a.Attributes["name"] != null && a.Attributes["name"].Value.Equals("flashvars"));
            return nodeParam;
        }

        private static string GetLinkStream(HtmlNode documentNode)
        {
            string linkStream = String.Empty;
            var flashvars = GetFlashvars(documentNode);
            if (flashvars != null)
            {
                var query = flashvars.Attributes["value"].Value;
                linkStream = GetParamInQuery(query, "trackurl");
            }
            return linkStream;
        }

        private static string GetParamInQuery(string query, string trackurl)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return String.Empty;
            }
            var paramsQuery = query.Split('&');
            var paramQuery = paramsQuery.SingleOrDefault(a => a.Contains(trackurl));
            if (paramQuery != null)
            {
                return paramQuery.Split('=')[1];
            }
            return string.Empty;
        }

        private static string GetLinkOrigin(HtmlAgilityPack.HtmlNode documentNode)
        {
            string linkOrigin = string.Empty;
            var node = documentNode
                .SelectNodes(
                    "//meta")
                .SingleOrDefault(
                    a => a.Attributes["property"] != null && a.Attributes["property"].Value.Equals("og:url") &&
                         a.Attributes["itemprop"] != null && a.Attributes["itemprop"].Value.Equals("url"));
            if (node != null)
            {
                linkOrigin = node.Attributes["content"].Value;
            }
            return linkOrigin;
        }

        private static string GetLinkImage(HtmlAgilityPack.HtmlNode documentNode)
        {
            string linkImage = String.Empty;

            var flashvars = GetFlashvars(documentNode);
            if (flashvars != null)
            {
                var query = flashvars.Attributes["value"].Value;
                linkImage = GetParamInQuery(query, "thumburl");
            }
            return linkImage;
        }

        private static string GetDescription(HtmlAgilityPack.HtmlNode documentNode)
        {
            string description = string.Empty;
            var node = documentNode.SelectNodes("//h4")
                .SingleOrDefault(
                    s =>
                        s.Attributes["class"] != null && s.Attributes["class"].Value.Equals("video_top_more") &&
                        s.Attributes["id"] != null && s.Attributes["id"].Value.Equals("video_top_more"));
            if (node != null)
            {
                description = node.InnerText.Replace("\r", "").Replace("\n", "").Replace("\t", "");
            }
            return description;
        }

        private static List<string> GetTags(HtmlAgilityPack.HtmlNode documentNode)
        {
            List<string> tags = new List<string>();
            var node = documentNode.SelectNodes("//div")
                .FirstOrDefault(c => c.Attributes["class"] != null && c.Attributes["class"].Value.Equals("tag_video"));
            if (node != null)
            {
                var nodeTags =
                    node.SelectNodes(".//a")
                        .Where(a => a.Attributes["class"] != null && a.Attributes["class"].Value.Equals("eachTag_video"))
                        .ToList();
                if (nodeTags.Any())
                {
                    tags.AddRange(nodeTags.Select(nodeTag => nodeTag.InnerText));
                }
            }
            return tags;
        }

        private static string GetTitle(HtmlAgilityPack.HtmlNode documentNode)
        {
            string title = String.Empty;
            ;
            var flashvars = GetFlashvars(documentNode);
            if (flashvars != null)
            {
                var query = flashvars.Attributes["value"].Value;
                title = GetParamInQuery(query, "tracktitle");
            }
            return title;
        }


        private static void Crawler_PageCrawlStartingAsync(object sender, Abot.Crawler.PageCrawlStartingArgs e)
        {
            Abot.Poco.PageToCrawl pageToCrawl = e.PageToCrawl;
            Console.WriteLine("About to crawl link {0} which was found on page {1}", pageToCrawl.Uri.AbsoluteUri,
                pageToCrawl.ParentUri.AbsoluteUri);
        }
    }
}