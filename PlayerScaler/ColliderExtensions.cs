using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using Il2CppInterop;
using Il2CppInterop.Runtime.InteropTypes.Fields;
using Il2CppInterop.Runtime;

/// <summary>
/// Stores the capsule collider radius of a model to make sure it isn't forgotten.
/// </summary>
public static class ColliderExtensions
{
    internal class ColliderExtensionProperties
    {
        internal bool initialized = false;
        public float initialRadius;

        public bool colliderDefaultEnabled = true;
        public bool colliderKnown = true;
    }
    
    private static UnityObjectDictionary<Collider, ColliderExtensionProperties> ColliderCollection = null;
    private static int ReadCounter = 0;

    /// <summary>
    /// Reads the settings of.
    /// </summary>
    /// <param name="collider"></param>
    /// <returns></returns>
    private static bool ReadExtensionSettings(Collider collider, bool autoCreate, out ColliderExtensionProperties properties)
    {
        properties = null;
        bool result = false;

        if (ColliderCollection == null)
        {
            Debug.Log("No Collection created!");
            ColliderExtensions.ColliderCollection = new UnityObjectDictionary<Collider, ColliderExtensionProperties>();
        }

        ReadCounter++;
        if (ReadCounter % 1000 == 0) 
        {
            ColliderExtensions.ColliderCollection.RemoveDestroyed();
            ReadCounter = 0;
        }

        ColliderExtensionProperties cp;

        if (autoCreate)
        {
            if (ColliderExtensions.ColliderCollection.TryGetValue(collider, out ColliderExtensionProperties props))
            {
                cp = props;
                result = true;
            }
            else
            {                
                cp = new ColliderExtensionProperties();
                ColliderExtensions.ColliderCollection.Add(collider, cp);
            }
        } 
        else
        {
            if (ColliderExtensions.ColliderCollection.TryGetValue(collider, out ColliderExtensionProperties props))
            {
                cp = props;
                result = true;
            } 
            else
            {
                return false;
            }
        }        

        if (!cp.initialized)
        {
            cp.initialized = true;
            cp.initialRadius = 0.0f;
            cp.colliderKnown = true;
            cp.colliderDefaultEnabled = collider.enabled;
            
            if (collider.GetIl2CppType().IsAssignableFrom(Il2CppType.From(typeof(CapsuleCollider))))
            {
                CapsuleCollider cc = new CapsuleCollider(collider.Pointer);
                cp.initialRadius = cc.radius;
            }
            if (collider.GetIl2CppType().IsAssignableFrom(Il2CppType.From(typeof(SphereCollider))))
            {
                SphereCollider sc = new SphereCollider(collider.Pointer);
                cp.initialRadius = sc.radius;
            }
        }         

        properties = cp;
        return result;
    }

    /// <summary>
    /// Gets the initial radius - or stores it if excuted the first time.
    /// </summary>
    /// <param name="collider"></param>
    /// <returns></returns>
    public static float GetInitialRadius(this Collider collider)
    {
        ColliderExtensionProperties cp = null;
        ColliderExtensions.ReadExtensionSettings(collider, true, out cp);

        return (cp == null ? 0.0f:cp.initialRadius);
    }

    /// <summary>
    /// Checks if the collider is supposed to be under plugin control.
    /// </summary>
    /// <param name="collider"></param>
    /// <returns>Is data stored here - or can game data be used without looking.</returns>
    public static bool IsColliderFromAMita(this Collider collider)
    {
        ColliderExtensionProperties? cp;
        return ColliderExtensions.ReadExtensionSettings(collider, false, out cp);        
    }

    /// <summary>
    /// Sometimes references get lost.
    /// </summary>
    /// <param name="collider"></param>
    /// <returns></returns>
    public static bool EnsureColliderIsKnown(this Collider collider)
    {
        ColliderExtensionProperties? cp;
        return ColliderExtensions.ReadExtensionSettings(collider, true, out cp);
    }

    /// <summary>
    /// Sets the default value for this collider which the game wishes to use.
    /// </summary>
    /// <param name="collider"></param>
    public static void SetGameDefault(this Collider collider, bool value)
    {
        ColliderExtensionProperties? cp = null;
        if (ColliderExtensions.ReadExtensionSettings(collider, true, out cp))
        {
            cp.colliderDefaultEnabled = value;
        }        
    }

    /// <summary>
    /// Gets the default value for this collider which the game wishes to use.
    /// </summary>
    /// <param name="collider"></param>
    public static bool GetGameDefault(this Collider collider)
    {
        ColliderExtensionProperties cp = null;
        ColliderExtensions.ReadExtensionSettings(collider, true, out cp);
        return cp.colliderDefaultEnabled;
    }
}

