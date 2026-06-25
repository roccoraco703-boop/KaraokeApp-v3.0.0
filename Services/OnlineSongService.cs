using KaraokeApp.ViewModels;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace KaraokeApp.Services
{
    public class OnlineSongService
    {
        public async Task<ObservableCollection<SongViewModel>> DownloadCatalogAsync(string catalogPath)
        {
            if (!File.Exists(catalogPath))
                return new ObservableCollection<SongViewModel>();

            string json = await File.ReadAllTextAsync(catalogPath);
            return JsonSerializer.Deserialize<ObservableCollection<SongViewModel>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new ObservableCollection<SongViewModel>();
        }

        public Task DownloadSongAsync(SongViewModel song, string destinationFolder)
        {
            Directory.CreateDirectory(destinationFolder);
            return Task.CompletedTask;
        }
    }
}
