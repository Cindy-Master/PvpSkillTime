using Dalamud.Configuration;
using System;
using System.Collections.Generic;

[Serializable]
public class PluginConfiguration : IPluginConfiguration
{
    public int Version { get; set; } = 1;  // 用于配置版本控制，可以在未来升级配置结构时使用

    // 存储要监视的技能ID
    public HashSet<uint> SkillsToMonitor { get; set; } = new HashSet<uint>();

    // 可以添加更多配置项
}
