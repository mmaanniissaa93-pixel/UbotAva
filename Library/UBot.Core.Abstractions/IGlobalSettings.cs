using System;
using System.Collections.Generic;

namespace UBot.Core.Abstractions;

public interface IGlobalSettings
{
    void Load();
    bool Exists(string key);
    T Get<T>(string key, T defaultValue = default);
    TEnum GetEnum<TEnum>(string key, TEnum defaultValue = default)
        where TEnum : struct;
    void Set<T>(string key, T value);
    T[] GetArray<T>(
        string key,
        char delimiter = ',',
        StringSplitOptions options = StringSplitOptions.RemoveEmptyEntries);
    void SetArray<T>(string key, IEnumerable<T> values, string delimiter = ",");
    void Save();
}
