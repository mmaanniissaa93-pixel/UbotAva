using System.Threading.Tasks;
using Forms = System.Windows.Forms;

namespace UBot.Avalonia.Services;

internal sealed class UbotDialogService : UbotServiceBase
{
    public Task<string> PickExecutableAsync()
    {
        using var dialog = new Forms.OpenFileDialog
        {
            Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false,
            Title = "Select Silkroad executable"
        };

        var result = dialog.ShowDialog();
        return Task.FromResult(result == Forms.DialogResult.OK ? dialog.FileName : string.Empty);
    }

    public Task<string> PickSoundFileAsync()
    {
        using var dialog = new Forms.OpenFileDialog
        {
            Filter = "WAV files (*.wav)|*.wav|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false,
            Title = "Select sound file"
        };

        var result = dialog.ShowDialog();
        return Task.FromResult(result == Forms.DialogResult.OK ? dialog.FileName : string.Empty);
    }

    public Task<string> PickScriptFileAsync()
    {
        using var dialog = new Forms.OpenFileDialog
        {
            Filter = "Script files (*.txt;*.script)|*.txt;*.script|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false,
            Title = "Select script file"
        };

        var result = dialog.ShowDialog();
        return Task.FromResult(result == Forms.DialogResult.OK ? dialog.FileName : string.Empty);
    }
}

