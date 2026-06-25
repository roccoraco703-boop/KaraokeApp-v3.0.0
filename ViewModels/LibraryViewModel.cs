using KaraokeApp.MVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace KaraokeApp.ViewModels
{
    public class LibraryViewModel : BaseViewModel
    {
        private ObservableCollection<SongViewModel> _songs = new ObservableCollection<SongViewModel>();
        public ObservableCollection<SongViewModel> Songs
        {
            get => _songs;
            set => SetProperty(ref _songs, value);
        }
        private ObservableCollection<SongViewModel> _allSongs = new();

        private string _searchText = string.Empty;

        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                ApplyFilters();
            }
        }

        private bool _showFavoritesOnly;

        public bool ShowFavoritesOnly
        {
            get => _showFavoritesOnly;

            set
            {
                SetProperty(
                    ref _showFavoritesOnly,
                    value);

                ApplyFilters();
            }
        }

        public void Load(string jsonPath, string basePath)
        {
            if (!File.Exists(jsonPath))
            {
                Songs = new ObservableCollection<SongViewModel>();
                return;
            }

            var json = File.ReadAllText(jsonPath);
            var songs = JsonSerializer.Deserialize<ObservableCollection<SongViewModel>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (songs == null)
            {
                Songs = new ObservableCollection<SongViewModel>();
                return;
            }

            foreach (var song in songs)
            {
                UpdateSongFileStatus(song, basePath);
            }

            _allSongs = new ObservableCollection<SongViewModel>(
                songs
                    .Where(song => !string.IsNullOrWhiteSpace(song.Title))
                    .OrderBy(song => song.Artist)
                    .ThenBy(song => song.Title));

            Songs = new ObservableCollection<SongViewModel>(_allSongs);

            // Aggiorna binding conteggi
            OnPropertyChanged(nameof(TotalSongs));
            OnPropertyChanged(nameof(FavoriteSongs));
            OnPropertyChanged(nameof(ReadySongs));
            OnPropertyChanged(nameof(MissingSongs));
            OnPropertyChanged(nameof(KarSongs));
            OnPropertyChanged(nameof(MidSongs));
        }

// Permette di impostare i brani scansionati direttamente dalla MainViewModel
        public void SetSongs(IEnumerable<SongViewModel> songs)
        {
            var songList = songs.ToList();
            KaraokeApp.Services.AppServices.Log.Info($"LibraryViewModel.SetSongs: received {songList.Count} songs");
            _allSongs = new ObservableCollection<SongViewModel>(songs.OrderBy(s => s.Artist).ThenBy(s => s.Title));
            Songs = new ObservableCollection<SongViewModel>(_allSongs);
            KaraokeApp.Services.AppServices.Log.Info($"LibraryViewModel.SetSongs: _allSongs.Count = {_allSongs.Count}, Songs.Count = {Songs.Count}");
            OnPropertyChanged(nameof(TotalSongs));
            OnPropertyChanged(nameof(FavoriteSongs));
            OnPropertyChanged(nameof(ReadySongs));
            OnPropertyChanged(nameof(MissingSongs));
            OnPropertyChanged(nameof(KarSongs));
            OnPropertyChanged(nameof(MidSongs));
            KaraokeApp.Services.AppServices.Log.Info($"LibraryViewModel.SetSongs: TotalSongs = {TotalSongs}");
        }

        private static void UpdateSongFileStatus(SongViewModel song, string basePath)
        {
            string midiPath = Path.Combine(basePath, song.MidiPath);
            string karPath = Path.Combine(basePath, song.KarPath);
            string videoPath = string.IsNullOrWhiteSpace(song.VideoPath)
                ? Path.Combine(basePath, "Songs", "song.mp4")
                : Path.Combine(basePath, song.VideoPath);

            if (string.IsNullOrWhiteSpace(song.VideoPath))
                song.VideoPath = Path.Combine("Songs", "song.mp4");

            song.MidiExists = File.Exists(midiPath);
            song.KarExists = File.Exists(karPath);
            song.VideoExists = File.Exists(videoPath);
            song.HasAllFiles = song.MidiExists && song.KarExists;
        }


        private void ApplyFilters()
        {
            IEnumerable<SongViewModel> query = _allSongs;

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string search = SearchText.Trim().ToLower();

                query = query.Where(song =>
                    (song.Title != null && song.Title.ToLower().Contains(search)) ||
                    (song.Artist != null && song.Artist.ToLower().Contains(search)));
            }

            if (ShowFavoritesOnly)
            {
                query =
                    query.Where(
                        song => song.IsFavorite);
            }

            Songs = new ObservableCollection<SongViewModel>(query);
        }

        public int TotalSongs =>
            _allSongs.Count;

        public int FavoriteSongs =>
            _allSongs.Count(
                s => s.IsFavorite);

        public int ReadySongs =>
            _allSongs.Count(
                s => s.HasAllFiles);

        public int MissingSongs =>
            _allSongs.Count(
                s => !s.HasAllFiles);

        // Conteggi aggiuntivi per estensioni
        public int KarSongs =>
            _allSongs.Count(
                s => s.KarExists);

public int MidSongs =>
            _allSongs.Count(
                s => s.MidiExists);

public IEnumerable<SongViewModel>
    RecentSongs =>
        Songs
            .Where(
                s => s.LastPlayed != null)
            .OrderByDescending(
                s => s.LastPlayed)
            .Take(20);

public IEnumerable<SongViewModel>
    TopSongs =>
        Songs
            .OrderByDescending(
                s => s.PlayCount)
            .Take(20);

public IEnumerable<SongViewModel>
    FavoriteList =>
        Songs
            .Where(
                s => s.IsFavorite)
            .OrderBy(
                s => s.Title);
                    // Helper per notificare cambiamenti di property da metodo statico
                    private static void OnPropertyChangedStatic(string propertyName)
                    {
                        // Non usato ora; conservato per compatibilità.
                    }
                }
            }

