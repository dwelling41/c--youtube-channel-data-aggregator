using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YoutubeDataParser
{
    public class LastMonthChannelVideoData
    {
        public ulong? Views { get; set; }
        public ulong? Likes { get; set; }
        public ulong? Dislikes { get; set; }
        public ulong? Comments { get; set; }
        public TimeSpan? Length { get; set; }

    }
}
