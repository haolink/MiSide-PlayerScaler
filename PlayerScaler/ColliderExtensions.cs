using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Stores the capsule collider radius of a model to make sure it isn't forgotten.
/// </summary>
public static class ColliderExtensions 
{
    internal class ColliderExtensionProperties
    {
        internal bool initialized = false;
        public float initialRadius;

        public bool colliderKnown = true;
    }

    private static ConditionalWeakTable<CapsuleCollider, ColliderExtensionProperties> _propertyValues = new ConditionalWeakTable<CapsuleCollider, ColliderExtensionProperties>();

    /// <summary>
    /// Reads the settings of.
    /// </summary>
    /// <param name="collider"></param>
    /// <returns></returns>
    private static ColliderExtensionProperties ReadExtensionSettings(CapsuleCollider collider, bool autoCreate = true)
    {
        ColliderExtensionProperties? cp;

        if (autoCreate)
        {
            cp = _propertyValues.GetOrCreateValue(collider);
        } 
        else
        {
            if (_propertyValues.TryGetValue(collider, out ColliderExtensionProperties props))
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
            cp.initialRadius = collider.radius;
            cp.colliderKnown = true;
        }

        return cp;
    }

    /// <summary>
    /// Gets the initial radius - or stores it if excuted the first time.
    /// </summary>
    /// <param name="collider"></param>
    /// <returns></returns>
    public static float GetInitialRadius(this CapsuleCollider collider)
    {
        ColliderExtensionProperties cp = ColliderExtensions.ReadExtensionSettings(collider);

        return cp.initialRadius;
    }

    /// <summary>
    /// Checks if the collider is supposed to be under plugin control.
    /// </summary>
    /// <param name="collider"></param>
    /// <returns>Is data stored here - or can game data be used without looking.</returns>
    public static bool IsColliderFromAMita(this CapsuleCollider collider)
    {
        ColliderExtensionProperties? cp = ColliderExtensions.ReadExtensionSettings(collider, false);

        return (cp != null && cp.colliderKnown);
    }

    /// <summary>
    /// Sometimes references get lost.
    /// </summary>
    /// <param name="collider"></param>
    /// <returns></returns>
    public static void EnsureColliderIsKnown(this CapsuleCollider collider)
    {
        ColliderExtensions.ReadExtensionSettings(collider, true);
    }
}

