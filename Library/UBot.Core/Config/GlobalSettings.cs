using System;
using System.Collections.Generic;
using UBot.Core.Abstractions;

namespace UBot.Core;

public sealed class GlobalSettings : IGlobalSettings
{
    public void Load() => GlobalConfig.Load();
    public bool Exists(string key) => GlobalConfig.Exists(key);
    public T Get<T>(string key, T defaultValue = default) => GlobalConfig.Get(key, defaultValue);
    public TEnum GetEnum<TEnum>(string key, TEnum defaultValue = default)
        where TEnum : struct => GlobalConfig.GetEnum(key, defaultValue);
    public void Set<T>(string key, T value) => GlobalConfig.Set(key, value);
    public T[] GetArray<T>(
        string key,
        char delimiter = ',',
        StringSplitOptions options = StringSplitOptions.RemoveEmptyEntries) =>
        GlobalConfig.GetArray<T>(key, delimiter, options);
    public void SetArray<T>(string key, IEnumerable<T> values, string delimiter = ",") =>
        GlobalConfig.SetArray(key, values, delimiter);
    public void Save() => GlobalConfig.Save();
}
