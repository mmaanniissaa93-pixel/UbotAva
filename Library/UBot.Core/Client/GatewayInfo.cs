using UBot.Core.Event;

namespace UBot.Core.Client;

public class GatewayInfo
{
    #region Constants

    internal const string Filename = @"GATEPORT.TXT";

    #endregion Constants

    public ushort Port { get; set; }

    /// <summary>
    ///     Loads this instance.
    /// </summary>
    /// <returns></returns>
    internal static GatewayInfo Load()
    {
        var result = new GatewayInfo();
        if (UBot.Core.RuntimeAccess.Session.MediaPk2 == null)
        {
            Log.Notify("Could not load the GATEPORT.TXT file, because there is no active Archive.");
            return result;
        }

        if (!UBot.Core.RuntimeAccess.Session.MediaPk2.TryGetFile(Filename, out var file))
        {
            Log.Error("Could not load the GATEPORT.txt file, because the file was not found.");
            return result;
        }

        if (!ushort.TryParse(file.ReadAllText(), out var port))
        {
            Log.Error("Could not load the GATEPORT.txt file, because the data doesn't contain port information");
            return result;
        }

        result.Port = port;

        UBot.Core.RuntimeAccess.Events.FireEvent("OnLoadGateport", result);

        return result;
    }
}
