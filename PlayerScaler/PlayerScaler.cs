using UnityEngine;
using BepInEx;
using System.Text.Json;
using System.Reflection;
using Assimp.Configs;
using BepInEx.Configuration;
using System.Runtime.CompilerServices;

public class PlayerScaler : MonoBehaviour
{
    private Dictionary<string, KeyCode> keybinds;

    private float scaleFactor = 1.02f;

    private bool fixedHeightMode;

    private BaseScaler? scalerInternal;

    private void Start()
    {
        this.keybinds = new Dictionary<string, KeyCode>();
        this.fixedHeightMode = false;

        LoadConfiguration();

        if (this.fixedHeightMode)
        {
            this.scalerInternal = new FixedSizeScaler(this);
        } 
        else
        {
            this.scalerInternal = new DynamicScaler();
        }

        SaveConfiguration();
    }

    void Update()
    {
        if (this.scalerInternal == null)
        {
            return;
        }

        this.scalerInternal.Update();

        foreach (var keybind in keybinds)
        {
            if (UnityEngine.Input.GetKeyDown(keybind.Value))
            {
                switch (keybind.Key)
                {
                    case "PlayerShrink":
                        this.scalerInternal.ResizePlayer(null, 1.0f / this.scaleFactor);
                        break;
                    case "PlayerGrow":
                        this.scalerInternal.ResizePlayer(null, this.scaleFactor);
                        break;
                    case "PlayerNormal":
                        this.scalerInternal.ResizePlayer(1.0f, null);
                        break;
                    case "PlayerRestore":
                        this.scalerInternal.ResizePlayer(null, null);
                        break;
                    case "MitaShrink":
                        this.scalerInternal.ResizeMita(null, 1.0f / this.scaleFactor);
                        break;
                    case "MitaGrow":
                        this.scalerInternal.ResizeMita(null, this.scaleFactor);
                        break;
                    case "MitaNormal":
                        this.scalerInternal.ResizeMita(1.0f, null);
                        break;
                    default:
                        Debug.Log(keybind.Key);
                        break;

                }
            }
        }
    }    
  
    void OnApplicationQuit()
    {
        
    }

    #region Settings Parser - probably should be in another class file - but meh
    public class ConfigurationFile
    {
        public class ConfigurationType
        {
            public bool? FixedHeightMode {  get; set; }
            public float? ScaleFactor { get; set; }
            public int? AfterScaleSaveTimeout { get; set; }
        }

        public class ScalesType
        {
            public float? MitaScale { get; set; }
            public float? PlayerScale { get; set; }
            public float? PlayerRestoreScale { get; set; }
        }

        public ConfigurationType? Configuration { get; set; }
        public Dictionary<string, string>? Keybinds { get; set; }
        public ScalesType? Scales { get; set; }
    }

    public ConfigurationFile? ConfigData { get; set; }

    public void SaveConfiguration()
    {
        if (this.ConfigData == null)
        {
            this.ConfigData = new ConfigurationFile();
        }

        string baseDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string path = Path.Combine(baseDirectory ?? "", "settings.json");

        JsonSerializerOptions options = new JsonSerializerOptions();
        options.WriteIndented = true;
        
        if (this.ConfigData.Configuration == null)
        {
            this.ConfigData.Configuration = new ConfigurationFile.ConfigurationType();
        }
        ConfigData.Configuration.FixedHeightMode = (this.scalerInternal is FixedSizeScaler);
        ConfigData.Configuration.ScaleFactor = this.scaleFactor;

        if (this.ConfigData.Keybinds == null)
        {
            this.ConfigData.Keybinds = new Dictionary<string, string>();
        }

        foreach (KeyValuePair<string, KeyCode> entry in this.keybinds)
        {
            string key = entry.Key;
            string value = Enum.GetName(typeof(KeyCode), entry.Value);
            if (value != null)
            {
                this.ConfigData.Keybinds[key] = value;
            }            
        }

        string jsonOut = JsonSerializer.Serialize<ConfigurationFile>(this.ConfigData, options);

        File.WriteAllText(path, jsonOut);
    }

    private void LoadConfiguration()
    {
        AssignDefault("MitaShrink", "Keypad2", KeyCode.Keypad2);
        AssignDefault("MitaNormal", "Keypad5", KeyCode.Keypad5);
        AssignDefault("MitaGrow", "Keypad8", KeyCode.Keypad8);

        AssignDefault("PlayerShrink", "Keypad3", KeyCode.Keypad3);
        AssignDefault("PlayerNormal", "Keypad6", KeyCode.Keypad6);
        AssignDefault("PlayerGrow", "Keypad9", KeyCode.Keypad9);
        AssignDefault("PlayerRestore", "KeypadPeriod", KeyCode.KeypadPeriod);

        string baseDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string path = Path.Combine(baseDirectory ?? "", "settings.json");

        if (File.Exists(path))
        {
            try
            {
                string json = File.ReadAllText(path);

                ConfigurationFile rawSettings = JsonSerializer.Deserialize<ConfigurationFile>(json);

                if (rawSettings != null)
                {
                    this.ConfigData = rawSettings;

                    if (rawSettings.Configuration != null)
                    {
                        if (rawSettings.Configuration.FixedHeightMode != null)
                        {                            
                            this.fixedHeightMode = rawSettings.Configuration.FixedHeightMode.Value;
                            Debug.Log("Fixed Height mode has been set to: " + (this.fixedHeightMode ? "True" : "False"));
                        }   
                        if (rawSettings.Configuration.ScaleFactor != null)
                        {
                            this.scaleFactor = Mathf.Clamp(rawSettings.Configuration.ScaleFactor.Value, 1.001f, 2.0f);
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"No configuration key found in settings.json");
                    }

                    if (rawSettings.Keybinds != null)
                    {
                        foreach (KeyValuePair<string, string> entry in rawSettings.Keybinds)
                        {
                            if (Enum.TryParse(entry.Value, out KeyCode key))
                            {
                                string actionName = entry.Key;

                                if (this.keybinds.ContainsKey(actionName))
                                {
                                    this.keybinds[actionName] = key;
                                    Debug.Log($"Reassigned action {actionName} --> {entry.Value}");
                                }
                                else
                                {
                                    Debug.LogWarning($"Unknown action: {actionName}");
                                }
                            }
                            else
                            {
                                Debug.LogWarning($"Invalid keybind: {entry.Key} -> {entry.Value}");
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"No keybind settings found in settings.json");
                    }
                }                        
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Unable to read keybinds file: " + ex.Message);
            }            
        }
    }

    private void AssignDefault(string name, string description, KeyCode keyCode)
    {
        if (!this.keybinds.ContainsKey(name))
        {
            this.keybinds[name] = keyCode;
            Debug.Log("Default keybind set: " + name + " --> " + description);
        }
    }
    #endregion

}
