using System;
using UnityEngine;

public class DynamicScaler : BaseScaler
{
    private float lastKnownPlayerScale;    

    public DynamicScaler() : base()
    {
        ColliderEnabledWatcher.HookNativeUnityCollider(false);
        this.lastKnownPlayerScale = 1.0f;
    }

    private Transform? GetClosestMita()
    {
        Transform? playerTransform = GetPlayerTransform();
        if (playerTransform == null)
        {
            return null;
        }

        MitaPerson[] mitas = GameObject.FindObjectsOfType<MitaPerson>().ToArray();
        if (mitas.Length == 0)
        {
            Debug.LogWarning($"No Mitas found");
            return null;
        }

        float dist = float.MaxValue;
        Transform? foundMita = null;

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
            float distanceToPlayer = (playerTransform.position - mitaTransform.position).magnitude;

            if (distanceToPlayer < dist)
            {
                foundMita = mitaTransform;
                dist = distanceToPlayer;
            }
        }

        return foundMita;
    }

    public override void ResizeMita(float? fixedScale, float? scaleFactor)
    {
        Transform? foundMita = GetClosestMita();

        if (foundMita == null)
        {
            Debug.LogWarning($"Mita transforms not available.");
            return;
        }

        float m = foundMita.localScale.x;

        if (fixedScale != null)
        {
            m = fixedScale.Value;
        }
        else if (scaleFactor != null)
        {
            m *= scaleFactor.Value;
        }

        Debug.Log($"Scale for closest Mita: " + m.ToString());

        if (this.SetTransformScale(foundMita, m, true, PluginConfiguration.ConfigJSON.Configuration.IncludeMitaSpeed))
        {
            this.ScaleMilaGlasses(foundMita, m);
            this.ScaleMitaAccessories(foundMita);
        }
    }

    public override void ToggleColliders()
    {
        if (!this.AllowColliderToggling)
        {
            return;
        }

        Transform? foundMita = GetClosestMita();        

        if (foundMita != null && foundMita.gameObject.TryGetComponent<CapsuleCollider>(out CapsuleCollider collider))
        {
            Debug.Log("Colliders toggled");
            collider.enabled = !collider.enabled;
        } 
        else
        {
            Debug.Log("No collider to toggle");
        }     
    }

    public override void ResizePlayer(float? fixedScale, float? scaleFactor)
    {
        Transform? playerTransform = GetPlayerTransform();
        if (playerTransform == null)
        {
            return;
        }

        float s = lastKnownPlayerScale;

        if (fixedScale != null)
        {
            s = fixedScale.Value;
        }
        else if (scaleFactor != null)
        {
            s = playerTransform.localScale.x;
            s *= scaleFactor.Value;
            s = Mathf.Min(Mathf.Max(0.1f, s), 1.0f);

            this.lastKnownPlayerScale = s;
        }

        Debug.Log($"New player scale: " + s.ToString());
        PlayerSpeedModder.MaxSpeedFactor = s;
        playerTransform.localScale = new Vector3(s, s, s);
    }

    protected override void InternalUpdate()
    {
        // Stub not required.
    }
}
