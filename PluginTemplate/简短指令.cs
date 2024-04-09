using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

[ApiVersion(2, 1)]
public class TestPlugin : TerrariaPlugin
{
    public class CC
    {
        public string Name { get; set; }

        public string Cmd { get; set; }

        public DateTime LastTiem { get; set; }

        public CC(string name, string cmd)
        {
            Name = name;
            Cmd = cmd;
            LastTiem = DateTime.UtcNow;
        }
    }

    public override string Name => "简短指令";

    public override string Author => "GK 羽学优化";

    public override Version Version => new Version(1, 0, 2, 0);

    public override string Description => "由GK改良的简短指令插件！";

    public static ConfigFile LConfig { get; set; }

    internal static string LConfigPath => Path.Combine(TShock.SavePath, "简短指令.json");

    private List<CC> LCC { get; set; }

    public static bool jump { get; set; }

    public TestPlugin(Main game)
        : base(game)
    {
        LCC = new List<CC>();
        LConfig = new ConfigFile();
        ((TerrariaPlugin)this).Order = ((TerrariaPlugin)this).Order + 1000000;
    }

    private static void RC()
    {
        try
        {
            if (!File.Exists(LConfigPath))
            {
                TShock.Log.ConsoleError("未找到简短指令配置，已为您创建！");
            }
            LConfig = ConfigFile.Read(LConfigPath);
            LConfig.Write(LConfigPath);
        }
        catch (Exception ex)
        {
            TShock.Log.ConsoleError("简短指令配置读取错误:" + ex.ToString());
        }
    }

    public override void Initialize()
    {
        // 注册配置重新加载事件
        RC();
        GeneralHooks.ReloadEvent += ReloadConfig;
        ServerApi.Hooks.GameInitialize.Register((TerrariaPlugin)(object)this, OnInitialize);
        PlayerHooks.PlayerCommand += OnChat;
    }

