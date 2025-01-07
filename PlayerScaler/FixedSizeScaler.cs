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

    public FixedSizeScaler(PlayerScaler _plugin) 
    { 
        this.playerScale = 1.0f;
        this.mitaScale = 1.0f;

        this._mainPlugin = _plugin;

        this.dataLastUpdate = null;

        bool restoreScaleOverruled = false;

        if (this._mainPlugin.ConfigData != null)
        {
            if (this._mainPlugin.ConfigData.Scales != null)
            {
                float? mitaBaseScale = this._mainPlugin.ConfigData.Scales.MitaScale;
                float? playerBaseScale = this._mainPlugin.ConfigData.Scales.PlayerScale;
                float? playerRestoreScale = this._mainPlugin.ConfigData.Scales.PlayerRestoreScale;

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

        if (!restoreScaleOverruled)
        {
            this.lastKnownPlayerScale = this.playerScale;
        }        
    }

    private void MarkForSaving()
    {
        if (this.dataLastUpdate == null)
        {
            this.dataLastUpdate = DateTime.Now;
        }
    }

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

    public override void Update()
    {
        if (this.dataLastUpdate != null)
        {
            if ((DateTime.Now - this.dataLastUpdate) >= TimeSpan.FromSeconds(this._afterScaleSaveTimeout))
            {
                this.dataLastUpdate = null;
                this.Save();
            }
        }

        Transform? t = GetPlayerTransform(true);

        if (t != null)
        {
            if (t.localScale.x != this.playerScale)
            {
                t.localScale = new Vector3(this.playerScale, this.playerScale, this.playerScale);
            }
        }


        MitaPerson[] mitas = GameObject.FindObjectsOfType<MitaPerson>().ToArray();
        foreach (MitaPerson mita in mitas)
        {
            Animator subAnimator = mita.gameObject.GetComponentInChildren<Animator>();
            if (subAnimator == null)
            {
                continue;
            }

            if (!subAnimator.gameObject.active)
            {
                continue;
            }

            Transform mitaTransform = subAnimator.gameObject.transform;

            if (mitaTransform != null)
            {
                if (mitaTransform.localScale.x != this.mitaScale)
                {
                    mitaTransform.localScale = new Vector3(this.mitaScale, this.mitaScale, this.mitaScale);
                }
            }
        }
    }
}
