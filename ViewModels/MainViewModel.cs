using KaraokeApp.MVVM;
using KaraokeApp.Audio;
using KaraokeApp.Core;
using KaraokeApp.Midi;
using KaraokeApp.Services;
using System.Windows.Input;
using System;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Linq;
using MidiFileCore = Melanchall.DryWetMidi.Core.MidiFile;
using ProgramChangeMidiEvent = Melanchall.DryWetMidi.Core.ProgramChangeEvent;
using NoteOnMidiEvent = Melanchall.DryWetMidi.Core.NoteOnEvent;
using Melanchall.DryWetMidi.Interaction;

namespace KaraokeApp.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        // 🎤 UI PROPERTIES
        private string? _lyrics;
        public string? Lyrics
        {
            get => _lyrics;
            set
            {
                if (SetProperty(ref _lyrics, value))
                    PlayerVM.Lyrics = value ?? string.Empty;
            }
        }

        private int _score;
        public int Score
        {
            get => _score;
            set
            {
                if (SetProperty(ref _score, value))
                    PlayerVM.Score = value;
            }
        }

        private string? _note;
        public string? Note
        {
            get => _note;
            set
            {
                if (SetProperty(ref _note, value))
                    PlayerVM.Note = value ?? string.Empty;
            }
        }

        private float _targetFrequency;
        public float TargetFrequency
        {
            get => _targetFrequency;
            set => SetProperty(ref _targetFrequency, value);
        }

        private int _voiceKeyShiftSemitones;
        private double _rawTargetFrequency;
        public int VoiceKeyShiftSemitones
        {
            get => _voiceKeyShiftSemitones;
            set
            {
                if (SetProperty(ref _voiceKeyShiftSemitones, Math.Clamp(value, -24, 24)))
                {
                    OnPropertyChanged(nameof(VoiceKeyShiftText));
                    ApplyVoiceKeyShiftToTarget();
                }
            }
        }

        public string VoiceKeyShiftText => VoiceKeyShiftSemitones == 0
            ? "Tonalità voce: 0"
            : $"Tonalità voce: {(VoiceKeyShiftSemitones > 0 ? "+" : string.Empty)}{VoiceKeyShiftSemitones} semitoni";

        public PitchState Pitch { get; } = new PitchState();
        public LibraryViewModel Library { get; } = new LibraryViewModel();

        public ObservableCollection<SongViewModel> Songs { get; set; }
        public ObservableCollection<string> AudioOutputDevices { get; } = new();
        public ObservableCollection<MidiChannelViewModel> MidiChannels { get; } = new();

        private int _selectedAudioOutputIndex;
        public int SelectedAudioOutputIndex
        {
            get => _selectedAudioOutputIndex;
            set
            {
                if (SetProperty(ref _selectedAudioOutputIndex, value) && _audioEngine != null && _configService != null)
                {
                    int deviceNumber = value - 1;
                    _audioEngine.SetOutputDevice(deviceNumber);
                    _configService.Settings.AudioOutputDeviceNumber = deviceNumber;
                    _configService.Save();
                    Status = value >= 0 && value < AudioOutputDevices.Count
                        ? "Uscita audio: " + AudioOutputDevices[value]
                        : "Uscita audio aggiornata";
                }
            }
        }

        private SongViewModel? _selectedSong;
        public SongViewModel? SelectedSong
        {
            get => _selectedSong;
            set
            {
                if (SetProperty(ref _selectedSong, value))
                {
                    StopVideoRequested?.Invoke();
                    PlayerVM.IsYouTube = value?.UseYouTube == true && !string.IsNullOrWhiteSpace(value.YouTubeUrl);
                    PlayerVM.YouTubeUrl = value?.YouTubeUrl ?? string.Empty;
                    OnPropertyChanged(nameof(UseYouTubeForSelectedSong));
                    VideoPathChanged?.Invoke(value?.VideoPath);
                }
            }
        }

        private string? _status;
        public string? Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        private int _offsetMs;
        public int OffsetMs
        {
            get => _offsetMs;
            set
            {
                SetProperty(ref _offsetMs, value);
                _syncEngine?.SetVideoOffset(value);
            }
        }

        private int _lyricsOffsetMs;

        public int LyricsOffsetMs
        {
            get => _lyricsOffsetMs;
            set
            {
                if (SetProperty(ref _lyricsOffsetMs, value))
                {
                    if (_syncEngine != null)
                        _syncEngine.LyricsOffsetMs = value;

                    if (_configService != null)
                    {
                        _configService.Settings.LyricsOffsetMs = value;
                        _configService.Save();
                    }
                }
            }
        }

        private int _midiOffsetMs;

        public int MidiOffsetMs
        {
            get => _midiOffsetMs;
            set
            {
                if (SetProperty(ref _midiOffsetMs, value))
                {
                    if (_syncEngine != null)
                        _syncEngine.MidiOffsetMs = value;

                    if (_configService != null)
                    {
                        _configService.Settings.MidiOffsetMs = value;
                        _configService.Save();
                    }
                }
            }
        }

        private int _videoOffsetMs;

        public int VideoOffsetMs
        {
            get => _videoOffsetMs;
            set
            {
                if (SetProperty(ref _videoOffsetMs, value))
                {
                    if (_configService != null)
                    {
                        _configService.Settings.VideoOffsetMs = value;
                        _configService.Save();
                    }
                }
            }
        }

        private bool _showStartButton = true;
        public bool ShowStartButton
        {
            get => _showStartButton;
            set => SetProperty(ref _showStartButton, value);
        }

        private bool _isAutotuneEnabled;
        public bool IsAutotuneEnabled
        {
            get => _isAutotuneEnabled;
            set
            {
                if (SetProperty(ref _isAutotuneEnabled, value))
                {
                    _audioEngine.PitchCorrection.IsEnabled = value;
                    OnPropertyChanged(nameof(AutotuneStatus));
                }
            }
        }

        public string AutotuneStatus => IsAutotuneEnabled ? "AUTOTUNE: ON" : "AUTOTUNE: OFF";

        private string _autoTuneMode = "Soft";
        public string AutoTuneMode
        {
            get => _autoTuneMode;
            set
            {
                if (SetProperty(ref _autoTuneMode, value))
                {
                    _configService.Settings.AutoTuneMode = value;
                    _configService.Save();
                    OnPropertyChanged(nameof(AutoTuneModeStatus));
                }
            }
        }

        public string AutoTuneModeStatus => "MODE: " + AutoTuneMode.ToUpperInvariant();

        public bool UseYouTubeForSelectedSong
        {
            get => SelectedSong?.UseYouTube == true;
            set
            {
                if (SelectedSong == null)
                    return;

                SelectedSong.UseYouTube = value;
                PlayerVM.IsYouTube = value && !string.IsNullOrWhiteSpace(SelectedSong.YouTubeUrl);
                PlayerVM.YouTubeUrl = SelectedSong.YouTubeUrl ?? string.Empty;
                OnPropertyChanged();
            }
        }

        private bool _isMixerEnabled;
        public bool IsMixerEnabled
        {
            get => _isMixerEnabled;
            set
            {
                if (SetProperty(ref _isMixerEnabled, value))
                    OnPropertyChanged(nameof(MixerStatus));
            }
        }

        public string MixerStatus => IsMixerEnabled ? "MIXER: ON" : "MIXER: OFF";

        private bool _isMonitorMuted;
        public bool IsMonitorMuted
        {
            get => _isMonitorMuted;
            set
            {
                if (SetProperty(ref _isMonitorMuted, value))
                    OnPropertyChanged(nameof(MonitorStatus));
            }
        }

        public string MonitorStatus => IsMonitorMuted ? "MONITOR: MUTO" : "MONITOR: ON";

        private string _multiplayerStatus = "MULTIPLAYER: OFF";
        public string MultiplayerStatus
        {
            get => _multiplayerStatus;
            set => SetProperty(ref _multiplayerStatus, value);
        }

        private string _multiplayerPlayers = "Players: -";
        public string MultiplayerPlayers
        {
            get => _multiplayerPlayers;
            set => SetProperty(ref _multiplayerPlayers, value);
        }

        private string _onlineCatalogStatus = "CATALOGO: locale";
        public string OnlineCatalogStatus
        {
            get => _onlineCatalogStatus;
            set => SetProperty(ref _onlineCatalogStatus, value);
        }

        public float MicVolume
        {
            get => _audioEngine?.MicrophoneVolume ?? 1.0f;
            set
            {
                if (_audioEngine == null || _configService == null)
                    return;

                _audioEngine.MicrophoneVolume = value;
                _configService.Settings.MicVolume = value;
                _configService.Save();
                OnPropertyChanged();
            }
        }

        public float MusicVolume
        {
            get => _audioEngine?.MusicVolume ?? 0.8f;
            set
            {
                if (_audioEngine == null || _configService == null)
                    return;

                _audioEngine.MusicVolume = value;
                _configService.Settings.MusicVolume = value;
                _configService.Save();
                OnPropertyChanged();
            }
        }

        // 🎤 COMPONENTI
        private readonly MicrophoneAnalyzer _mic;
        private readonly MediaClock _clock;
        private readonly SyncEngine _syncEngine;
        // Kept for compatibility: lyrics engine instance is declared here for older integrations.
        private readonly MidiPlaybackService _midiPlayer;
        private readonly Queue<double> _pitchHistory = new();
        private readonly AudioEngine _audioEngine;
        private readonly OnlineSongService _onlineSongService;
        private readonly MultiplayerService _multiplayerService;
        private readonly ConfigService _configService;
        private readonly UserLibraryService
            _userLibraryService =
                new();

        private UserLibraryData
            _userLibraryData =
                new();

        private readonly PlaylistPersistenceService
            _playlistPersistence =
                new();

        private PlaylistData
            _playlistData =
                new();

        private string _currentLyricLine = string.Empty;
        private int _currentLyricLineIndex = -1;

        public event Action<string?>? VideoPathChanged;
        public event Action? StopVideoRequested;
        public virtual PlayerViewModel PlayerVM { get; } = new PlayerViewModel();

                public PlaylistViewModel
                    PlaylistVM
                    { get; } = new();

        public ICommand StartCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand PreviousSongCommand { get; }
        public ICommand NextSongCommand { get; }
        public ICommand ResetScoreCommand { get; }
        public ICommand ToggleFavoriteCommand { get; }
        public ICommand TestMicrophoneCommand { get; }
        public ICommand ToggleAutotuneCommand { get; }
        public ICommand ToggleMixerCommand { get; }
        public ICommand ToggleMicrophoneMuteCommand { get; }
        public ICommand IncreaseMicVolumeCommand { get; }
        public ICommand DecreaseMicVolumeCommand { get; }
        public ICommand IncreaseMusicVolumeCommand { get; }
        public ICommand DecreaseMusicVolumeCommand { get; }
        public ICommand DownloadCatalogCommand { get; }
        public ICommand DownloadSelectedSongCommand { get; }
        public ICommand CreateMultiplayerCommand { get; }
        public ICommand StopMultiplayerCommand { get; }
        public ICommand CycleAutoTuneModeCommand { get; }
        public ICommand IncreaseVoiceToneCommand { get; }
        public ICommand DecreaseVoiceToneCommand { get; }
        public ICommand IncreaseVoiceSemitoneCommand { get; }
        public ICommand DecreaseVoiceSemitoneCommand { get; }
        public ICommand ResetVoiceKeyCommand { get; }
        public ICommand SelectLibraryFolderCommand { get; } // E1: Seleziona cartella
        public ICommand RefreshLibraryCommand { get; }      // E3: Aggiorna libreria

        public MainViewModel()
        {
            Lyrics = "Ready...";
            Score = 0;
            Status = "Caricamento canzoni...";

            // ⏱ CLOCK + SYNC
            _clock = new MediaClock();
            _syncEngine = new SyncEngine(_clock);
            OffsetMs = 0;
            _midiPlayer = new MidiPlaybackService();
            _audioEngine = new AudioEngine();
            _onlineSongService = new OnlineSongService();
            _multiplayerService = new MultiplayerService();
            _configService = new ConfigService();
            _configService.Load();
            OffsetMs = _configService.Settings.OffsetMs;
            _userLibraryService = new UserLibraryService();
            // Load saved offsets
            LyricsOffsetMs = _configService.Settings.LyricsOffsetMs;
            MidiOffsetMs = _configService.Settings.MidiOffsetMs;
            VideoOffsetMs = _configService.Settings.VideoOffsetMs;
            _audioEngine.MusicVolume = _configService.Settings.MusicVolume;
            _audioEngine.MicrophoneVolume = _configService.Settings.MicVolume;
            foreach (string device in _audioEngine.GetOutputDevices())
                AudioOutputDevices.Add(device);
            SelectedAudioOutputIndex = Math.Clamp(_configService.Settings.AudioOutputDeviceNumber + 1, 0, Math.Max(0, AudioOutputDevices.Count - 1));
            AutoTuneMode = string.IsNullOrWhiteSpace(_configService.Settings.AutoTuneMode) ? "Soft" : _configService.Settings.AutoTuneMode;
            IsAutotuneEnabled = !AutoTuneMode.Equals("Off", StringComparison.OrdinalIgnoreCase);
            _audioEngine.PitchCorrection.Strength = AutoTuneMode.Equals("Hard", StringComparison.OrdinalIgnoreCase) ? 0.75 : 0.35;
            OnPropertyChanged(nameof(MicVolume));
            OnPropertyChanged(nameof(MusicVolume));
            _audioEngine.OnStatusChanged += message => Status = message;
            _multiplayerService.OnStatusChanged += message => Status = message;
            _midiPlayer.OnStatusChanged += message => Status = message;

            // 🎤 MICROFONO
            _mic = new MicrophoneAnalyzer();
            _mic.OnStatusChanged += message =>
            {
                Status = message;
            };

            _mic.OnAudioAvailable += (buffer, format) =>
            {
                byte[] monitorBuffer = buffer;

                if (format.BitsPerSample == 16)
                    monitorBuffer = _audioEngine.PitchCorrection.ProcessPcm16(buffer, format.SampleRate, format.Channels, TargetFrequency);

                _audioEngine.AddMicrophoneSamples(monitorBuffer, format);
            };

            _mic.OnPitchDetected += f =>
            {
                _pitchHistory.Enqueue(f);

                if (_pitchHistory.Count > 5)
                    _pitchHistory.Dequeue();

                double avg = 0;
                foreach (var p in _pitchHistory)
                    avg += p;

                avg /= _pitchHistory.Count;
                _audioEngine.PitchCorrection.Processor.CurrentPitch = (float)avg;

                if (TargetFrequency <= 0)
                {
                    PlayerVM.CurrentPitch = avg;
                    Pitch.Current = (float)avg;
                    return;
                }

                double corrected = _audioEngine.PitchCorrection.IsEnabled
                    ? _audioEngine.PitchCorrection.CorrectPitch(avg, TargetFrequency)
                    : PlayerVM.ApplyPitchCorrection(avg, TargetFrequency);

                PlayerVM.CurrentPitch = corrected;
                Pitch.Current = (float)corrected;

                double targetMidi = MusicUtils.FrequencyToMidi(TargetFrequency);
                double voiceMidi = MusicUtils.FrequencyToMidi(corrected);

                double diff = Math.Abs(targetMidi - voiceMidi);

                if (diff < 0.3)
                {
                    PlayerVM.Combo++;
                    Score += 10 + PlayerVM.Combo;
                }
                else if (diff < 1.0)
                {
                    PlayerVM.Combo = 0;
                    Score += 2;
                }
                else
                {
                    PlayerVM.Combo = 0;
                }

                // log pitch engine activity when it is active
                AppServices.Log.Info($"Pitch detected: avg={avg:0.00}, corrected={corrected:0.00}, diff={diff:0.00}");
            };

            _syncEngine.OnCurrentPhraseChanged += p =>
            {
                Lyrics = FormatLyric(p?.Text ?? string.Empty);
            };

            _syncEngine.OnCurrentPhraseChanged += phrase =>
            {
                PlayerVM.CurrentPhrase =
                    phrase?.Text ?? string.Empty;

                PlayerVM.CurrentLineWords.Clear();

                if (phrase?.Words != null)
                {
                    foreach (var word in phrase.Words)
                    {
                        PlayerVM.CurrentLineWords.Add(
                            new KaraokeWordViewModel
                            {
                                Text = word.Text
                            });
                    }
                }

                PlayerVM.CurrentWordIndex = 0;
            };

            _syncEngine.OnNextPhraseChanged += phrase =>
            {
                PlayerVM.NextPhrase =
                    phrase?.Text ?? string.Empty;

                PlayerVM.PendingLyrics =
                    phrase?.Text ?? string.Empty;
            };

            _syncEngine.OnCurrentWordChanged += index =>
            {
                PlayerVM.CurrentWordIndex =
                    index;
            };

            _syncEngine.OnTick += time =>
            {
                PlayerVM.CurrentTimeMs = time;
            };

            _syncEngine.OnNoteChanged += f =>
            {
                _rawTargetFrequency = f;
                TargetFrequency = ApplyVoiceKeyShift(_rawTargetFrequency);
                _audioEngine.PitchCorrection.Processor.TargetFrequency = TargetFrequency;
                Console.WriteLine("NOTE: " + f);
                Pitch.Target = TargetFrequency;
            };

            // CLOCK TICK - use MediaClock as sole time source
            _clock.OnTick += ms =>
            {
                long currentTime = PlayerVM.HasVideoSource && !PlayerVM.IsYouTube && PlayerVM.GetVideoTimeMs is not null
                    ? PlayerVM.GetVideoTimeMs.Invoke()
                    : ms;

                PlayerVM.CurrentTime = currentTime;
                UpdateKaraokeLyrics(currentTime);
                _syncEngine.Update(currentTime);
                PlayerVM.RaiseFrame();
            };

            if (_configService.Settings.StartMixerEnabled)
            {
                _audioEngine.Start();
                IsMixerEnabled = _audioEngine.IsMixerRunning;
            }

            Songs = LoadSongs();
            _userLibraryData = _userLibraryService.Load();
            _playlistData = _playlistPersistence.Load();
            AppServices.Log.Info($"Playlist loaded. Items: {_playlistData.Songs.Count}");
            // apply persisted user library state (favorites, play count, last played)
            if (_userLibraryData != null)
            {
                var data = _userLibraryData;
                foreach (var s in Songs)
                {
                    var id = s.MidiPath ?? s.Title;
                    s.IsFavorite = data.Favorites.Contains(id);
                    s.PlayCount = data.PlayCount.TryGetValue(id, out var pc) ? pc : 0;
                    s.LastPlayed = data.LastPlayed.TryGetValue(id, out var lp) ? lp : (DateTime?)null;
                }
            }

            // restore saved playlist
            foreach (var title in _playlistData.Songs)
            {
                var song = Songs.FirstOrDefault(s => s.Title == title);
                if (song == null)
                    continue;

                PlayerVM.Playlist.Add(new PlaylistItemViewModel
                {
                    Position = PlayerVM.Playlist.Count + 1,
                    Song = song
                });
            }

            SelectedSong = Songs.Count > 0 ? Songs[0] : null;
            Status = Songs.Count > 0 ? $"{Songs.Count} canzoni caricate" : "Nessuna canzone trovata";

            // ▶️ START
            StartCommand = new RelayCommand(_ =>
            {
                if (SelectedSong == null)
                {
                    Status = "Seleziona una canzone";
                    return;
                }

                if (!SelectedSong.IsPlayable)
                {
                    Status = "Brano non pronto: controlla MIDI/KAR";
                    return;
                }

                string basePath = AppDomain.CurrentDomain.BaseDirectory;

                string midiPath = System.IO.Path.Combine(basePath, SelectedSong.MidiPath);
                string karPath = System.IO.Path.Combine(basePath, SelectedSong.KarPath);
                string videoPath = string.IsNullOrWhiteSpace(SelectedSong.VideoPath)
                    ? string.Empty
                    : System.IO.Path.Combine(basePath, SelectedSong.VideoPath);

                if (!File.Exists(midiPath))
                {
                    Lyrics = "File MIDI non trovato";
                    Status = midiPath;
                    return;
                }

                if (!File.Exists(karPath))
                {
                    Lyrics = "File KAR non trovato";
                    Status = karPath;
                    return;
                }

                _clock.Stop();
                _mic.Stop();
                _midiPlayer.Stop();
                _clock.Reset();
                _pitchHistory.Clear();

                try
                {
                    var parser = new MidiNoteParser();
                    var midi = parser.Parse(midiPath);
                    LoadMidiChannels(midiPath);
                    _syncEngine.LoadNotes(midi);
                    PlayerVM.PitchNotes = midi.ConvertAll(n => (n.StartTime / 1000.0, (double)KaraokeApp.Audio.NoteToFrequencyConverter.ToFrequency(n.NoteNumber)));

                    var karParser = new KarParser();
                    var lyrics = karParser.Parse(karPath);
                    BuildVerticalLyrics(lyrics, GetExactLyricsLines(SelectedSong, basePath));
                    _syncEngine.LoadLyrics(lyrics);



                    Score = 0;
                    PlayerVM.Combo = 0;
                    PlayerVM.IsFinished = false;
                    PlayerVM.HasVideoSource = false;
                    PlayerVM.CurrentWordIndex = 0;
                    _currentLyricLine = string.Empty;
                    _currentLyricLineIndex = -1;
                    ResetKaraokeLines();
                    UpdateKaraokeLyrics(0);
                    PlayerVM.IsYouTube = SelectedSong.UseYouTube && !string.IsNullOrWhiteSpace(SelectedSong.YouTubeUrl);
                    PlayerVM.YouTubeUrl = SelectedSong.YouTubeUrl ?? string.Empty;
                    TargetFrequency = 0;
                    _rawTargetFrequency = 0;
                    Pitch.Target = 0;
                    Pitch.Current = 0;
                    Lyrics = SelectedSong.Title;
                    Status = PlayerVM.IsYouTube
                        ? "Riproduzione YouTube: " + SelectedSong.Title
                        : SelectedSong.VideoExists
                        ? "Riproduzione: " + SelectedSong.Title
                        : "Riproduzione senza video: " + SelectedSong.Title;

                    VideoPathChanged?.Invoke(PlayerVM.IsYouTube ? PlayerVM.YouTubeUrl : File.Exists(videoPath) ? videoPath : null);
                    _clock.Start();
                    try
                                        {
                                            _midiPlayer.Play(midiPath);
                                            AppServices.Log.Info($"Song started: {SelectedSong.Title}");
                                            _mic.Start();
                                        }
                                        catch(Exception ex)
                                        {
                                            AppServices.Log.Error(ex.ToString());
                                            Status = "Errore riproduzione";
                                            return;
                                        }

                    RegisterSongPlay();

                            try
                            {
                                if (_userLibraryService != null && SelectedSong != null)
                                {
                                    var id = SelectedSong.MidiPath;
                                    var data = _userLibraryService.Load();

                                    if (data.PlayCount.ContainsKey(id))
                                        data.PlayCount[id]++;
                                    else
                                        data.PlayCount[id] = 1;

                                    data.LastPlayed[id] = DateTime.UtcNow;
                                    data.PlayHistory.Add(id);

                                    _userLibraryService.Save(data);

                                    SelectedSong.PlayCount = data.PlayCount[id];
                                    SelectedSong.LastPlayed = data.LastPlayed[id];
                                    OnPropertyChanged(nameof(Songs));
                                }
                            }
                            catch
                            {
                                // ignore persistence errors
                            }

                    try
                    {
                        if (_userLibraryService != null && SelectedSong != null)
                        {
                            var id = SelectedSong.MidiPath;
                            var data = _userLibraryService.Load();

                            if (data.PlayCount.ContainsKey(id))
                                data.PlayCount[id]++;
                            else
                                data.PlayCount[id] = 1;

                            data.LastPlayed[id] = DateTime.UtcNow;
                            data.PlayHistory.Add(id);

                            _userLibraryService.Save(data);

                            SelectedSong.PlayCount = data.PlayCount[id];
                            SelectedSong.LastPlayed = data.LastPlayed[id];
                            OnPropertyChanged(nameof(Songs));
                        }
                    }
                    catch
                    {
                        // ignore persistence errors
                    }
                }
                catch (Exception ex)
                {
                    Lyrics = "Errore avvio canzone";
                    Status = ex.Message;
                }
            });

            StopCommand = new RelayCommand(_ =>
            {
                _clock.Stop();
                _mic.Stop();
                try
                {
                    _midiPlayer.Stop();
                    AppServices.Log.Info($"Song stopped: {SelectedSong?.Title}");
                }
                catch (Exception ex)
                {
                    AppServices.Log.Error(ex.ToString());
                }

                StopVideoRequested?.Invoke();
                TargetFrequency = 0;
                _rawTargetFrequency = 0;
                PlayerVM.Combo = 0;
                PlayerVM.CurrentPitch = 0;
                PlayerVM.CurrentLyricLine = null;
                PlayerVM.LyricLines.Clear();
                _currentLyricLine = string.Empty;
                _currentLyricLineIndex = -1;
                Pitch.Target = 0;
                Pitch.Current = 0;
                Status = "Stop";
                PlayerVM.IsFinished = true;
            });

            PreviousSongCommand = new RelayCommand(_ =>
            {
                if (Songs.Count == 0 || SelectedSong == null)
                    return;

                int index = Songs.IndexOf(SelectedSong);
                SelectedSong = Songs[(index - 1 + Songs.Count) % Songs.Count];
                Status = "Brano selezionato: " + SelectedSong.Title;
            });

            NextSongCommand = new RelayCommand(_ =>
            {
                if (Songs.Count == 0 || SelectedSong == null)
                    return;

                int index = Songs.IndexOf(SelectedSong);
                SelectedSong = Songs[(index + 1) % Songs.Count];
                Status = "Brano selezionato: " + SelectedSong.Title;
            });

            ResetScoreCommand = new RelayCommand(_ =>
            {
                Score = 0;
                PlayerVM.Combo = 0;
                Status = "Score azzerato";
            });

            IncreaseVoiceToneCommand = new RelayCommand(_ => ChangeVoiceKey(2));
            DecreaseVoiceToneCommand = new RelayCommand(_ => ChangeVoiceKey(-2));
            IncreaseVoiceSemitoneCommand = new RelayCommand(_ => ChangeVoiceKey(1));
            DecreaseVoiceSemitoneCommand = new RelayCommand(_ => ChangeVoiceKey(-1));
            ResetVoiceKeyCommand = new RelayCommand(_ =>
            {
                VoiceKeyShiftSemitones = 0;
                Status = VoiceKeyShiftText;
            });

            TestMicrophoneCommand = new RelayCommand(_ =>
            {
                _mic.Stop();
                _pitchHistory.Clear();
                Status = "Test microfono...";
                try
                {
                    _mic.Start();
                    AppServices.Log.Info("Mic started for test");
                }
                catch (Exception ex)
                {
                    AppServices.Log.Error(ex.ToString());
                    Status = "Errore microfono";
                }
            });

            ToggleAutotuneCommand = new RelayCommand(_ =>
            {
                IsAutotuneEnabled = !IsAutotuneEnabled;
                AutoTuneMode = IsAutotuneEnabled ? "Soft" : "Off";
                Status = IsAutotuneEnabled
                    ? "Autotune audio base attivo"
                    : "Autotune audio base disattivo";

                AppServices.Log.Info(IsAutotuneEnabled ? "Pitch engine started" : "Pitch engine stopped");
            });

            CycleAutoTuneModeCommand = new RelayCommand(_ =>
            {
                AutoTuneMode = AutoTuneMode.Equals("Off", StringComparison.OrdinalIgnoreCase)
                    ? "Soft"
                    : AutoTuneMode.Equals("Soft", StringComparison.OrdinalIgnoreCase)
                        ? "Hard"
                        : "Off";

                IsAutotuneEnabled = !AutoTuneMode.Equals("Off", StringComparison.OrdinalIgnoreCase);
                _audioEngine.PitchCorrection.Strength = AutoTuneMode.Equals("Hard", StringComparison.OrdinalIgnoreCase) ? 0.75 : 0.35;
                Status = "Autotune mode: " + AutoTuneMode;

                AppServices.Log.Info($"Pitch engine mode changed: {AutoTuneMode}");
            });

            ToggleMixerCommand = new RelayCommand(_ =>
            {
                if (_audioEngine.IsMixerRunning)
                {
                    _audioEngine.Stop();
                    IsMixerEnabled = false;
                    _configService.Settings.StartMixerEnabled = false;
                    _configService.Save();
                }
                else
                {
                    _audioEngine.Start();
                    IsMixerEnabled = _audioEngine.IsMixerRunning;
                    _configService.Settings.StartMixerEnabled = IsMixerEnabled;
                    _configService.Save();
                }
            });

            ToggleMicrophoneMuteCommand = new RelayCommand(_ =>
            {
                _audioEngine.ToggleMicrophoneMute();
                IsMonitorMuted = _audioEngine.IsMicrophoneMuted;
            });

            IncreaseMicVolumeCommand = new RelayCommand(_ =>
            {
                MicVolume = Math.Min(2.0f, MicVolume + 0.1f);
                Status = $"Volume microfono: {MicVolume:0.0}";
            });

            DecreaseMicVolumeCommand = new RelayCommand(_ =>
            {
                MicVolume = Math.Max(0.0f, MicVolume - 0.1f);
                Status = $"Volume microfono: {MicVolume:0.0}";
            });

            IncreaseMusicVolumeCommand = new RelayCommand(_ =>
            {
                MusicVolume = Math.Min(2.0f, MusicVolume + 0.1f);
                Status = $"Volume musica: {MusicVolume:0.0}";
            });

            DecreaseMusicVolumeCommand = new RelayCommand(_ =>
            {
                MusicVolume = Math.Max(0.0f, MusicVolume - 0.1f);
                Status = $"Volume musica: {MusicVolume:0.0}";
            });

            DownloadCatalogCommand = new RelayCommand(async _ =>
            {
                string catalogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "songs.json");
                var onlineSongs = await _onlineSongService.DownloadCatalogAsync(catalogPath);

                if (onlineSongs.Count == 0)
                {
                    Status = "Catalogo online vuoto";
                    return;
                }

                Songs.Clear();
                foreach (var song in onlineSongs)
                    Songs.Add(song);

                SelectedSong = Songs.Count > 0 ? Songs[0] : null;
                OnlineCatalogStatus = $"CATALOGO: {Songs.Count} canzoni";
                Status = $"Catalogo online simulato: {Songs.Count} canzoni";
            });

            DownloadSelectedSongCommand = new RelayCommand(async _ =>
            {
                if (SelectedSong == null)
                {
                    Status = "Seleziona un brano da scaricare";
                    return;
                }

                string destination = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Songs");
                await _onlineSongService.DownloadSongAsync(SelectedSong, destination);
                Status = "Download pronto: " + SelectedSong.Title;
            });

            CreateMultiplayerCommand = new RelayCommand(_ =>
            {
                _multiplayerService.CreateLocalSession("Player 1");
                _multiplayerService.JoinLocalSession("Player 2");
                MultiplayerStatus = "MULTIPLAYER: 2 PLAYER";
                MultiplayerPlayers = "Players: " + string.Join(", ", _multiplayerService.Players);
            });

            StopMultiplayerCommand = new RelayCommand(_ =>
            {
                _multiplayerService.StopSession();
                MultiplayerStatus = "MULTIPLAYER: OFF";
                MultiplayerPlayers = "Players: -";
            });

            PlayerVM.StartCommand = StartCommand;
            PlayerVM.StopCommand = StopCommand;
            PlayerVM.BackCommand = PreviousSongCommand;

            ToggleFavoriteCommand =
                new RelayCommand(_ =>
                {
                    if (SelectedSong == null || _userLibraryService == null)
                        return; // guard nulls

                    SelectedSong.IsFavorite = !SelectedSong.IsFavorite;

                    if (SelectedSong.IsFavorite)
                    {
                        if (_userLibraryData != null && !_userLibraryData.Favorites
                            .Contains(
                                SelectedSong.Title))
                        {
                            _userLibraryData.Favorites
                                .Add(
                                    SelectedSong.Title);
                        }
                    }
                    else
                    {
                        _userLibraryData?.Favorites
                            .Remove(
                                SelectedSong.Title);
                    }

                    if (_userLibraryData != null)
                        _userLibraryService.Save(
                            _userLibraryData);

                    try
                    {
                        var data = _userLibraryService.Load();
                        var id = SelectedSong.MidiPath ?? SelectedSong.Title;
                        if (SelectedSong.IsFavorite)
                        {
                            if (!data.Favorites.Contains(id))
                                data.Favorites.Add(id);
                        }
                        else
                        {
                            data.Favorites.Remove(id);
                        }
                        _userLibraryService.Save(data);
                    }
                    catch
                    {
                        // ignore persistence errors
                    }

                    Status = SelectedSong.IsFavorite ? "Aggiunto ai preferiti" : "Rimosso dai preferiti";
                                        AppServices.Log.Info(SelectedSong.IsFavorite ? $"Favorite added: {SelectedSong.Title}" : $"Favorite removed: {SelectedSong.Title}");

                    OnPropertyChanged(nameof(Songs));
                });

                // E1: comando per selezionare cartella libreria
                SelectLibraryFolderCommand = new RelayCommand(_ => SelectLibraryFolder());

                // E3: comando per aggiornare la libreria
                RefreshLibraryCommand = new RelayCommand(_ =>
                {
                    Songs = LoadSongs();
                    SelectedSong = Songs.Count > 0 ? Songs[0] : null;
                    Status = Songs.Count > 0 ? $"{Songs.Count} canzoni caricate" : "Nessuna canzone trovata";
                    OnPropertyChanged(nameof(Songs));
                });
        }

        private ObservableCollection<SongViewModel> LoadSongs()
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string configPath = Path.Combine(basePath, "songs.json");

            AppServices.Log.Info($"LoadSongs: basePath={basePath}, configPath={configPath}");

            if (File.Exists(configPath))
            {
                try
                {
                    Library.Load(configPath, basePath);

                    foreach (var song in Library.Songs)
                    {
                        song.IsFavorite = _userLibraryData != null && _userLibraryData.Favorites.Contains(song.Title);
                    }

                    if (Library.Songs.Count > 0)
                    {
                        AppServices.Log.Info($"LoadSongs: loaded {Library.Songs.Count} entries from songs.json");
                        return Library.Songs;
                    }
                }
                catch (Exception ex)
                {
                    Status = "Errore songs.json: " + ex.Message;
                    AppServices.Log.Error("Errore parsing songs.json", ex);
                }
            }

            var songs = new ObservableCollection<SongViewModel>();
            var files = new List<string>();

            // Se è configurato LibraryPath, scansiona ricorsivamente quella cartella
            string? libraryPath = _configService?.Settings?.LibraryPath;
            if (!string.IsNullOrWhiteSpace(libraryPath) && Directory.Exists(libraryPath))
            {
                AppServices.Log.Info($"LoadSongs: scanning LibraryPath={libraryPath} (recursive)");
                try
                {
                    files.AddRange(Directory.GetFiles(libraryPath, "*.mid", SearchOption.AllDirectories));
                    files.AddRange(Directory.GetFiles(libraryPath, "*.midi", SearchOption.AllDirectories));
                    files.AddRange(Directory.GetFiles(libraryPath, "*.kar", SearchOption.AllDirectories));
                }
                catch (Exception ex)
                {
                    AppServices.Log.Error($"Error scanning library path: {ex.Message}", ex);
                }
            }
            else
            {
                AppServices.Log.Info("LoadSongs: scanning default folders (Songs, Assets\\karaoke)");
                foreach (string folder in new[] { Path.Combine(basePath, "Songs"), Path.Combine(basePath, "Assets", "karaoke") })
                {
                    if (!Directory.Exists(folder))
                        continue;

                    files.AddRange(Directory.GetFiles(folder, "*.mid"));
                    files.AddRange(Directory.GetFiles(folder, "*.midi"));
                    files.AddRange(Directory.GetFiles(folder, "*.kar"));
                }
            }

            AppServices.Log.Info($"LoadSongs: found {files.Count} raw files (pre-distinct)");

            foreach (string file in files.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(Path.GetFileNameWithoutExtension))
            {
                AppServices.Log.Info($"LoadSongs: file detected: {file}");
                string title = Path.GetFileNameWithoutExtension(file);
                string karFile = Path.ChangeExtension(file, ".kar");
                string karPath = File.Exists(karFile) ? karFile : file;

                if (Path.GetExtension(file).Equals(".kar", StringComparison.OrdinalIgnoreCase))
                    karPath = file;

                songs.Add(new SongViewModel
                {
                    Title = title,
                    MidiPath = Path.GetRelativePath(basePath, file),
                    KarPath = Path.GetRelativePath(basePath, karPath),
                    VideoPath = Path.Combine("Songs", "song.mp4"),
                    MidiExists = File.Exists(file),
                    KarExists = File.Exists(karPath),
                    VideoExists = File.Exists(Path.Combine(basePath, "Songs", "song.mp4"))
                });
            }

            // Aggiorna anche il viewmodel Library così la UI mostra i risultati
            try
            {
                Library.SetSongs(songs);
                AppServices.Log.Info($"LoadSongs: LibraryViewModel updated with {songs.Count} songs");
            }
            catch (Exception ex)
            {
                AppServices.Log.Error("LoadSongs: error updating Library.SetSongs", ex);
            }

            // Log summary counts
            int karCount = songs.Count(s => s.KarExists);
            int midCount = songs.Count(s => s.MidiExists);
            AppServices.Log.Info($"LoadSongs summary: total={songs.Count}, kar={karCount}, mid={midCount}");

            return songs;
        }

        // E2: apre FolderBrowserDialog e salva LibraryPath nelle impostazioni
        private void SelectLibraryFolder()
        {
            try
            {
                AppServices.Log.Info("SelectLibraryFolder: starting");
                using (var dlg = new FolderBrowserDialog())
                {
                    dlg.Description = "Seleziona cartella Libreria Karaoke";
                    dlg.ShowNewFolderButton = true;

                    AppServices.Log.Info("SelectLibraryFolder: dialog created, about to show");

                    var result = dlg.ShowDialog();
                    AppServices.Log.Info($"SelectLibraryFolder: dialog result = {result}");

                    if (result == DialogResult.OK)
                    {
                        AppServices.Log.Info($"SelectLibraryFolder: user selected {dlg.SelectedPath}");

                        if (_configService != null)
                        {
                            _configService.Settings.LibraryPath = dlg.SelectedPath;
                            _configService.Save();
                            AppServices.Log.Info($"SelectLibraryFolder: LibraryPath saved to config");
                        }

                        Songs = LoadSongs();
                        SelectedSong = Songs.Count > 0 ? Songs[0] : null;
                        Status = Songs.Count > 0 ? $"{Songs.Count} canzoni caricate" : "Nessuna canzone trovata";
                        OnPropertyChanged(nameof(Songs));
                        AppServices.Log.Info($"SelectLibraryFolder: UI updated with {Songs.Count} songs");
                    }
                    else
                    {
                        AppServices.Log.Info("SelectLibraryFolder: user cancelled dialog");
                    }
                }
            }
            catch (Exception ex)
            {
                Status = "Errore selezione cartella: " + ex.Message;
                AppServices.Log.Error("SelectLibraryFolder error", ex);
            }
        }

        private string FormatLyric(string lyric)
        {
            if (string.IsNullOrWhiteSpace(lyric))
                return _currentLyricLine;

            string text = lyric.Trim();

            if (text.StartsWith("@") || text.StartsWith("#"))
                return _currentLyricLine;

            bool newLine = text.StartsWith("/") || text.StartsWith("\\");
            text = text.TrimStart('/', '\\').Trim();

            if (string.IsNullOrWhiteSpace(text))
                return _currentLyricLine;

            if (newLine || _currentLyricLine.Length > 90)
                _currentLyricLine = text;
            else if (text.StartsWith("-"))
                _currentLyricLine += text.Substring(1);
            else if (_currentLyricLine.Length == 0)
                _currentLyricLine = text;
            else
                _currentLyricLine += " " + text;

            UpdateKaraokeHighlight();
            return _currentLyricLine;
        }

        private void ChangeVoiceKey(int semitones)
        {
            VoiceKeyShiftSemitones += semitones;
            Status = VoiceKeyShiftText;
        }

        private float ApplyVoiceKeyShift(double frequency)
        {
            if (frequency <= 0)
                return 0;

            return (float)(frequency * Math.Pow(2, VoiceKeyShiftSemitones / 12.0));
        }

        private void ApplyVoiceKeyShiftToTarget()
        {
            if (TargetFrequency <= 0)
                return;

            TargetFrequency = ApplyVoiceKeyShift(_rawTargetFrequency);
            _audioEngine.PitchCorrection.Processor.TargetFrequency = TargetFrequency;
            Pitch.Target = TargetFrequency;
        }

        private void LoadMidiChannels(string midiPath)
        {
            MidiChannels.Clear();

            var channels = Enumerable.Range(1, 16)
                .ToDictionary(channel => channel, channel => new MidiChannelViewModel
                {
                    ChannelNumber = channel,
                    InstrumentName = channel == 10 ? "Drums" : "Acoustic Grand Piano"
                });

            var midiFile = MidiFileCore.Read(midiPath);

            foreach (var eventTimed in midiFile.GetTimedEvents())
            {
                if (eventTimed.Event is ProgramChangeMidiEvent programChange)
                {
                    int channel = programChange.Channel + 1;
                    int program = programChange.ProgramNumber;

                    if (channels.TryGetValue(channel, out var model))
                        model.InstrumentName = GetGeneralMidiInstrumentName(program);
                }

                if (eventTimed.Event is NoteOnMidiEvent noteOn && noteOn.Velocity > 0)
                {
                    int channel = noteOn.Channel + 1;

                    if (channels.TryGetValue(channel, out var model))
                        model.HasNotes = true;
                }
            }

            foreach (var channel in channels.Values.Where(item => item.HasNotes).OrderBy(item => item.ChannelNumber))
                MidiChannels.Add(channel);
        }

        private static string GetGeneralMidiInstrumentName(int programNumber)
        {
            string[] names =
            {
                "Acoustic Grand Piano", "Bright Acoustic Piano", "Electric Grand Piano", "Honky-tonk Piano",
                "Electric Piano 1", "Electric Piano 2", "Harpsichord", "Clavi",
                "Celesta", "Glockenspiel", "Music Box", "Vibraphone",
                "Marimba", "Xylophone", "Tubular Bells", "Dulcimer",
                "Drawbar Organ", "Percussive Organ", "Rock Organ", "Church Organ",
                "Reed Organ", "Accordion", "Harmonica", "Tango Accordion",
                "Acoustic Guitar Nylon", "Acoustic Guitar Steel", "Electric Guitar Jazz", "Electric Guitar Clean",
                "Electric Guitar Muted", "Overdriven Guitar", "Distortion Guitar", "Guitar Harmonics",
                "Acoustic Bass", "Electric Bass Finger", "Electric Bass Pick", "Fretless Bass",
                "Slap Bass 1", "Slap Bass 2", "Synth Bass 1", "Synth Bass 2",
                "Violin", "Viola", "Cello", "Contrabass",
                "Tremolo Strings", "Pizzicato Strings", "Orchestral Harp", "Timpani",
                "String Ensemble 1", "String Ensemble 2", "SynthStrings 1", "SynthStrings 2",
                "Choir Aahs", "Voice Oohs", "Synth Voice", "Orchestra Hit",
                "Trumpet", "Trombone", "Tuba", "Muted Trumpet",
                "French Horn", "Brass Section", "SynthBrass 1", "SynthBrass 2",
                "Soprano Sax", "Alto Sax", "Tenor Sax", "Baritone Sax",
                "Oboe", "English Horn", "Bassoon", "Clarinet",
                "Piccolo", "Flute", "Recorder", "Pan Flute",
                "Blown Bottle", "Shakuhachi", "Whistle", "Ocarina",
                "Lead 1 Square", "Lead 2 Sawtooth", "Lead 3 Calliope", "Lead 4 Chiff",
                "Lead 5 Charang", "Lead 6 Voice", "Lead 7 Fifths", "Lead 8 Bass+Lead",
                "Pad 1 New Age", "Pad 2 Warm", "Pad 3 Polysynth", "Pad 4 Choir",
                "Pad 5 Bowed", "Pad 6 Metallic", "Pad 7 Halo", "Pad 8 Sweep",
                "FX 1 Rain", "FX 2 Soundtrack", "FX 3 Crystal", "FX 4 Atmosphere",
                "FX 5 Brightness", "FX 6 Goblins", "FX 7 Echoes", "FX 8 Sci-fi",
                "Sitar", "Banjo", "Shamisen", "Koto",
                "Kalimba", "Bag Pipe", "Fiddle", "Shanai",
                "Tinkle Bell", "Agogo", "Steel Drums", "Woodblock",
                "Taiko Drum", "Melodic Tom", "Synth Drum", "Reverse Cymbal",
                "Guitar Fret Noise", "Breath Noise", "Seashore", "Bird Tweet",
                "Telephone Ring", "Helicopter", "Applause", "Gunshot"
            };

            return programNumber >= 0 && programNumber < names.Length
                ? names[programNumber]
                : $"Program {programNumber + 1}";
        }

        private void UpdateKaraokeHighlight()
        {
            int split = Math.Clamp(_currentLyricLine.Length - 1, 0, _currentLyricLine.Length);
            PlayerVM.HighlightedLyrics = _currentLyricLine.Substring(0, split);
            PlayerVM.PendingLyrics = _currentLyricLine.Substring(split);
        }

        private static List<string>? GetExactLyricsLines(SongViewModel song, string basePath)
        {
            var candidates = new List<string>();

            string? configuredLyricsPath = song.GetType().GetProperty("LyricsPath")?.GetValue(song) as string;
            if (!string.IsNullOrWhiteSpace(configuredLyricsPath))
                candidates.Add(Path.Combine(basePath, configuredLyricsPath));

            candidates.Add(Path.Combine(basePath, "Assets", "karaoke", $"{Path.GetFileNameWithoutExtension(song.KarPath)}.txt"));
            candidates.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "canzone per un amica.txt"));

            string? lyricsFile = candidates.FirstOrDefault(File.Exists);
            if (lyricsFile == null)
                return null;

            return File.ReadAllLines(lyricsFile)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToList();
        }

        private void BuildVerticalLyrics(List<LyricEvent> lyrics, List<string>? exactLines = null)
        {
            PlayerVM.LyricLines.Clear();
            _currentLyricLineIndex = -1;

            if (exactLines is { Count: > 0 })
            {
                BuildLyricsFromExactText(lyrics, exactLines);
                return;
            }

            foreach (var lyric in lyrics)
            {
                if (string.IsNullOrWhiteSpace(lyric.Text))
                    continue;

                string text = lyric.Text.Trim();

                if (text.StartsWith("@") || text.StartsWith("#"))
                    continue;

                var line = new LyricLineViewModel();
                line.Words.Add(new KaraokeWordViewModel
                {
                    Text = text
                });

                line.Text = text;
                PlayerVM.LyricLines.Add(line);
            }
        }

        private void AddBuiltLine(LyricLineViewModel line)
        {
            string text = string.Join(" ", line.Words.Select(word => word.Text));
            if (string.IsNullOrWhiteSpace(text))
                return;

            line.Text = text;
            PlayerVM.LyricLines.Add(line);
        }

        private void BuildLyricsFromExactText(List<LyricEvent> timingEvents, List<string> exactLines)
        {
            foreach (string rawLine in exactLines)
            {
                string lineText = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(lineText))
                    continue;

                var line = new LyricLineViewModel();
                string[] words = lineText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                foreach (string word in words)
                {
                    line.Words.Add(new KaraokeWordViewModel
                    {
                        Text = word
                    });
                }

                line.Text = lineText;
                PlayerVM.LyricLines.Add(line);
            }
        }

        private void UpdateKaraokeLyrics(long currentTime)
        {
            if (PlayerVM.LyricLines.Count == 0)
                return;

            int nextIndex = PlayerVM.LyricLines.Count - 1;

            for (int i = 0; i < PlayerVM.LyricLines.Count; i++)
            {
                if (PlayerVM.LyricLines[i].Words.FirstOrDefault()?.Time > currentTime)
                {
                    nextIndex = Math.Max(0, i - 1);
                    break;
                }
            }

            _currentLyricLineIndex = nextIndex;

            for (int i = 0; i < PlayerVM.LyricLines.Count; i++)
            {
                bool isCurrent = i == nextIndex;
                PlayerVM.LyricLines[i].HighlightedText = isCurrent ? PlayerVM.LyricLines[i].Text : string.Empty;
                PlayerVM.LyricLines[i].PendingText = isCurrent ? string.Empty : PlayerVM.LyricLines[i].Text;
            }

            PlayerVM.CurrentLyricLine = PlayerVM.LyricLines[nextIndex];
        }

        private void ResetKaraokeLines()
        {
            foreach (var line in PlayerVM.LyricLines)
            {
                line.HighlightedText = string.Empty;
                line.PendingText = line.Text;
            }
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
        }

        public class PitchState : BaseViewModel
        {
            private float _target;
            public float Target
            {
                get => _target;
                set => SetProperty(ref _target, value);
            }

            private float _current;
            public float Current
            {
                get => _current;
                set => SetProperty(ref _current, value);
            }
        }

        private void RegisterSongPlay()
        {
            if (SelectedSong == null)
                return;

            SelectedSong.PlayCount++;

            SelectedSong.LastPlayed =
                DateTime.Now;

            string key =
                SelectedSong.Title;

            if (!_userLibraryData.PlayCount
                    .ContainsKey(key))
            {
                _userLibraryData.PlayCount[key] = 0;
            }

            _userLibraryData.PlayCount[key]++;

            _userLibraryData.LastPlayed[key] =
                DateTime.Now;

            _userLibraryService.Save(
                _userLibraryData);
        }

private void SavePlaylist()
        {
            _playlistData.Songs.Clear();

foreach (var song in PlayerVM.Playlist)
            {
                if (song?.Song?.Title is null)
                    continue;

                _playlistData.Songs.Add(
                    song.Song.Title);
            }

_playlistPersistence.Save(
                _playlistData);
            AppServices.Log.Info($"Playlist saved. Items: {_playlistData.Songs.Count}");
        }
    }
}
