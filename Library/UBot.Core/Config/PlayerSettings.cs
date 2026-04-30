using System.Collections.Generic;
using UBot.Core.Abstractions;
using PConfig = UBot.Core.PlayerConfig;

namespace UBot.Core;

public sealed class PlayerSettings : IPlayerSettings
{
    public void Load(string characterName) => PConfig.Load(characterName);
    public bool Exists(string key) => PConfig.Exists(key);
    public T Get<T>(string key, T defaultValue = default) => PConfig.Get(key, defaultValue);
    public TEnum GetEnum<TEnum>(string key, TEnum defaultValue = default)
        where TEnum : struct => PConfig.GetEnum(key, defaultValue);
    public T[] GetArray<T>(string key, char delimiter = ',') => PConfig.GetArray<T>(key, delimiter);
    public TEnum[] GetEnums<TEnum>(string key, char delimiter = ',')
        where TEnum : struct => PConfig.GetEnums<TEnum>(key, delimiter);
    public void Set<T>(string key, T value) => PConfig.Set(key, value);
    public void SetArray<T>(string key, IEnumerable<T> values, string delimiter = ",") =>
        PConfig.SetArray(key, values, delimiter);
    public void Save() => PConfig.Save();
}
