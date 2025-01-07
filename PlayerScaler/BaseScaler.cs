using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public abstract class BaseScaler
{
    public abstract void ResizePlayer(float? fixedScale, float? scaleFactor);
    public abstract void ResizeMita(float? fixedScale, float? scaleFactor);

    public abstract void Update();
    
    /// <summary>
    /// Finds the player Transform.
    /// </summary>
    /// <returns></returns>
    protected Transform? GetPlayerTransform(bool suppressWarnings = false)
    {
        GameObject? playerObject = GameObject.Find("GameController/Player");

        if (playerObject == null)
        {
            if (!suppressWarnings)
            {
                Debug.LogWarning($"Player not found");
            }            
            return null;
        }

        Transform? playerTransform = playerObject.GetComponent<Transform>();

        if (playerTransform == null)
        {
            if (!suppressWarnings)
            {
                Debug.LogWarning($"Player object lacks transform");
            }
            return null;
        }

        return playerTransform;
    }
}
