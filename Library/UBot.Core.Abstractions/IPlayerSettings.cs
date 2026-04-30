using System.Collections.Generic;

namespace UBot.Core.Abstractions;

public interface IPlayerSettings
{
    void Load(string charName);
    bool Exists(string key);
    T Get<T>(string key, T defaultValue = default);
    TEnum GetEnum<TEnum>(string key, TEnum defaultValue = default)
        where TEnum : struct;
    TEnum[] GetEnums<TEnum>(string key, char delimiter = ',')
        where TEnum : struct;
    void Set<T>(string key, T value);
    T[] GetArray<T>(string key, char delimiter = ',');
    void SetArray<T>(string key, IEnumerable<T> values, string delimiter = ",");
    void Save();
}