using UnityEngine;
using System.Text.Json;
using System.Reflection;
using UnityEditor.SceneManagement;
using HarmonyLib;

public class PlayerScaler : MonoBehaviour
{     
    /// <summary>
    /// Which scaler to use.
    /// </summary>
    private BaseScaler? scalerInternal;

    /// <summary>
    /// Scaling factor.
    /// </summary>
    private float scaleFactor;

    private void Start()
    {
        PluginConfiguration.AddDefaultKeybind("MitaShrink", "Keypad2", KeyCode.Keypad2, () => { this.scalerInternal?.ResizeMita(null, 1.0f / this.scaleFactor) ; }, true, new string[] { "MitaGrow" });
        PluginConfiguration.AddDefaultKeybind("MitaNormal", "Keypad5", KeyCode.Keypad5, () => { this.scalerInternal?.ResizeMita(1.0f, null); });
        PluginConfiguration.AddDefaultKeybind("MitaGrow", "Keypad8", KeyCode.Keypad8, () => { this.scalerInternal?.ResizeMita(null, this.scaleFactor); }, true, new string[] { "MitaShrink" });
        PluginConfiguration.AddDefaultKeybind("MitaCollission", "KeypadDivide", KeyCode.KeypadDivide, () => { this.scalerInternal?.ToggleColliders(); });

        PluginConfiguration.AddDefaultKeybind("PlayerShrink", "Keypad3", KeyCode.Keypad3, () => { this.scalerInternal?.ResizePlayer(null, 1.0f / this.scaleFactor); }, true, new string[] { "PlayerGrow" });
        PluginConfiguration.AddDefaultKeybind("PlayerNormal", "Keypad6", KeyCode.Keypad6, () => { this.scalerInternal?.ResizePlayer(1.0f, null); });
        PluginConfiguration.AddDefaultKeybind("PlayerGrow", "Keypad9", KeyCode.Keypad9, () => { this.scalerInternal?.ResizePlayer(null, this.scaleFactor); }, true, new string[] { "PlayerShrink" });
        PluginConfiguration.AddDefaultKeybind("PlayerRestore", "KeypadPeriod", KeyCode.KeypadPeriod, () => { this.scalerInternal?.ResizePlayer(null, null); });

        // Load configuration
        PluginConfiguration.LoadConfiguration();

        this.scaleFactor = (PluginConfiguration.ConfigJSON.Configuration.ScaleFactor - 1.0f) / 4.0f + 1.0f; //Due to a change of format, this variable is divided by 4.        

        // Depending on mode load the respective scaler.
        if (PluginConfiguration.ConfigJSON.Configuration.FixedHeightMode)
        {
            this.scalerInternal = new FixedSizeScaler();
        }
        else
        {
            this.scalerInternal = new DynamicScaler();
        }

        PluginConfiguration.SaveConfiguration();
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
        foreach (KeyValuePair<string, PluginConfiguration.KeyDownType> kvp in PluginConfiguration.Keybinds)
        {
            PluginConfiguration.KeyDownType keydownConfiguration = kvp.Value;

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
                        if (!PluginConfiguration.Keybinds.ContainsKey(conflictingKey)) 
                        {
                            continue;
                        }

                        if (Input.GetKey(PluginConfiguration.Keybinds[conflictingKey].KeyCode)) {
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
}
