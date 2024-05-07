using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.Collections.Generic;

using Dalamud.Plugin;
using System.Runtime.InteropServices;
using Dalamud.Hooking;
using System.Linq;
using Dalamud.Game.ClientState.Objects.SubKinds;
using System.Numerics;
using Dalamud.Plugin.Services;
using PvpSkillTime;




public class MainWindow : Window, IDisposable
{

    private bool isListeningForSkillUse = true;
    private bool drawConfigWindow = false;
    private DalamudPluginInterface pluginInterface;

    private bool magic = false;
    private HashSet<uint> attackableActorsIds = new HashSet<uint>();

    private Dictionary<uint, int> skillCooldowns = new Dictionary<uint, int>
    {
        { 29054, 30 },  // Defense
        { 29056, 30 },  // Purification
        { 29066, 25 },    // Protection
        { 29482, 30 }
    };
    private readonly Dictionary<uint, string> jobIdMap = new Dictionary<uint, string>
    {
        { 19, "骑士" },
        { 20, "武僧" },
        { 21, "战士" },
        { 22, "龙骑士" },
        { 23, "吟游诗人" },
        { 24, "白魔法师" },
        { 25, "黑魔法师" },
        { 26, "秘术师" },
        { 27, "召唤师" },
        { 28, "学者" },
        { 29, "双剑师" },
        { 30, "忍者" },
        { 31, "机工士" },
        { 32, "暗黑骑士" },
        { 33, "占星术士" },
        { 34, "武士" },
        { 35, "赤魔法师" },
        { 36, "青魔法师" },
        { 37, "绝枪战士" },
        { 38, "舞者" },
        { 39, "钐镰客" },
        { 40, "贤者" }
    };

    private readonly Dictionary<uint, string> skillNameMap = new Dictionary<uint, string>
    {
        { 29054, "防御" },    // Defense
        { 29056, "净化" },    // Purification
        { 29066, "保护" },     // Protection
        { 29482, "金刚极意"}// 添加更多的技能ID和中文名称
    };
    private Dictionary<(uint sourceId, uint skillId), DateTime> cooldownEndTimes = new Dictionary<(uint, uint), DateTime>();
    private CanAttackDelegate CanAttack;
    private delegate int CanAttackDelegate(int arg, IntPtr objectAddress);
    private delegate void ReceiveActionEffectDelegate(uint sourceId, IntPtr sourceCharacter, IntPtr pos, IntPtr effectHeader, IntPtr effectArray, IntPtr effectTrail);
    private Hook<ReceiveActionEffectDelegate> ReceiveActionEffectHook;

    public static readonly string ReceiveActionEffectSig = "E8 ?? ?? ?? ?? 48 8B 8D F0 03 00 00";

    public MainWindow(DalamudPluginInterface pluginInterface) : base("PvpSkillTime by Cindy-Master 闲鱼司马")
    {
        this.pluginInterface = pluginInterface;
        IntPtr canAttackPtr = Service.SigScanner.ScanText("48 89 5C 24 ?? 57 48 83 EC 20 48 8B DA 8B F9 E8 ?? ?? ?? ?? 4C 8B C3");
        CanAttack = Marshal.GetDelegateForFunctionPointer<CanAttackDelegate>(canAttackPtr);
        IntPtr receiveActionEffectPtr = Service.SigScanner.ScanText(ReceiveActionEffectSig);
        ReceiveActionEffectHook = Service.Hook.HookFromSignature<ReceiveActionEffectDelegate>(ReceiveActionEffectSig, ReceiveActionEffect);
        ReceiveActionEffectHook.Enable();
        if (!pluginInterface.IsDev || pluginInterface.SourceRepository == "https://raw.githubusercontent.com/Cindy-Master/DalamudPlugins/main/plugin.json")
        {
            Service.Framework.Update += this.OnFrameworkUpdate;
            pluginInterface.UiBuilder.OpenConfigUi += OnOpenConfig;


        }
        else
        {
            Service.ChatGui.PrintError("请使用https://github.com/Cindy-Master/DalamudPlugins！", "", 0x0033);
        }


    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (!isListeningForSkillUse)
        {
            return;
        }


        if (Service.ClientState.LocalPlayer != null && Service.ClientState.IsPvP)
        {
            RefreshEnemyActorsAndAutoSelect();
        }
    }

