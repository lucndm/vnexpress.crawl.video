using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VnExpressCrawler
{
    [System.Diagnostics.DebuggerDisplay("{Title}")]
    public class CrawlVideo
    {
        public CrawlVideo()
        {
        }

        public string Title { set; get; }
        public int? Id { set; get; }
        public string LinkOrigin { set; get; }
        public string LinkStream { set; get; }
        public string LinkImage { set; get; }
        public List<string> Tags { set; get; }
        public string Description { set; get; }
    }
}