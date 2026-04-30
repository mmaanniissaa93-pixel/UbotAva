using System;
using System.Collections.Generic;
using UBot.Core.Abstractions;
using GConfig = UBot.Core.GlobalConfig;

namespace UBot.Core;

public sealed class GlobalSettings : IGlobalSettings
{
    public void Load() => GConfig.Load();
    public bool Exists(string key) => GConfig.Exists(key);
    public T Get<T>(string key, T defaultValue = default) => GConfig.Get(key, defaultValue);
    public TEnum GetEnum<TEnum>(string key, TEnum defaultValue = default)
        where TEnum : struct => GConfig.GetEnum(key, defaultValue);
    public void Set<T>(string key, T value) => GConfig.Set(key, value);
    public T[] GetArray<T>(
        string key,
        char delimiter = ',',
        StringSplitOptions options = StringSplitOptions.RemoveEmptyEntries) =>
        GConfig.GetArray<T>(key, delimiter, options);
    public void SetArray<T>(string key, IEnumerable<T> values, string delimiter = ",") =>
        GConfig.SetArray(key, values, delimiter);
    public void Save() => GConfig.Save();
}
