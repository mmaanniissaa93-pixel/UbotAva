using UBot.Core.Abstractions;

namespace UBot.Core.Objects.Item;

public class MagicOptionInfo
{
    public uint Id { get; set; }
    public uint Value { get; set; }

    public dynamic Record => ReferenceProvider.Instance?.GetMagicOption(Id);
}
