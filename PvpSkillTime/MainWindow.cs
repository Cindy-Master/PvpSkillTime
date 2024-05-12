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
    private PluginConfiguration config;
    private bool isListeningForSkillUse = true;
    private bool drawConfigWindow = false;
    private DalamudPluginInterface pluginInterface;
    private string currentItem = "";
    private Dictionary<uint, string> searchResults = new Dictionary<uint, string>();
    private HashSet<uint> skillToMonitor = new HashSet<uint>();
    private bool showSkillSearchWindow = false;

    private bool magic = false;
    private HashSet<uint> attackableActorsIds = new HashSet<uint>();

    public Dictionary<uint, int> skillCooldowns = new Dictionary<uint, int>
    {
        {29546,60},
        {29228,45},
        {9981,30},
        {29054,30},
        {29056,30},
        {29094,30},
        {29236,30},
        {29253,30},
        {29260,30},
        {29395,30},
        {29412,30},
        {29511,30},
        {29659,30},
        {29669,30},
        {29066,25},
        {29400,25},
        {29482,25},
        {29496,25},
        {29533,25},
        {29697,25},
        {29698,25},
        {29067,20},
        {29070,20},
        {29080,20},
        {29082,20},
        {29105,20},
        {29124,20},
        {29125,20},
        {29126,20},
        {29128,20},
        {29129,20},
        {29227,20},
        {29229,20},
        {29235,20},
        {29238,20},
        {29249,20},
        {29267,20},
        {29393,20},
        {29404,20},
        {29409,20},
        {29414,20},
        {29421,20},
        {29422,20},
        {29428,20},
        {29429,20},
        {29480,20},
        {29491,20},
        {29494,20},
        {29509,20},
        {29513,20},
        {29530,20},
        {29535,20},
        {29536,20},
        {29549,20},
        {29552,20},
        {29657,20},
        {29670,20},
        {29674,20},
        {29679,20},
        {29689,20},
        {29692,20},
        {29695,20},
        {29696,20},
        {29716,20},
        {9975,15},
        {9980,15},
        {29064,15},
        {29081,15},
        {29084,15},
        {29092,15},
        {29093,15},
        {29102,15},
        {29123,15},
        {29224,15},
        {29226,15},
        {29232,15},
        {29233,15},
        {29234,15},
        {29243,15},
        {29244,15},
        {29245,15},
        {29246,15},
        {29247,15},
        {29248,15},
        {29258,15},
        {29259,15},
        {29262,15},
        {29396,15},
        {29397,15},
        {29398,15},
        {29399,15},
        {29479,15},
        {29481,15},
        {29490,15},
        {29507,15},
        {29547,15},
        {29548,15},
        {29566,15},
        {29658,15},
        {29661,15},
        {29663,15},
        {29667,15},
        {29671,15},
        {29672,15},
        {29699,15},
        {29700,15},
        {29737,15},
        {9974,10},
        {9978,10},
        {29065,10},
        {29069,10},
        {29079,10},
        {29083,10},
        {29095,10},
        {29097,10},
        {29130,10},
        {29230,10},
        {29237,10},
        {29255,10},
        {29261,10},
        {29266,10},
        {29401,10},
        {29405,10},
        {29406,10},
        {29407,10},
        {29408,10},
        {29415,10},
        {29430,10},
        {29432,10},
        {29484,10},
        {29485,10},
        {29493,10},
        {29497,10},
        {29505,10},
        {29515,10},
        {29532,10},
        {29537,10},
        {29550,10},
        {29553,10},
        {29660,10},
        {29662,10},
        {29673,10},
        {29678,10},
        {29701,10},
        {29704,10},
        {29705,10},
        {9971,5},
        {9977,5},
        {29055,5},
        {9973,3},
        {9979,3},
        {10057,3},
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


    public Dictionary<uint, string> SkillDictionary = new Dictionary<uint, string>
        {
            {9971,"制导"},
            {9973,"回旋碎踢"},
            {9974,"交叉光剑"},
            {9975,"超大型导弹"},
            {9977,"喷射蒸汽"},
            {9978,"大火炎放射"},
            {9979,"双重火箭飞拳"},
            {9980,"巨型光束炮"},
            {9981,"装填青磷水"},
            {9982,"炮击"},
            {10057,"降落"},
            {18923,"飞刀"},
            {18924,"血雨飞花"},
            {18925,"八卦无刃杀"},
            {29054,"防御"},
            {29055,"军用圣灵药"},
            {29056,"净化"},
            {29057,"冲刺"},
            {29058,"先锋剑"},
            {29059,"暴乱剑"},
            {29060,"王权剑"},
            {29061,"赎罪剑"},
            {29062,"ホーリースピリット"},
            {29063,""},
            {29064,"盾牌猛击"},
            {29065,"调停"},
            {29066,"卫护"},
            {29067,"圣盾阵"},
            {29068,"圣盾阵"},
            {29069,"列阵"},
            {29070,"悔罪"},
            {29071,"信念之剑"},
            {29072,"真理之剑"},
            {29073,"英勇之剑"},
            {29074,"重劈"},
            {29075,"凶残裂"},
            {29076,"暴风斩"},
            {29078,"裂石飞环"},
            {29079,"猛攻"},
            {29080,"群山隆起"},
            {29081,"献身"},
            {29082,"原初的血气"},
            {29083,"原初的怒号"},
            {29084,"蛮荒崩裂"},
            {29085,"重斩"},
            {29086,"吸收斩"},
            {29087,"噬魂斩"},
            {29088,"血溅"},
            {29089,"アビサルドレイン"},
            {29090,"漆黒の剣"},
            {29091,"暗影使者"},
            {29092,"跳斩"},
            {29093,"至黑之夜"},
            {29094,"腐秽大地"},
            {29095,"腐秽黑暗"},
            {29096,"腐秽黑暗"},
            {29097,"夜昏"},
            {29098,"利刃斩"},
            {29099,"残暴弹"},
            {29100,"迅连斩"},
            {29101,"爆发击"},
            {29102,"烈牙"},
            {29103,"猛兽爪"},
            {29104,"凶禽爪"},
            {29105,"倍攻"},
            {29106,"续剑"},
            {29107,"超高速"},
            {29108,"撕喉"},
            {29109,"裂膛"},
            {29110,"穿目"},
            {29111,"超高速"},
            {29112,"撕喉"},
            {29113,"裂膛"},
            {29114,"穿目"},
            {29115,"超高速"},
            {29116,"撕喉"},
            {29117,"裂膛"},
            {29118,"穿目"},
            {29119,"超高速"},
            {29120,"撕喉"},
            {29121,"裂膛"},
            {29122,"穿目"},
            {29123,"粗分斩"},
            {29124,"抽取融合"},
            {29125,"融合释放"},
            {29126,"星云"},
            {29127,""},
            {29128,"爆破领域"},
            {29129,"极光"},
            {29130,"连续剑"},
            {29131,"终结击"},
            {29223,"闪灼"},
            {29224,"救疗"},
            {29225,"愈疗"},
            {29226,"苦难之心"},
            {29227,"水流幕"},
            {29228,"自然的奇迹"},
            {29229,"炽天强袭"},
            {29230,"涤罪之心"},
            {29231,"极炎法"},
            {29232,"鼓舞激励之策"},
            {29233,"蛊毒法"},
            {29234,"展开战术"},
            {29235,"枯骨法"},
            {29236,"疾风怒涛之计"},
            {29237,"炽天召唤"},
            {29238,"慰藉"},
            {29239,"炽天之翼"},
            {29240,"炽天的幕帘"},
            {29241,"慰藉"},
            {29242,"落陷凶星"},
            {29243,"吉星相位"},
            {29244,"中重力"},
            {29245,"双重咏唱"},
            {29246,"落陷凶星"},
            {29247,"吉星相位"},
            {29248,"中重力"},
            {29249,"抽卡"},
            {29250,"太阳神之衡"},
            {29251,"河流神之瓶"},
            {29252,"建筑神之塔"},
            {29253,"大宇宙"},
            {29254,"小宇宙"},
            {29255,"星河漫天"},
            {29256,"注药III"},
            {29257,"均衡注药III"},
            {29258,"均衡"},
            {29259,"发炎III"},
            {29260,"魂灵风息"},
            {29261,"神翼"},
            {29262,"箭毒"},
            {29263,"箭毒II"},
            {29264,"心关"},
            {29265,"カルディア"},
            {29266,"中庸之道"},
            {29267,"中庸之道"},
            {29371,"秽浊"},
            {29391,"强劲射击"},
            {29392,"完美音调"},
            {29393,"绝峰箭"},
            {29394,"爆破箭"},
            {29395,"默者的夜曲"},
            {29396,"九天连箭"},
            {29397,"九天连箭"},
            {29398,"九天连箭"},
            {29399,"后跃射击"},
            {29400,"光阴神的礼赞凯歌"},
            {29401,"英雄的幻想曲"},
            {29402,"蓄力冲击"},
            {29403,"热冲击"},
            {29404,"霰弹枪"},
            {29405,"钻头"},
            {29406,"毒菌冲击"},
            {29407,"空气锚"},
            {29408,"回转飞锯"},
            {29409,"野火"},
            {29410,"野火"},
            {29411,"野火"},
            {29412,"象式浮空炮塔"},
            {29413,"以太炮"},
            {29414,"分析"},
            {29415,"魔弹射手"},
            {29416,"瀑泻"},
            {29417,"喷泉"},
            {29418,"逆瀑泻"},
            {29419,"坠喷泉"},
            {29420,"剑舞"},
            {29421,"流星舞"},
            {29422,"刃舞"},
            {29423,"刃舞&#183;终"},
            {29424,"刃舞&#183;终"},
            {29425,"刃舞&#183;终"},
            {29426,"刃舞&#183;终"},
            {29427,"刃舞&#183;终"},
            {29428,"扇舞"},
            {29429,"治疗之华尔兹"},
            {29430,"前冲步"},
            {29431,"闭式舞姿"},
            {29432,"行列舞"},
            {29469,"终结击"},
            {29470,"刃舞&#183;终"},
            {29472,"连击"},
            {29473,"正拳"},
            {29474,"崩拳"},
            {29475,"双龙脚"},
            {29476,"双掌打"},
            {29477,"破碎拳"},
            {29478,"梦幻斗舞"},
            {29479,"六合星导脚"},
            {29480,"万象斗气圈"},
            {29481,"凤凰舞"},
            {29482,"金刚极意"},
            {29483,"金刚转轮"},
            {29484,"轻身步法"},
            {29485,"陨石冲击"},
            {29486,"龙眼雷电"},
            {29487,"龙牙龙爪"},
            {29488,"龙尾大回旋"},
            {29489,"苍穹刺"},
            {29490,"樱花缭乱"},
            {29491,"武神枪"},
            {29492,"死者之岸"},
            {29493,"高跳"},
            {29494,"回避跳跃"},
            {29495,"天龙点睛"},
            {29496,"恐惧咆哮"},
            {29497,"冲天"},
            {29498,"天穹破碎"},
            {29499,"天穹破碎"},
            {29500,"双刃旋"},
            {29501,"绝风"},
            {29502,"旋风刃"},
            {29503,"断绝"},
            {29504,"劫火灭却之术"},
            {29505,"风魔手里剑"},
            {29506,"冰晶乱流之术"},
            {29507,"三印自在"},
            {29508,"命水"},
            {29509,"夺取"},
            {29510,"月影雷兽爪"},
            {29511,"分身之术"},
            {29512,"风遁之术"},
            {29513,"缩地"},
            {29514,"土遁之术"},
            {29515,"星遁天诛"},
            {29516,"星遁天诛"},
            {29517,"双刃旋"},
            {29518,"绝风"},
            {29519,"旋风刃"},
            {29520,"断绝"},
            {29521,"风魔手里剑"},
            {29522,"月影雷兽爪"},
            {29523,"雪风"},
            {29524,"月光"},
            {29525,"花车"},
            {29526,"冰雪"},
            {29527,"满月"},
            {29528,"樱花"},
            {29529,"纷乱雪月花"},
            {29530,"奥义斩浪"},
            {29531,"回返斩浪"},
            {29532,"必杀剑&#183;早天"},
            {29533,"必杀剑&#183;地天"},
            {29534,"地天返"},
            {29535,"刀背击打"},
            {29536,"明镜止水"},
            {29537,"斩铁剑"},
            {29538,"切割"},
            {29539,"增盈切割"},
            {29540,"地狱切割"},
            {29543,"虚无收割"},
            {29544,"交错收割"},
            {29545,"收获月"},
            {29546,"大丰收"},
            {29547,"束缚挥割"},
            {29548,"夜游魂钐割"},
            {29549,"斩首令"},
            {29550,"地狱入境"},
            {29551,"回退"},
            {29552,"神秘纹"},
            {29553,"暗夜游魂"},
            {29554,"团契"},
            {29557,"连续剑"},
            {29558,"刃舞"},
            {29559,"幻影野槌"},
            {29566,"灵魂切割"},
            {29603,"斩首令"},
            {29649,"火炎"},
            {29650,"炽炎"},
            {29651,"核爆"},
            {29652,"核爆"},
            {29653,"冰结"},
            {29654,"冰澈"},
            {29655,"玄冰"},
            {29656,"玄冰"},
            {29657,"磁暴"},
            {29658,"ゼノグロシー"},
            {29659,"夜翼"},
            {29660,"以太步"},
            {29661,"热震荡"},
            {29662,"灵魂共鸣"},
            {29663,"悖论"},
            {29664,"毁荡"},
            {29665,"星极脉冲"},
            {29666,"灵泉之炎"},
            {29667,"深红旋风"},
            {29668,"深红强袭"},
            {29669,"螺旋气流"},
            {29670,"守护之光"},
            {29671,"山崩"},
            {29672,"溃烂爆发"},
            {29673,"龙神召唤"},
            {29674,"龙神迸发"},
            {29675,"百万核爆"},
            {29676,"真龙波"},
            {29677,"死亡轮回"},
            {29678,"不死鸟召唤"},
            {29679,"不死鸟迸发"},
            {29680,"不死鸟之翼"},
            {29681,"赤焰"},
            {29682,"天启"},
            {29683,"赤飞石"},
            {29684,"赤暴风"},
            {29685,"赤神圣"},
            {29686,"赤火炎"},
            {29687,"赤暴雷"},
            {29688,"赤核爆"},
            {29689,"魔回刺"},
            {29690,"魔交击斩"},
            {29691,"魔连攻"},
            {29692,"魔回刺"},
            {29693,"魔交击斩"},
            {29694,"魔连攻"},
            {29695,"决断"},
            {29696,"决断"},
            {29697,"抗死"},
            {29698,"疲惫"},
            {29699,"短兵相接"},
            {29700,"移转"},
            {29701,"フレッシュ"},
            {29702,"黑魔元转换"},
            {29703,"白魔元转换"},
            {29704,"南天十字"},
            {29705,"南天十字"},
            {29706,"魂灵风息"},
            {29707,"月影雷兽牙"},
            {29708,"月影雷兽牙"},
            {29711,"自愈"},
            {29716,"連環計"},
            {29735,"防御"},
            {29736,"混沌旋风"},
            {29737,"寂灭"},
            {29738,"暗影使者"},
            {29783,"世界树之干"},
            {29784,"放浪神之箭"},
            {34786,"断首"},
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

        this.config = pluginInterface.GetPluginConfig() as PluginConfiguration ?? new PluginConfiguration();

        // 加载技能监控列表到 HashSet
        this.skillToMonitor = config.SkillsToMonitor ?? new HashSet<uint>();
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
        if (!magic)
        {
            ReceiveActionEffectHook!.Original(sourceId, sourceCharacter, pos, effectHeader, effectArray, effectTrail);
        }
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

            // 检查是否监控此技能和是否可以攻击
            if (skillToMonitor.Contains(actionId) && attackableActorsIds.Contains(sourceId) && skillCooldowns.TryGetValue(actionId, out int cooldown))
            {
                var key = (sourceId, actionId);
                cooldownEndTimes[key] = DateTime.Now.AddSeconds(cooldown);
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
        ImGui.Checkbox("监控技能清单", ref showSkillSearchWindow);

        if (showSkillSearchWindow)
        {
            ImGui.InputText("搜索技能", ref currentItem, 100);
            ImGui.SameLine();
            if (ImGui.Button("搜索"))
            {
                PerformSearch();
            }

            // 展示搜索结果
            if (searchResults.Count > 0)
            {
                ImGui.BeginChild("搜索结果", new Vector2(0, 150), true);
                foreach (var result in searchResults)
                {
                    ImGui.Text($"{result.Value} (ID: {result.Key})");
                    ImGui.SameLine();
                    if (ImGui.Button($"添加##{result.Key}"))
                    {
                        AddSkillToMonitor(result.Key);
                    }
                }
                ImGui.EndChild();
            }

            // 展示当前监视的技能列表
            ImGui.BeginChild("监视列表", new Vector2(0, 150), true);
            foreach (var skillId in config.SkillsToMonitor)  // 使用配置中的集合
            {
                if (SkillDictionary.TryGetValue(skillId, out string skillName))
                {
                    ImGui.Text($"{skillName} (ID: {skillId})");
                    ImGui.SameLine();
                    if (ImGui.Button($"移除##{skillId}"))
                    {
                        RemoveSkillFromMonitor(skillId);
                    }
                }
            }
            ImGui.EndChild();
        }


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
                        var skillName = SkillDictionary.ContainsKey(skillId) ? SkillDictionary[skillId] : "未知技能";
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
    private void PerformSearch()
    {
        searchResults.Clear();  // 清空之前的搜索结果
        foreach (var skill in SkillDictionary)  // 假设技能名称存在于 SkillDictionary 中
        {
            if (skill.Value.ToLower().Contains(currentItem.ToLower()))
            {
                searchResults.Add(skill.Key, skill.Value);
            }
        }
    }

    private void AddSkillToMonitor(uint skillId)
    {
        if (!config.SkillsToMonitor.Contains(skillId))
        {
            config.SkillsToMonitor.Add(skillId);
            pluginInterface.SavePluginConfig(config);
        }
    }

    private void RemoveSkillFromMonitor(uint skillId)
    {
        if (config.SkillsToMonitor.Contains(skillId))
        {
            config.SkillsToMonitor.Remove(skillId);
            pluginInterface.SavePluginConfig(config);
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