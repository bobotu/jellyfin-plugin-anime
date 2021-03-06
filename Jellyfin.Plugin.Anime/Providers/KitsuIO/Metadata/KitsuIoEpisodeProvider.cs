using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Anime.Providers.KitsuIO.ApiClient;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.Anime.Providers.KitsuIO.Metadata
{
    public class KitsuIoEpisodeProvider : IRemoteMetadataProvider<Episode, EpisodeInfo>
    {
        private readonly IHttpClient _httpClient;
        public string Name => ProviderNames.KitsuIo;

        public KitsuIoEpisodeProvider(IHttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        
        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(EpisodeInfo searchInfo, CancellationToken cancellationToken)
        {
            var id = searchInfo.ProviderIds.GetOrDefault(ProviderNames.KitsuIo);
            if (id == null)
            {
                return new List<RemoteSearchResult>();;
            }

            var apiResponse = await KitsuIoApi.Get_Episodes(id);
            return apiResponse.Data.Select(x => new RemoteSearchResult
            {
                IndexNumber = x.Attributes.Number,
                Name = x.Attributes.Titles.GetTitle,
                ParentIndexNumber = x.Attributes.SeasonNumber,
                PremiereDate = x.Attributes.AirDate,
                ProviderIds = new Dictionary<string, string> {{ProviderNames.KitsuIo, x.Id.ToString()}},
                SearchProviderName = Name,
                Overview = x.Attributes.Synopsis,
            });
        }

        public async Task<MetadataResult<Episode>> GetMetadata(EpisodeInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Episode>();
            
            var id = info.ProviderIds.GetOrDefault(ProviderNames.KitsuIo);
            if (string.IsNullOrEmpty(id))
            {
                return result;
            }

            var episodeInfo = await KitsuIoApi.Get_Episode(id);
            
            result.HasMetadata = true;
            result.Item = new Episode
            {
                IndexNumber = info.IndexNumber,
                ParentIndexNumber = info.ParentIndexNumber,
                Name = episodeInfo.Data.Attributes.Titles.GetTitle,
            };
            
            if (episodeInfo.Data.Attributes.Length != null)
            {
                result.Item.RunTimeTicks = TimeSpan.FromMinutes(episodeInfo.Data.Attributes.Length.Value).Ticks;
            }

            return result;
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClient.GetResponse(new HttpRequestOptions
            {
                UserAgent = Constants.UserAgent,
                CancellationToken = cancellationToken,
                Url = url,
            });
        }
    }
}