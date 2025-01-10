using UnityEngine;
using System.Text.Json;
using System.Reflection;
using Unity.Collections.LowLevel.Unsafe;
using System.Text.Json.Serialization;

public class PlayerScaler : MonoBehaviour
{
    internal class KeyDownType
    {        
        /// <summary>
        /// If true, then this button will be checked with the GetKey event which will verify if a key is pressed, otherwise each keypress will trigger the event only once.
        /// </summary>
        public bool Continuous { get; init; } = false;

        private string[]? _conflictingdKeyPresses;
        /// <summary>
        /// Which buttons cause a conflict with this action (Continuous must be true).
        /// </summary>
        public string[] ConflictingdKeypresses {
            get => _conflictingdKeyPresses ??= new string[0];
            init => _conflictingdKeyPresses = value ?? new string[0];
        }

        /// <summary>
        /// What's the respective key code to press.
        /// </summary>
        public KeyCode KeyCode { get; set; } = KeyCode.None;

        /// <summary>
        /// Callback to execute in case of a match.
        /// </summary>
        public Action ActionCallback { get; init; } = null!;
    }

    /// <summary>
    /// Stored keybinds to capture.
    /// </summary>
    private Dictionary<string, KeyDownType> keybinds = new Dictionary<string, KeyDownType>();

    /// <summary>
    /// Default scale factor.
    /// </summary>
    private float scaleFactor = 1.02f;

    /// <summary>
    /// Is Fixed height mode enabled?
    /// </summary>
    private bool fixedHeightMode;

    /// <summary>
    /// Which scaler to use.
    /// </summary>
    private BaseScaler? scalerInternal;

    private void Start()
    {        
        this.keybinds = new Dictionary<string, KeyDownType>();
        this.fixedHeightMode = false;

        // Load configuration
        LoadConfiguration();

        this.scaleFactor = (this.ConfigData.Configuration.ScaleFactor - 1.0f) / 4.0f + 1.0f; //Due to a change of format, this variable is divided by 4.
        bool includeChibiMita = this.ConfigData.Configuration.IncludeChibiMita;

        // Depending on mode load the respective scaler.
        if (this.fixedHeightMode)
        {
            this.scalerInternal = new FixedSizeScaler(includeChibiMita, this);
        } 
        else
        {
            this.scalerInternal = new DynamicScaler(includeChibiMita);
        }

        SaveConfiguration();
    }

    /// <summary>
    /// Hook to Unity's Update method.
    /// </summary>
    void Update()
    {
        if (this.scalerInternal == null)
        {
            return;
        }

        // Run the scalers Update() method.
        this.scalerInternal.Update();

        // Check if a button was pressed
        foreach (KeyValuePair<string, KeyDownType> kvp in keybinds)
        {
            KeyDownType keydownConfiguration = kvp.Value;

            // Is this a continuous button press
            if (keydownConfiguration.Continuous)
            {
                // Then we use get key
                if (Input.GetKey(keydownConfiguration.KeyCode))
                {
                    // And verify that no conflicting action is pressed (Shrink and Grow conflict each other)
                    bool otherButtonsPressed = false;
                    
                    foreach (string conflictingKey in keydownConfiguration.ConflictingdKeypresses)
                    {
                        if (!keybinds.ContainsKey(conflictingKey)) 
                        {
                            continue;
                        }

                        if (Input.GetKey(keybinds[conflictingKey].KeyCode)) {
                            otherButtonsPressed = true;
                            break;
                        }
                    }

                    // THere was a conflict - abort.
                    if (otherButtonsPressed)
                    {
                        continue;
                    }

                    // Otherwise - let's go!
                    keydownConfiguration.ActionCallback();                    
                }                
            }
            else
            {
                // If it's not continuous - just check if the button was pressed.
                if (Input.GetKeyDown(keydownConfiguration.KeyCode))
                {
                    keydownConfiguration.ActionCallback();
                }                    
            }
        }
    }    
  
    void OnApplicationQuit()
    {
        
    }

    #region Settings Parser - probably should be in another class file - but meh
    /// <summary>
    /// Configuration file structure.
    /// </summary>
    public class ConfigurationFile
    {
        public class ConfigurationType
        {
            public bool FixedHeightMode { get; set; } = false;
            public bool IncludeChibiMita { get; set; } = false;

            private float _scaleFactor;

            public float ScaleFactor { 
                get => _scaleFactor;
                set => _scaleFactor = Mathf.Clamp(value, 1.001f, 2.0f);                
            }            

            private int _afterScaleTimeout = 10;
            public int AfterScaleSaveTimeout {
                get => _afterScaleTimeout;
                set => _afterScaleTimeout = Math.Clamp(value, 2, 60);
            }
        }

        public class ScalesType
        {
            public float _mitaScale = 1.0f;
            public float MitaScale {
                get => _mitaScale;
                set => _mitaScale = Mathf.Max(value, 0.1f);
            }

