using CommunityToolkit.Mvvm.ComponentModel;
using UBot.Avalonia.Services;

namespace UBot.Avalonia.ViewModels;

public record TranslationBundle(
    string Start, string Stop, string Disconnect, string Save,
    string On, string Off, string WaitingCharacter,
    string Profile, string Division,
    string GroupCore, string GroupAuto, string GroupData, string GroupOther);

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty] private bool   _isDarkTheme   = true;
    [ObservableProperty] private string _language      = "English";
    [ObservableProperty] private string _activeSection = "UBot.General";

    private static readonly TranslationBundle En = new(
        "Start","Stop","Disconnect","Save","ON","OFF","Waiting for character...",
        "Profile","Division","Core","Automation","Data","Other");
    private static readonly TranslationBundle Tr = new(
        "Başlat","Durdur","Bağlantıyı Kes","Kaydet","AÇIK","KAPALI","Karakter Bekleniyor...",
        "Profil","Sunucu Grubu","Temel","Otomasyon","Veri","Diğer");

    public TranslationBundle T => Language == "Turkish" ? Tr : En;

    public void SetTheme(bool dark)      => IsDarkTheme = dark;
    public void SetLanguage(string lang) => Language    = lang;
}
