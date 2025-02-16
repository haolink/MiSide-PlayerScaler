using System;
using System.Runtime.CompilerServices;
using MagicaCloth;
using UnityEngine;

/// <summary>
/// Stores the capsule collider radius of a model to make sure it isn't forgotten.
/// </summary>
public static class MagicaColliderExtensions 
{
    internal class MagicaColliderExtensionProperties
    {
        internal bool initialized = false;
        public float initialRadiusStart;
        public float initialRadiusEnd;

        public bool colliderKnown = true;
    }

    private static UnityObjectDictionary<MagicaCapsuleCollider, MagicaColliderExtensionProperties> MagicaColliderCollection = null;
    private static int ReadCounter = 0;

    /// <summary>
    /// Reads the settings of.
    /// </summary>
    /// <param name="collider"></param>
    /// <returns></returns>
    private static MagicaColliderExtensionProperties ReadExtensionSettings(MagicaCapsuleCollider collider, bool autoCreate = true)
    {
        if (MagicaColliderCollection == null)
        {
            MagicaColliderCollection = new UnityObjectDictionary<MagicaCapsuleCollider, MagicaColliderExtensionProperties> ();
        }
        
        ReadCounter++;
        if (ReadCounter % 1000 == 0)
        {
            MagicaColliderCollection.RemoveDestroyed();
            ReadCounter = 0;
        }


        MagicaColliderExtensionProperties? cp;

        if (autoCreate)
        {
            if (MagicaColliderCollection.TryGetValue(collider, out MagicaColliderExtensionProperties props))
            {
                cp = props;
            }
            else
            {
                cp = new MagicaColliderExtensionProperties();
                MagicaColliderCollection.Add(collider, cp);
            }
        } 
        else
        {
            if (MagicaColliderCollection.TryGetValue(collider, out MagicaColliderExtensionProperties props))
            {
                cp = props;
            } 
            else
            {
                return null;
            }
        }        

        if (!cp.initialized)
        {
            cp.initialized = true;
            cp.initialRadiusStart = collider.StartRadius;
            cp.initialRadiusEnd = collider.EndRadius;
            cp.colliderKnown = true;
        }

        return cp;
    }

    /// <summary>
    /// Gets the initial start radius - or stores it if excuted the first time.
    /// </summary>
    /// <param name="collider"></param>
    /// <returns></returns>
    public static float GetInitialStartRadius(this MagicaCapsuleCollider collider)
    {
        MagicaColliderExtensionProperties cp = MagicaColliderExtensions.ReadExtensionSettings(collider);

        return cp.initialRadiusStart;
    }

    /// <summary>
    /// Gets the initial end radius - or stores it if excuted the first time.
    /// </summary>
    /// <param name="collider"></param>
    /// <returns></returns>
    public static float GetInitialEndRadius(this MagicaCapsuleCollider collider)
    {
        MagicaColliderExtensionProperties cp = MagicaColliderExtensions.ReadExtensionSettings(collider);

        return cp.initialRadiusEnd;
    }

    /// <summary>
    /// Checks if the collider is supposed to be under plugin control.
    /// </summary>
    /// <param name="collider"></param>
    /// <returns>Is data stored here - or can game data be used without looking.</returns>
    public static bool IsColliderFromAMita(this MagicaCapsuleCollider collider)
    {
        MagicaColliderExtensionProperties? cp = MagicaColliderExtensions.ReadExtensionSettings(collider, false);

        return (cp != null && cp.colliderKnown);
    }

    /// <summary>
    /// Sometimes references get lost.
    /// </summary>
    /// <param name="collider"></param>
    /// <returns></returns>
    public static void EnsureColliderIsKnown(this MagicaCapsuleCollider collider)
    {
        MagicaColliderExtensions.ReadExtensionSettings(collider, true);
    }
}