    private void RefreshEnemyActorsAndAutoSelect()
    {


        if (Service.ClientState.LocalPlayer == null)
        {
            return;
        }

        lock (attackableActorsIds)
        {
            attackableActorsIds.Clear();
            foreach (var obj in Service.ObjectTable)
            {
                try
                {
                    if (obj is PlayerCharacter rcTemp && obj.ObjectId != Service.ClientState.LocalPlayer.ObjectId && CanAttack(142, obj.Address) == 1)
                    {
                        attackableActorsIds.Add(rcTemp.ObjectId);
                    }
                }
                catch (Exception)
                {

                    continue;
                }
            }
        }
    }
    public override void PreOpenCheck()
    {
        IsOpen = drawConfigWindow;
    }

    private void ReceiveActionEffect(uint sourceId, IntPtr sourceCharacter, IntPtr pos, IntPtr effectHeader, IntPtr effectArray, IntPtr effectTrail)
    {
        if (!isListeningForSkillUse)
        {
            return;
        }

        unsafe
        {
            uint actionId = *((uint*)effectHeader.ToPointer() + 0x2);  // Skill ID
            byte targetType = *((byte*)effectHeader.ToPointer() + 0x1F);  // Action type, 1 for action

            if (targetType != 1)
            {
                return;  // If not action type, return immediately
            }

            if (attackableActorsIds.Contains(sourceId) && skillCooldowns.TryGetValue(actionId, out int cooldown))
            {
                var key = (sourceId, actionId);
                cooldownEndTimes[key] = DateTime.Now.AddSeconds(cooldown);
            }
            if (!magic)
            {
                ReceiveActionEffectHook!.Original(sourceId, sourceCharacter, pos, effectHeader, effectArray, effectTrail);
            }
        }
    }

    public void Dispose()
    {

        Service.Framework.Update -= this.OnFrameworkUpdate;
        ReceiveActionEffectHook?.Disable();
        ReceiveActionEffectHook?.Dispose();
        pluginInterface.UiBuilder.OpenConfigUi -= OnOpenConfig;
    }

    public override void Draw()
    {
        ImGui.Checkbox("开启技能监控", ref isListeningForSkillUse);
        ImGui.Checkbox("栽赃别人移动施法(会有未测试的副作用)", ref magic);

        if (isListeningForSkillUse)
        {
            ImGui.BeginChild("SkillCooldowns", new Vector2(0, 200), true);
            foreach (var entry in cooldownEndTimes)
            {
                var sourceId = entry.Key.sourceId;
                var skillId = entry.Key.skillId;
                var cooldownEnd = entry.Value;
                var remainingTime = cooldownEnd - DateTime.Now;

                if (remainingTime.TotalSeconds > 0)
                {
                    var jobID = GetJobIdFromSourceId(sourceId);
                    if (jobID != null)
                    {
                        var jobName = jobIdMap.ContainsKey(jobID.Value) ? jobIdMap[jobID.Value] : "未知职业";
                        var skillName = skillNameMap.ContainsKey(skillId) ? skillNameMap[skillId] : "未知技能";
                        var character = Service.ObjectTable.FirstOrDefault(obj => obj.ObjectId == sourceId) as PlayerCharacter;
                        if (character != null && character.IsDead)
                        {
                            // 如果对象已死亡，则不显示文本
                            continue;
                        }

                        // 根据技能名设置颜色
                        switch (skillName)
                        {
                            case "防御":
                                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.68f, 0.85f, 0.90f, 1.0f)); // 蓝色
                                break;
                            case "净化":
                                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.0f, 1.0f, 0.0f, 1.0f)); // 绿色
                                break;
                            case "金刚极意":
                                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 1.0f, 0.4f, 1.0f)); // 黄色
                                break;
                            default:
                                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 1.0f, 1.0f, 1.0f)); // 白色
                                break;
                        }

                        ImGui.Text($"{jobName} {skillName} {remainingTime.TotalSeconds:0}s");
                        ImGui.PopStyleColor();
                    }
                }
            }

            ImGui.EndChild();
        }
        if (!drawConfigWindow)
        {
            IsOpen = false; // 此处控制窗口关闭
        }
    }

    public void OnOpenConfig()
    {
        drawConfigWindow = !drawConfigWindow;
    }

    private uint? GetJobIdFromSourceId(uint sourceId)
    {

        var character = Service.ObjectTable.FirstOrDefault(obj => obj.ObjectId == sourceId) as PlayerCharacter;
        return character?.ClassJob.Id;
    }
}