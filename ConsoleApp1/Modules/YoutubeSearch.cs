using System;
using System.Collections.Generic;
using System.Text;
using Google.Apis.YouTube.v3;
using System.Threading.Tasks;
using Google.Apis.Services;

namespace WheelchairBot.Modules
{
    public class YoutubeSearch
    {
        private string APIkey;
        public YoutubeSearch(string APIKey) => this.APIkey = APIKey;

        public List<string> videos { get; private set; }

        public async Task Search(string searchTerm)
        {
            try
            {
                var ytService = new YouTubeService(new BaseClientService.Initializer()
                {
                    ApiKey = APIkey,
                    ApplicationName = GetType().ToString()
                });

                var searchListRequest = ytService.Search.List("snippet");
                searchListRequest.Q = searchTerm;
                searchListRequest.MaxResults = 5;

                var searchListResponse = await searchListRequest.ExecuteAsync();

                videos = new List<string>();

                foreach (var searchResult in searchListResponse.Items)
                    if (searchResult.Id.Kind == "youtube#video")
                    { videos.Add(string.Format("{0} ({1})", searchResult.Snippet.Title, searchResult.Id.VideoId)); }
            } catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
