namespace UBot.Core.Abstractions;

public interface IGameSession
{
    GameClientType ClientType { get; set; }
    object Player { get; }
    object SelectedEntity { get; set; }
    object AcceptanceRequest { get; }
    bool Started { get; set; }
    bool Ready { get; }
    bool Clientless { get; set; }
    ushort Port { get; }
    IReferenceManager ReferenceManager { get; }

    void Initialize();
    void Start();
    bool InitializeArchiveFiles();
    void ShowNotification(string message);
}
