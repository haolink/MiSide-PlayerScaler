using System;
using System.Text.Json;
using UnityEngine;
using System.Reflection;

public static class PluginConfiguration
{
    public class KeyDownType
    {
        /// <summary>
        /// If true, then this button will be checked with the GetKey event which will verify if a key is pressed, otherwise each keypress will trigger the event only once.
        /// </summary>
        public bool Continuous { get; init; } = false;

        private string[]? _conflictingdKeyPresses;
        /// <summary>
        /// Which buttons cause a conflict with this action (Continuous must be true).
        /// </summary>
        public string[] ConflictingdKeypresses
        {
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
    private static Dictionary<string, KeyDownType> _keybinds = new Dictionary<string, KeyDownType>();

    /// <summary>
    /// Stored keybinds to capture.
    /// </summary>
    public static Dictionary<string, KeyDownType> Keybinds { get { return PluginConfiguration._keybinds; } }

    /// <summary>
    /// Configuration file structure.
    /// </summary>
    public class ConfigurationFile
    {
        public class ConfigurationType
        {
            public bool FixedHeightMode { get; set; } = false;
            public bool IncludeChibiMita { get; set; } = false;
            public bool IncludeCoreMita { get; set; } = false;

            private float _scaleFactor;

            public float ScaleFactor
            {
                get => _scaleFactor;
                set => _scaleFactor = Mathf.Clamp(value, 1.001f, 2.0f);
            }

            private int _afterScaleTimeout = 10;
            public int AfterScaleSaveTimeout
            {
                get => _afterScaleTimeout;
                set => _afterScaleTimeout = Math.Clamp(value, 2, 60);
            }

            public bool AllowColliderToggle { get; set; } = false;
        }

        public class ScalesType
        {
            public float _mitaScale = 1.0f;
            public float MitaScale
            {
                get => _mitaScale;
                set => _mitaScale = Mathf.Max(value, 0.1f);
            }

            private float _playerScale;
            public float PlayerScale
            {
                get => _playerScale;
                set => _playerScale = Mathf.Clamp(value, 0.1f, 1.0f);
            }

            private float _playerRestoreScale = 1.0f;
            public float PlayerRestoreScale
            {
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

    public static ConfigurationFile ConfigJSON { get; set; } = new ConfigurationFile();

    /// <summary>
    /// Serialises to Settings.json
    /// </summary>
    public static void SaveConfiguration()
    {
        if (ConfigJSON == null)
        {
            ConfigJSON = new ConfigurationFile();
        }

        string baseDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string path = Path.Combine(baseDirectory ?? "", "settings.json");

        JsonSerializerOptions options = new JsonSerializerOptions();
        options.WriteIndented = true;

        //PluginConfiguration.ConfigJSON.Configuration.FixedHeightMode = (this.scalerInternal is FixedSizeScaler);

        ConfigJSON.Keybinds.Clear();
        foreach (KeyValuePair<string, KeyDownType> entry in _keybinds)
        {
            string key = entry.Key;
            string value = Enum.GetName(typeof(KeyCode), entry.Value.KeyCode);
            if (value != null)
            {
                ConfigJSON.Keybinds.Add(key, value);
            }
        }

        string jsonOut = JsonSerializer.Serialize<ConfigurationFile>(ConfigJSON, options);

        File.WriteAllText(path, jsonOut);
    }

    /// <summary>
    /// Loads setting.json.
    /// </summary>
    public static void LoadConfiguration()
    {
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
                    ConfigJSON = rawSettings;

                    Debug.Log("Fixed Height mode has been set to: " + (ConfigJSON.Configuration.FixedHeightMode ? "True" : "False"));

                    foreach (KeyValuePair<string, string> entry in rawSettings.Keybinds)
                    {
                        if (Enum.TryParse(entry.Value, out KeyCode key))
                        {
                            string actionName = entry.Key;

                            if (_keybinds.ContainsKey(actionName))
                            {
                                _keybinds[actionName].KeyCode = key;
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

    public static void AddDefaultKeybind(string name, string description, KeyCode keyCode, Action callback, bool continuous = false, string[]? conflictingKeyPressed = null)
    {
        if (!_keybinds.ContainsKey(name))
        {
            _keybinds[name] = new KeyDownType()
            {
                KeyCode = keyCode,
                Continuous = continuous,
                ActionCallback = callback,
                ConflictingdKeypresses = continuous ? (conflictingKeyPressed ?? new string[0]) : new string[0]
            };
            Debug.Log("Default keybind set: " + name + " --> " + description);
        }
    }
}
