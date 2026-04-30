using System.Collections.Generic;
using UBot.Core.Abstractions;

namespace UBot.Core;

public sealed class PlayerSettings : IPlayerSettings
{
    public void Load(string characterName) => PlayerConfig.Load(characterName);
    public bool Exists(string key) => PlayerConfig.Exists(key);
    public T Get<T>(string key, T defaultValue = default) => PlayerConfig.Get(key, defaultValue);
    public TEnum GetEnum<TEnum>(string key, TEnum defaultValue = default)
        where TEnum : struct => PlayerConfig.GetEnum(key, defaultValue);
    public T[] GetArray<T>(string key, char delimiter = ',') => PlayerConfig.GetArray<T>(key, delimiter);
    public TEnum[] GetEnums<TEnum>(string key, char delimiter = ',')
        where TEnum : struct => PlayerConfig.GetEnums<TEnum>(key, delimiter);
    public void Set<T>(string key, T value) => PlayerConfig.Set(key, value);
    public void SetArray<T>(string key, IEnumerable<T> values, string delimiter = ",") =>
        PlayerConfig.SetArray(key, values, delimiter);
    public void Save() => PlayerConfig.Save();
}
