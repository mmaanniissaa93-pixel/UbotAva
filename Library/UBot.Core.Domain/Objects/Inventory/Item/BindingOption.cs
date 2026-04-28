namespace UBot.Core.Objects.Item;

public class BindingOption
{
    public BindingOptionType Type { get; set; }
    public byte Slot { get; set; }
    public uint Id { get; set; }
    public uint Value { get; set; }
}

public enum BindingOptionType : byte
{
    Socket = 1,
    AdvancedElixir = 2,
}
