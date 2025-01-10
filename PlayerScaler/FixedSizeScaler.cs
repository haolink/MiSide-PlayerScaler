using AsmResolver.IO;
using System;
using UnityEngine;


public class FixedSizeScaler : BaseScaler
{
    private float playerScale;
    private float lastKnownPlayerScale;
    private float mitaScale;

    private bool collidersEnabled;
    private bool collidersToggled = false; //If true, will force update all colliders in the next Update() cycle

    private DateTime? dataLastUpdate = null;

    private int _afterScaleSaveTimeout = 10;

    private PlayerScaler _mainPlugin;

    public int offCycleUpdate = 0;
    public int offCycleUpdateInterval = 30;

    /// <summary>
    /// Initialise Fixed Scaler.
    /// </summary>
    /// <param name="_plugin">Main Plugin code.</param>
    public FixedSizeScaler(bool includeChibiMita, PlayerScaler _plugin) : base(includeChibiMita)
    { 
        // Initialise variables
        this.playerScale = 1.0f;
        this.mitaScale = 1.0f;

        this._mainPlugin = _plugin;

        this.dataLastUpdate = null;

        this.mitaScale = this._mainPlugin.ConfigData.Scales.MitaScale;
        this.playerScale = this._mainPlugin.ConfigData.Scales.PlayerScale;
        this.lastKnownPlayerScale = this._mainPlugin.ConfigData.Scales.PlayerRestoreScale;

        this.collidersEnabled = true;
        this.collidersToggled = false;

        this._afterScaleSaveTimeout = this._mainPlugin.ConfigData.Configuration.AfterScaleSaveTimeout;
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
    /// Toggles Mita colliders.
    /// </summary>
    public override void ToggleColliders()
    {
        this.collidersEnabled = !this.collidersEnabled;
        this.collidersToggled = true;

        Debug.Log("Collider setting set to: " + (this.collidersEnabled ? "On" : "Off"));
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
        this._mainPlugin.ConfigData.Scales.MitaScale = this.mitaScale;
        this._mainPlugin.ConfigData.Scales.PlayerScale = this.playerScale;
        this._mainPlugin.ConfigData.Scales.PlayerRestoreScale = this.lastKnownPlayerScale;

        this._mainPlugin.ConfigData.Configuration.AfterScaleSaveTimeout = this._afterScaleSaveTimeout;
        this._mainPlugin.ConfigData.Configuration.IncludeChibiMita = this.IncludeChibiMita;


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

        WorldPlayer wp = GameObject.FindFirstObjectByType<WorldPlayer>();
        if (wp != null)
        {
            wp.speed = this.playerScale;
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
            bool requiredAdjustment = this.SetTransformScale(mitaTransform, this.mitaScale);
            if (requiredAdjustment)
            {
                this.ScaleMilaGlasses(mitaTransform, this.mitaScale);
            }

            // Update the colliders if required.
            if (requiredAdjustment || this.collidersToggled)
            {
                if (mitaTransform.gameObject.TryGetComponent<CapsuleCollider>(out CapsuleCollider collider))
                {
                    collider.enabled = this.collidersEnabled;
                }
            }

            // this makes sure Crazy Mita glasses are scaled properly when she puts them on. This check runs only in intervals.
            if (this.offCycleUpdate == 0)             
            {
                this.ScaleMitaAccessories(mitaTransform);
            }
        }

        this.offCycleUpdate++;
        if (this.offCycleUpdate >= this.offCycleUpdateInterval)
        {
            this.offCycleUpdate = 0;
        }
    }
}
