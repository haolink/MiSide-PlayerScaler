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
public static class ColliderRadiusExtension 
{
    internal class ColliderExtensionProperties
    {
        public float? initialRadius;
    }

    private static ConditionalWeakTable<CapsuleCollider, ColliderExtensionProperties> _propertyValues = new ConditionalWeakTable<CapsuleCollider, ColliderExtensionProperties>();

    /// <summary>
    /// Gets the initial radius - or stores it if excuted the first time.
    /// </summary>
    /// <param name="collider"></param>
    /// <returns></returns>
    public static float GetInitialRadius(this CapsuleCollider collider)
    {
        ColliderExtensionProperties cp = _propertyValues.GetOrCreateValue(collider);

        if (cp.initialRadius == null)
        {
            cp.initialRadius = collider.radius;
        }

        return cp.initialRadius.Value;
    }
}

