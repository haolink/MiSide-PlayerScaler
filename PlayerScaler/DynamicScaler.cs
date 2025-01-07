using System;
using UnityEngine;

public class DynamicScaler : BaseScaler
{
    private float lastKnownPlayerScale;    

    public DynamicScaler()
    {
        this.lastKnownPlayerScale = 1.0f;
    }

    public override void ResizeMita(float? fixedScale, float? scaleFactor)
    {
        Transform? playerTransform = GetPlayerTransform();
        if (playerTransform == null)
        {
            return;
        }

        MitaPerson[] mitas = GameObject.FindObjectsOfType<MitaPerson>().ToArray();
        if (mitas.Length == 0)
        {
            Debug.LogWarning($"No Mitas found");
            return;
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

        if (foundMita == null)
        {
            Debug.LogWarning($"Mita transforms not available.");
            return;
        }

        float s = playerTransform.localScale.x;
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
        foundMita.localScale = new Vector3(m, m, m);
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
        playerTransform.localScale = new Vector3(s, s, s);
    }

    public override void Update()
    {
        // Stub not required.
    }
}
