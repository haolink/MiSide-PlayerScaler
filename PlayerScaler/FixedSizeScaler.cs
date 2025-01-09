using AsmResolver.IO;
using System;
using UnityEngine;


public class FixedSizeScaler : BaseScaler
{
    private float playerScale;
    private float lastKnownPlayerScale;
    private float mitaScale;

    private DateTime? dataLastUpdate = null;

    private int _afterScaleSaveTimeout = 10;

    private PlayerScaler _mainPlugin;

    /// <summary>
    /// Initialise Fixed Scaler.
    /// </summary>
    /// <param name="_plugin">Main Plugin code.</param>
    public FixedSizeScaler(PlayerScaler _plugin) 
    { 
        // Initialise variables
        this.playerScale = 1.0f;
        this.mitaScale = 1.0f;

        this._mainPlugin = _plugin;

        this.dataLastUpdate = null;

        // Read configuration.
        bool restoreScaleOverruled = false;

        if (this._mainPlugin.ConfigData != null)
        {
            if (this._mainPlugin.ConfigData.Scales != null)
            {
                float? mitaBaseScale = this._mainPlugin.ConfigData.Scales.MitaScale;
                float? playerBaseScale = this._mainPlugin.ConfigData.Scales.PlayerScale;
                float? playerRestoreScale = this._mainPlugin.ConfigData.Scales.PlayerRestoreScale;

                // Overrule base variables if configuration contains data.
                if (mitaBaseScale != null)
                {
                    this.mitaScale = mitaBaseScale.Value;
                }
                if (playerBaseScale != null)
                {
                    this.playerScale = Mathf.Clamp(playerBaseScale.Value, 0.1f, 1.0f);
                }
                if (playerRestoreScale != null)
                {
                    this.lastKnownPlayerScale = Mathf.Clamp(playerRestoreScale.Value, 0.1f, 1.0f);
                    restoreScaleOverruled = true;
                }
            }
            
            if (this._mainPlugin.ConfigData.Configuration != null && this._mainPlugin.ConfigData.Configuration.AfterScaleSaveTimeout != null) 
            {
                this._afterScaleSaveTimeout = this._mainPlugin.ConfigData.Configuration.AfterScaleSaveTimeout.Value;
                this._afterScaleSaveTimeout = Math.Clamp(this._afterScaleSaveTimeout, 2, 60);
            }
        }

        // Only set this if it hasn't been read via the configuration file.
        if (!restoreScaleOverruled)
        {
            this.lastKnownPlayerScale = this.playerScale;
        }        
    }

    /// <summary>
    /// Save at the next possible moment.
    /// </summary>
    private void MarkForSaving()
    {
        if (this.dataLastUpdate == null)
        {
            this.dataLastUpdate = DateTime.Now;
        }
    }

    /// <summary>
    /// In Fixed mode we only change the internal Mita scale variable, actual scaling happens in Update().
    /// </summary>
    /// <param name="fixedScale"></param>
    /// <param name="scaleFactor"></param>
    public override void ResizeMita(float? fixedScale, float? scaleFactor)
    {
        if (fixedScale != null)
        {
            this.mitaScale = fixedScale.Value;
            this.MarkForSaving();
        } 
        else if (scaleFactor != null)
        {
            this.mitaScale *= scaleFactor.Value;
            this.MarkForSaving();
        }

        Debug.Log($"Mita scale fixed at: {this.mitaScale}");
    }

    /// <summary>
    /// In Fixed mode we only change the internal player scale variables, actual scaling happens in Update().
    /// </summary>
    /// <param name="fixedScale"></param>
    /// <param name="scaleFactor"></param>
    public override void ResizePlayer(float? fixedScale, float? scaleFactor)
    {
        float p = this.playerScale;
        if (fixedScale != null)
        {
            p = fixedScale.Value;
            this.MarkForSaving();
        }
        else if (scaleFactor != null)
        {
            p *= scaleFactor.Value;
            p = MathF.Min(Math.Max(0.1f, p), 1.0f);
            this.lastKnownPlayerScale = p;            
            this.MarkForSaving();
        }
        else
        {
            p = this.lastKnownPlayerScale;
            this.MarkForSaving();
        }

        this.playerScale = p;
        Debug.Log($"Player scale fixed at: {this.playerScale}");
    }

    /// <summary>
    /// Saving settings.
    /// </summary>
    public void Save()
    {
        if (this._mainPlugin.ConfigData == null)
        {
            this._mainPlugin.ConfigData = new PlayerScaler.ConfigurationFile();
        }
        if (this._mainPlugin.ConfigData.Scales == null)
        {
            this._mainPlugin.ConfigData.Scales = new PlayerScaler.ConfigurationFile.ScalesType();
        }

        this._mainPlugin.ConfigData.Scales.MitaScale = this.mitaScale;
        this._mainPlugin.ConfigData.Scales.PlayerScale = this.playerScale;
        this._mainPlugin.ConfigData.Scales.PlayerRestoreScale = this.lastKnownPlayerScale;

        if (this._mainPlugin.ConfigData.Configuration == null)
        {
            this._mainPlugin.ConfigData.Configuration = new PlayerScaler.ConfigurationFile.ConfigurationType();
        }
        this._mainPlugin.ConfigData.Configuration.AfterScaleSaveTimeout = this._afterScaleSaveTimeout;


        this._mainPlugin.SaveConfiguration();
    }

    /// <summary>
    /// Updates scale in real time.
    /// </summary>
    public override void Update()
    {
        // Consider saving
        if (this.dataLastUpdate != null)
        {
            if ((DateTime.Now - this.dataLastUpdate) >= TimeSpan.FromSeconds(this._afterScaleSaveTimeout))
            {
                this.dataLastUpdate = null;
                this.Save();
            }
        }

        // Does a player exist?
        Transform? t = GetPlayerTransform(true);

        // No? Well, dang.
        if (t != null)
        {
            if (t.localScale.x != this.playerScale)
            {
                t.localScale = new Vector3(this.playerScale, this.playerScale, this.playerScale);
            }

            // Are there extra player armatures (door event in Mila's sub quest).
            Transform[] doorQuestPlayers = this.FindPlayerDoorGlitchTransforms();
            foreach (Transform dq in doorQuestPlayers)
            {
                if (dq.localScale.x != this.playerScale)
                {
                    dq.localScale = new Vector3(this.playerScale, this.playerScale, this.playerScale);
                }
            }
        }

        Location14_PlayerQuest[] boredPlayerAtHome = GameObject.FindObjectsOfType<Location14_PlayerQuest>().ToArray();
        foreach (Location14_PlayerQuest pq in boredPlayerAtHome)
        {
            if (pq.gameObject.transform.localScale.x != this.playerScale)
            {
                pq.gameObject.transform.localScale = new Vector3(this.playerScale, this.playerScale, this.playerScale);
            }            
        }

        // Search for all mitas.
        Transform[] mitaTransforms = this.FindMitas();        
        foreach (Transform mitaTransform in mitaTransforms)
        {        
            // Scale them accordingly.
            if (mitaTransform.localScale.x != this.mitaScale)
            {
                mitaTransform.localScale = new Vector3(this.mitaScale, this.mitaScale, this.mitaScale);

                this.ScaleMilaGlasses(mitaTransform, this.mitaScale);
            }            
        }
    }
}
