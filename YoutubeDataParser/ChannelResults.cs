using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YoutubeDataParser
{
    public class ChannelResults
    {
        public string ChannelName { get; set; }
        public string ChannelLanguage { get; set; }
        public ulong? Subscribers { get; set; }
        public List<LastMonthChannelVideoData> LastMonthVideoData { get; set; }
    }
}
