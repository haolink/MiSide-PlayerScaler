using AsmResolver.IO;
using EPOOutline;
using Il2CppInterop.Runtime;
using MagicaCloth;
using System;
using UnityEngine;
using VertexFragment;


public class FixedSizeScaler : BaseScaler
{
    private float playerScale;
    private float lastKnownPlayerScale;
    private float mitaScale;

    public static bool CollidersEnabled;
    private bool collidersToggled = false; //If true, will force update all colliders in the next Update() cycle

    private DateTime? dataLastUpdate = null;

    private int _afterScaleSaveTimeout = 10;

    public int offCycleUpdate = 0;
    public int offCycleUpdateInterval = 30;

    /// <summary>
    /// Initialise Fixed Scaler.
    /// </summary>
    /// <param name="_plugin">Main Plugin code.</param>
    public FixedSizeScaler() : base()
    {
        // Hook collider watch
        ColliderEnabledWatcher.HookNativeUnityCollider();

        // Initialise variables
        this.playerScale = 1.0f;
        this.mitaScale = 1.0f;

        this.dataLastUpdate = null;

        this.mitaScale = PluginConfiguration.ConfigJSON.Scales.MitaScale;
        this.playerScale = PluginConfiguration.ConfigJSON.Scales.PlayerScale;
        this.lastKnownPlayerScale = PluginConfiguration.ConfigJSON.Scales.PlayerRestoreScale;

        FixedSizeScaler.CollidersEnabled = true;
        this.collidersToggled = false;

        this._afterScaleSaveTimeout = PluginConfiguration.ConfigJSON.Configuration.AfterScaleSaveTimeout;
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
        if ( this.AllowColliderToggling )
        {
            CollidersEnabled = !CollidersEnabled;
            this.collidersToggled = true;

            Debug.Log("Collider setting set to: " + (CollidersEnabled ? "On" : "Off"));
        }        
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
        PlayerSpeedModder.MaxSpeedFactor = this.playerScale;
        Debug.Log($"Player scale fixed at: {this.playerScale}");
    }

    /// <summary>
    /// Saving settings.
    /// </summary>
    public void Save()
    {
        PluginConfiguration.ConfigJSON.Scales.MitaScale = this.mitaScale;
        PluginConfiguration.ConfigJSON.Scales.PlayerScale = this.playerScale;
        PluginConfiguration.ConfigJSON.Scales.PlayerRestoreScale = this.lastKnownPlayerScale;
       
        PluginConfiguration.SaveConfiguration();
    }

    /// <summary>
    /// Updates scale in real time.
    /// </summary>
    protected override void InternalUpdate()
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

        Location20_Arena go = GameObject.FindAnyObjectByType<Location20_Arena>();
        if (go != null)
        {
            go.speedMovePlayer = 30.0f * this.playerScale;
        }

        // Does a player exist?
        Transform? t = GetPlayerTransform(true);
        //Collider[] playerColliders = null;

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

            // playerColliders = this.GetPlayerColliders(t);
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
            bool requiredAdjustment = this.SetTransformScale(mitaTransform, this.mitaScale, true, PluginConfiguration.ConfigJSON.Configuration.IncludeMitaSpeed);

            List<Collider> colliders = new List<Collider>(this.QuerySubColliders<Collider>(mitaTransform));
            /*BoxCollider dialogCollider = this.FetchDialogCollider();

            if (dialogCollider != null)
            {
                Debug.Log("Dialog collider has been found!");
                colliders.Add(dialogCollider);
            }*/

            // Update the colliders if required.
            foreach (Collider c in colliders)
            {
                /*if (!( (c.GetIl2CppType().IsAssignableFrom(Il2CppType.From(typeof(CapsuleCollider)))) ||
                       (c.GetIl2CppType().IsAssignableFrom(Il2CppType.From(typeof(SphereCollider))))) )
                {
                    continue;
                }*/

                bool enforceActivation = !c.EnsureColliderIsKnown();
                if (this.AllowColliderToggling && (requiredAdjustment || this.collidersToggled || enforceActivation))
                {
                    c.enabled = c.enabled; // just execute the setter
                }
            }            
            
            if (requiredAdjustment)
            {
                this.ScaleMilaGlasses(mitaTransform, this.mitaScale);
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

        this.collidersToggled = false;
    }
}
