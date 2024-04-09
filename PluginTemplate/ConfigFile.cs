

// ClassLibrary1, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// TestPlugin.ConfigFile
using Newtonsoft.Json;
using System.ComponentModel;

public class ConfigFile
{
    public class CMD
    {
        public string 原始命令 = "warp {0}";

        public string 新的命令 = "传送";

        public bool 余段补充 = false;

        public bool 禁用指令 = false;

        public int 死亡条件 = 0;

        public int 冷却秒数 = 0;

        public bool 冷却共用 = false;
    }

    [Description("命令表")]
    public List<CMD> 命令表 = new List<CMD>();

    public static Action<ConfigFile> ConfigR;

    public static ConfigFile Read(string Path)
    {
        if (!File.Exists(Path))
        {
            ConfigFile configFile = new ConfigFile();
            configFile.命令表.Add(new CMD());
            configFile.命令表.Add(new CMD
            {
                原始命令 = "spawn",
                新的命令 = "回城"
            });
            return configFile;
        }
        using FileStream stream = new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Read(stream);
    }

    public static ConfigFile Read(Stream stream)
    {
        using StreamReader streamReader = new StreamReader(stream);
        ConfigFile configFile = JsonConvert.DeserializeObject<ConfigFile>(streamReader.ReadToEnd());
        if (ConfigR != null)
        {
            ConfigR(configFile);
        }
        return configFile;
    }

    public void Write(string Path)
    {
        using FileStream stream = new FileStream(Path, FileMode.Create, FileAccess.Write, FileShare.Write);
        Write(stream);
    }

    public void Write(Stream stream)
    {
        string value = JsonConvert.SerializeObject((object)this, (Formatting)1);
        using StreamWriter streamWriter = new StreamWriter(stream);
        streamWriter.Write(value);
    }
}
