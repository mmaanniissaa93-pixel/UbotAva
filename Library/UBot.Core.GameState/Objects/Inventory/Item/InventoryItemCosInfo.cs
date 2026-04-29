using UBot.Core.Abstractions;
using UBot.GameData.ReferenceObjects;

namespace UBot.Core.Objects.Item;

public struct InventoryItemCosInfo
{
    /// <summary>
    ///     Model Id
    /// </summary>
    public uint Id;

    /// <summary>
    ///     The name
    /// </summary>
    public string Name;

    private byte _level;

    /// <summary>
    ///     The level (Do not use lower then chinese clients!)
    /// </summary>
    public byte Level
    {
        get
        {
            var context = GameStateRuntimeProvider.Instance;
            if (context?.ClientType >= GameClientType.Chinese_Old)
                return _level;

            var record = context?.GetReference("RefObjChar", Id) as RefObjChar;
            if (record != null)
                return record.Level;

            return 1;
        }
        set => _level = value;
    }

    /// <summary>
    ///     The rental
    /// </summary>
    public RentInfo Rental;
}
