using UBot.Core.Abstractions.Services;

namespace UBot.Core.Abstractions;

public interface IGameSession
{
    GameClientType ClientType { get; set; }
    object Player { get; internal set; }
    object SelectedEntity { get; set; }
    bool Clientless { get; set; }
    bool Started { get; set; }
    bool Ready { get; internal set; }
    ushort Port { get; }
    IReferenceManager ReferenceManager { get; set; }
    object MediaPk2 { get; set; }
    object DataPk2 { get; set; }
    object Party { get; internal set; }
    byte[] MacAddress { get; set; }
    object AcceptanceRequest { get; set; }

    void Start();
    void Initialize();
    bool InitializeArchiveFiles();
    void ShowNotification(string message);
}