            private float _playerScale;
            public float PlayerScale {
                get => _playerScale;
                set => _playerScale = Mathf.Clamp(value, 0.1f, 1.0f);
            }

            private float _playerRestoreScale = 1.0f;
            public float PlayerRestoreScale {
                get => _playerRestoreScale;
                set => _playerRestoreScale = Mathf.Clamp(value, 0.1f, 1.0f);
            }
        }

        // Private internal storage variables. Would be auto-set if empty on getter access.
        private ConfigurationType? _configuration;
        private Dictionary<string, string>? _keybinds;
        private ScalesType? _scales;


        public ConfigurationType Configuration
        {
            get => _configuration ??= new ConfigurationType();
            init => _configuration = value ?? new ConfigurationType();
        }
        public Dictionary<string, string> Keybinds
        {
            get => _keybinds ??= new Dictionary<string, string>();
            init => _keybinds = value ?? new Dictionary<string, string>();
        }
        public ScalesType Scales 
        {
            get => _scales ??= new ScalesType();
            init => _scales = value ?? new ScalesType();
        }
    }

    public ConfigurationFile ConfigData { get; set; } = new ConfigurationFile();

    /// <summary>
    /// Serialises to Settings.json
    /// </summary>
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
        
        ConfigData.Configuration.FixedHeightMode = (this.scalerInternal is FixedSizeScaler);
        
        this.ConfigData.Keybinds.Clear();
        foreach (KeyValuePair<string, KeyDownType> entry in this.keybinds)
        {
            string key = entry.Key;
            string value = Enum.GetName(typeof(KeyCode), entry.Value.KeyCode);
            if (value != null)
            {
                this.ConfigData.Keybinds.Add(key, value);
            }            
        }

        string jsonOut = JsonSerializer.Serialize<ConfigurationFile>(this.ConfigData, options);

        File.WriteAllText(path, jsonOut);
    }

    /// <summary>
    /// Loads setting.json.
    /// </summary>
    private void LoadConfiguration()
    {
        AssignDefault("MitaShrink", "Keypad2", KeyCode.Keypad2, () => { this.scalerInternal?.ResizeMita(null, 1.0f / this.scaleFactor); }, true, new string[] { "MitaGrow" });
        AssignDefault("MitaNormal", "Keypad5", KeyCode.Keypad5, () => { this.scalerInternal?.ResizeMita(1.0f, null); });
        AssignDefault("MitaGrow", "Keypad8", KeyCode.Keypad8, () => { this.scalerInternal?.ResizeMita(null, this.scaleFactor); }, true, new string[] { "MitaShrink" });
        AssignDefault("MitaCollission", "KeypadDivide", KeyCode.KeypadDivide, () => { this.scalerInternal?.ToggleColliders(); });

        AssignDefault("PlayerShrink", "Keypad3", KeyCode.Keypad3, () => { this.scalerInternal?.ResizePlayer(null, 1.0f / this.scaleFactor); }, true, new string[] { "PlayerGrow" });
        AssignDefault("PlayerNormal", "Keypad6", KeyCode.Keypad6, () => { this.scalerInternal?.ResizePlayer(1.0f, null); });
        AssignDefault("PlayerGrow", "Keypad9", KeyCode.Keypad9, () => { this.scalerInternal?.ResizePlayer(null, this.scaleFactor); }, true, new string[] { "PlayerShrink" });
        AssignDefault("PlayerRestore", "KeypadPeriod", KeyCode.KeypadPeriod, () => { this.scalerInternal?.ResizePlayer(null, null); });

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

                    this.fixedHeightMode = rawSettings.Configuration.FixedHeightMode;
                    Debug.Log("Fixed Height mode has been set to: " + (this.fixedHeightMode ? "True" : "False"));
                    
                    foreach (KeyValuePair<string, string> entry in rawSettings.Keybinds)
                    {
                        if (Enum.TryParse(entry.Value, out KeyCode key))
                        {
                            string actionName = entry.Key;

                            if (this.keybinds.ContainsKey(actionName))
                            {
                                this.keybinds[actionName].KeyCode = key;
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
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to read settings.json: {e.Message}");
            }
        }
    }

    private void AssignDefault(string name, string description, KeyCode keyCode, Action callback, bool continuous = false, string[]? conflictingKeyPressed = null)
    {
        if (!this.keybinds.ContainsKey(name))
        {
            this.keybinds[name] = new KeyDownType()
            {
                KeyCode = keyCode,
                Continuous = continuous,
                ActionCallback = callback,
                ConflictingdKeypresses = continuous ? (conflictingKeyPressed ??  new string[0]) :new string[0]
            };
            Debug.Log("Default keybind set: " + name + " --> " + description);
        }
    }
    #endregion

}