    // 重新加载配置的方法    
    private static void ReloadConfig(ReloadEventArgs args)
    {
        // 重新加载配置    
        RC();
        // 向触发重新加载的玩家发送成功消息    
        args.Player?.SendSuccessMessage("[简短指令] 重新加载配置完毕。");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ServerApi.Hooks.GameInitialize.Deregister((TerrariaPlugin)(object)this, OnInitialize);
            PlayerHooks.PlayerCommand -= OnChat;
        }
        base.Dispose(disposing);
    }

    private void OnInitialize(EventArgs args)
    {
        RC();
    }

    //禁用原生指令独立成一个方法
    private void CheckAndBlockOriginalCommands(PlayerCommandEventArgs args2)
    {
        for (int j = 0; j < LConfig.命令表.Count; j++)
        {
            string originalCommandTemplate = LConfig.命令表[j].原始命令;
            string[] originalCommandParts;
            if (!SR(ref originalCommandTemplate, args2.Player.Name, args2.Parameters.ToList(), LConfig.命令表[j].余段补充, out originalCommandParts))
            {
                continue;
            }

            string currentPlayerCommand = string.Join(" ", args2.CommandText.Split(' ').Take(originalCommandParts.Length));

            if (currentPlayerCommand.Equals(originalCommandTemplate) && LConfig.命令表[j].禁用指令)
            {
                args2.Player.SendErrorMessage("此指令已被禁用，故无法使用！");
                args2.Handled = true;
                return;
            }

        }
    }


    private void OnChat(PlayerCommandEventArgs args)
    {
        if (!args.Player.HasPermission("免禁指令"))
        {
            PlayerCommandEventArgs args2 = args;
            if (args2.Handled)
            {
                return;
            }
            if (jump)
            {
                jump = false;
                return;
            }
            //调用“禁用原生指令”的方法
            if (Commands.TShockCommands.Count((Command p) => p.Name == args2.CommandName) > 0 || Commands.ChatCommands.Count((Command p) => p.Name == args2.CommandName) > 0)
            {
                CheckAndBlockOriginalCommands(args2);
            }
            int i;
            for (i = 0; i < LConfig.命令表.Count; i++)
            {
                if (LConfig.命令表[i].新的命令 == "" || !(LConfig.命令表[i].新的命令 == args2.CommandName))
                {
                    continue;
                }
                string O2 = LConfig.命令表[i].原始命令;
                string[] processedCommandParts;
                if (!SR(ref O2, args2.Player.Name, args2.Parameters, LConfig.命令表[i].余段补充, out processedCommandParts))
                {
                    continue;
                }
                if (args2.Player.Index >= 0)
                {
                    if (LConfig.命令表[i].死亡条件 == 1 && (args2.Player.Dead || args2.Player.TPlayer.statLife < 1))
                    {
                        args2.Player.SendErrorMessage("此指令要求你必须活着才能使用！");
                        args2.Handled = true;
                        break;
                    }
                    if (LConfig.命令表[i].死亡条件 == -1 && (!args2.Player.Dead || args2.Player.TPlayer.statLife > 0))
                    {
                        args2.Player.SendErrorMessage("此指令要求你必须死亡才能使用！");
                        args2.Handled = true;
                        break;
                    }
                    int num = CJ(args2.Player.Name, LConfig.命令表[i].新的命令, LConfig.命令表[i].冷却秒数, LConfig.命令表[i].冷却共用);
                    if (num > 0)
                    {
                        args2.Player.SendErrorMessage("此指令正在冷却，还有{0}秒才能使用！", num);
                        args2.Handled = true;
                        break;
                    }
                    jump = true;
                    if (Commands.HandleCommand(args2.Player, $"{args2.CommandPrefix}{O2}"))
                    {
                        lock (LCC)
                        {
                            if (!LCC.Exists((CC t) => t.Name == args2.Player.Name && t.Cmd == LConfig.命令表[i].新的命令))
                            {
                                LCC.Add(new CC(args2.Player.Name, LConfig.命令表[i].新的命令));
                            }
                        }
                    }
                }
                else
                {
                    jump = true;
                    Commands.HandleCommand(args2.Player, $"{args2.CommandPrefix}{O2}");
                }
                args2.Handled = true;
                break;
            }
        }
    }

    private int CJ(string name, string Cmd, int C, bool share)
    {
        lock (LCC)
        {
            for (int i = 0; i < LCC.Count; i++)
            {
                if (share)
                {
                    if (LCC[i].Cmd != Cmd)
                    {
                        continue;
                    }
                }
                else if (LCC[i].Cmd != Cmd || LCC[i].Name != name)
                {
                    continue;
                }
                int num = (int)(DateTime.UtcNow - LCC[i].LastTiem).TotalSeconds;
                num = C - num;
                if (num > 0)
                {
                    return num;
                }
                LCC.RemoveAt(i);
                return 0;
            }
        }
        return 0;
    }

    private bool SR(ref string O, string N, List<string> P, bool R, out string[] processedCommandParts)
    {
        processedCommandParts = new string[] { O };
        string[] commandParts = O.Split(' ');

        for (int i = 0; i < commandParts.Length; i++)
        {
            string currentPart = commandParts[i];
            if (currentPart.StartsWith("{") && currentPart.EndsWith("}"))
            {
                int parameterIndex = int.Parse(currentPart.Substring(1, currentPart.Length - 2)) - 1;
                if (parameterIndex >= 0 && parameterIndex < P.Count)
                {
                    commandParts[i] = P[parameterIndex];
                }
                else
                {
                    return false;
                }
            }
            else if (currentPart == "{player}")
            {
                commandParts[i] = N;
            }
        }


        O = string.Join(" ", commandParts);
        if (R)
        {
            foreach (var extraParam in P.Skip(commandParts.Length))
            {
                O += $" {extraParam}";
            }
        }


        return !O.Contains("{") && !O.Contains("}");
    }
}
