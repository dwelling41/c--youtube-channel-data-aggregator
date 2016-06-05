using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace YoutubeDataParser
{
    public class ChannelStatisticsGatherer
    {
        private readonly string APIKey = "";

        public ChannelStatisticsGatherer(string apiKey)
        {
            APIKey = apiKey;
        }


        public List<ChannelResults> GetChannelStatistics(string username, out string errorMessage)
        {
            // Setup the error message
            errorMessage = "";

            // make sure the username is set
            if(string.IsNullOrEmpty(username))
            {
                errorMessage = "Invalid username";
                return null;
            }

            // Determine the previous month
            var today = DateTime.Today;
            var month = new DateTime(today.Year, today.Month, 1);
            var firstDayLastMonth = month.AddMonths(-1);
            var lastDayLastMonth = month.AddDays(-1);

            // Setup the youtube service
            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = APIKey,
                ApplicationName = "ChannelDataScraper"
            });

            // Get the channel IDs
            var channelListRequest = youtubeService.Channels.List("id, contentDetails, statistics, topicDetails, status, brandingSettings, localizations, snippet, contentOwnerDetails");
            channelListRequest.ForUsername = username;
            var userChannels = channelListRequest.Execute();
            if (userChannels == null || userChannels.Items == null || userChannels.Items.Count <= 0)
            {
                errorMessage = "There are no channels associated with the user.";
                return null;
            }

            // Go through all of the channels and fill up the data
            List<ChannelResults> results = new List<ChannelResults>();

            foreach (var curApiChannel in userChannels.Items)
            {
                // Setup a new channel
                var result = new ChannelResults();
                results.Add(result);

                // Pull in channel results
                result.ChannelName = curApiChannel.Snippet.Title;
                result.ChannelLanguage = curApiChannel.Snippet.Country;
                result.Subscribers = curApiChannel.Statistics.SubscriberCount;

                // Get the current channel's playlist id and make sure it is set
                var curApiChannelPlaylistId = curApiChannel?.ContentDetails?.RelatedPlaylists?.Uploads;
                if (string.IsNullOrEmpty(curApiChannelPlaylistId))
                {
                    continue;
                }

                // Go through all of the playlist items and build up a list of videos to search on
                bool searchingLastMonthVideo = true;
                string nextPageToken = null;
                List<string> lastMonthVideoIds = new List<string>();

                while (searchingLastMonthVideo)
                {
                    // Setup the request to get the playlist items
                    var playlistRequest = youtubeService.PlaylistItems.List("snippet, contentDetails");
                    playlistRequest.PlaylistId = curApiChannelPlaylistId;
                    playlistRequest.MaxResults = 50;
                    playlistRequest.PageToken = nextPageToken;
                    var playlistItems = playlistRequest.Execute();

                    // Exit if the list is empty
                    if (playlistItems == null || playlistItems.Items == null || playlistItems.Items.Count <= 0)
                    {
                        searchingLastMonthVideo = false;
                        break;
                    }

                    // Go through the uploaded playlist items and get all of the video ids to search for
                    foreach (var curUploadedVideo in playlistItems.Items)
                    {
                        // Videos come in descending order so videos in the current month or null can just be skipped 
                        if (curUploadedVideo?.Snippet?.PublishedAt == null)
                        {
                            continue;
                        }

                        if (curUploadedVideo.Snippet.PublishedAt.Value > lastDayLastMonth)
                        {
                            continue;
                        }

                        // Stop once a video is before the previous month
                        if (curUploadedVideo.Snippet.PublishedAt.Value < firstDayLastMonth)
                        {
                            searchingLastMonthVideo = false;
                            break;
                        }

                        // Make sure the video id is set
                        if (!string.IsNullOrEmpty(curUploadedVideo?.ContentDetails?.VideoId))
                        {
                            lastMonthVideoIds.Add(curUploadedVideo.ContentDetails.VideoId);
                        }
                    }

                    // Continue reading if we haven't exited
                    nextPageToken = playlistItems?.NextPageToken;
                }

                // Go through and pull in video data in batches of 50
                var lastMonthVideoData = new List<LastMonthChannelVideoData>();
                int pageNumber = 0;
                bool continueRequestingVideos = lastMonthVideoIds.Count > 0;

                while (continueRequestingVideos)
                {
                    // Get a batch of videos
                    var curVideoIds = lastMonthVideoIds.Skip(50 * pageNumber).Take(50).ToList();
                    pageNumber++;
                    if (curVideoIds.Count <= 0)
                    {
                        continueRequestingVideos = false;
                        break;
                    }

                    // Get a comma separated list of ids
                    var videoIdQueryString = string.Join(",", curVideoIds);

                    // Get the data
                    var videoRequest = youtubeService.Videos.List("statistics, contentDetails");
                    videoRequest.Id = videoIdQueryString;
                    var videoItems = videoRequest.Execute();

                    // make sure the data is set
                    if (videoItems == null || videoItems.Items == null)
                    {
                        continue;
                    }

                    // parse out the data
                    foreach (var curVideoApiData in videoItems.Items)
                    {
                        var lastMonthData = new LastMonthChannelVideoData();
                        lastMonthVideoData.Add(lastMonthData);

                        lastMonthData.Comments = curVideoApiData.Statistics.CommentCount;
                        lastMonthData.Dislikes = curVideoApiData.Statistics.DislikeCount;
                        lastMonthData.Likes = curVideoApiData.Statistics.LikeCount;
                        lastMonthData.Views = curVideoApiData.Statistics.ViewCount;

                        if (!string.IsNullOrEmpty(curVideoApiData.ContentDetails.Duration))
                        {
                            TimeSpan timeDuration = XmlConvert.ToTimeSpan(curVideoApiData.ContentDetails.Duration);
                            lastMonthData.Length = timeDuration;
                        }
                    }

                }

                result.LastMonthVideoData = lastMonthVideoData;

            }


            return results;

        }
    }
}
