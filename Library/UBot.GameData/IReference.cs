namespace UBot.Core.Client;

public interface IReference
{
    bool Load(ReferenceParser parser);
}

public interface IReference<TKey> : IReference
{
    TKey PrimaryKey { get; }
}
