using System;
using System.Collections.ObjectModel;

namespace KaraokeApp.Services
{
    public class MultiplayerService
    {
        public ObservableCollection<string> Players { get; } = new ObservableCollection<string>();
        public bool IsSessionActive { get; private set; }

        public event Action<string>? OnStatusChanged;

        public void CreateLocalSession(string playerName)
        {
            Players.Clear();
            Players.Add(playerName);
            IsSessionActive = true;
            OnStatusChanged?.Invoke("Sessione multiplayer locale creata");
        }

        public void JoinLocalSession(string playerName)
        {
            if (!Players.Contains(playerName))
                Players.Add(playerName);

            IsSessionActive = true;
            OnStatusChanged?.Invoke(playerName + " è entrato nella sessione");
        }

        public void StopSession()
        {
            IsSessionActive = false;
            Players.Clear();
            OnStatusChanged?.Invoke("Sessione multiplayer chiusa");
        }
    }
}
