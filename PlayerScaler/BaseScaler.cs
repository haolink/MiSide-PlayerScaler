using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public abstract class BaseScaler
{
    /// <summary>
    /// Resized the player (not including clones).
    /// </summary>
    /// <param name="fixedScale">Set the player scale to a fixed value.</param>
    /// <param name="scaleFactor">Or rescale it by a factor set here.</param>
    public abstract void ResizePlayer(float? fixedScale, float? scaleFactor);
    
    /// <summary>
    /// Resizes Mita (or all Mitas).
    /// </summary>
    /// <param name="fixedScale">Sets her to a fixed value.</param>
    /// <param name="scaleFactor">Or alternative resizes her by a factor.</param>
    public abstract void ResizeMita(float? fixedScale, float? scaleFactor);

    /// <summary>
    /// Hook for Unity's Update method.
    /// </summary>
    public abstract void Update();

    /// <summary>
    /// Enable or disable colliders.
    /// </summary>
    public abstract void ToggleColliders();

    /// <summary>
    /// Should Chibi Mita be put into consideration for scaling?
    /// </summary>
    protected bool IncludeChibiMita { get; set; }

    /// <summary>
    /// Should Core Mita be put into consideration for scaling?
    /// </summary>
    protected bool IncludeCoreMita { get; set; }

    /// <summary>
    /// Should the change of colliders be allowed?
    /// </summary>
    protected bool AllowColliderToggling { get; set; }
    
    public BaseScaler()
    {
        this.IncludeChibiMita = PluginConfiguration.ConfigJSON.Configuration.IncludeChibiMita;
        this.IncludeCoreMita = PluginConfiguration.ConfigJSON.Configuration.IncludeCoreMita;
        this.AllowColliderToggling = PluginConfiguration.ConfigJSON.Configuration.AllowColliderToggle;
    }

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

    /// <summary>
    /// Attempts to search a gameobject by its path and adds it to a transform list.
    /// </summary>
    /// <param name="transforms"></param>
    /// <param name="path"></param>
    private void SearchTransform(List<Transform> transforms, string path)
    {
        GameObject seek = GameObject.Find(path);
        if (seek != null)
        {
            transforms.Add(seek.transform);
        }
    }
        
    /// <summary>
    /// Searches more Mitas by Type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name=""></param>
    /// <param name=""></param>
    private void AddMitasByType<T>(List<Transform> result, Func<T, bool> predicate = null) where T : UnityEngine.MonoBehaviour 
    {
        T[] mitas = GameObject.FindObjectsOfType<T>().ToArray();

        foreach (T mita in mitas)
        {
            if (!mita.gameObject.active)
            {
                continue;
            }

            if (mita.transform != null && (predicate == null || predicate(mita))) 
            { 
                result.Add(mita.transform);
            }
        }
    }

    /// <summary>
    /// Finds all Mita Transform objects which are currently active.
    /// </summary>
    /// <returns></returns>
    protected Transform[] FindMitas()
    {
        MitaPerson[] mitas = GameObject.FindObjectsOfType<MitaPerson>().ToArray();

        List<Transform> result = new List<Transform>();

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
                result.Add(mitaTransform);
            }
        }

        this.AddMitasByType<Location5_MitaLegs>(result);
        this.AddMitasByType<Location10_MitaInShadow>(result, m => !m.gameObject.name.ToLower().Contains("maneken"));
        this.AddMitasByType<Location11_BlackMita>(result);        

        //Escape quest Mita
        //this.SearchTransform(result, "World/Quests/Quest1 Побег по коридорам/Location20_RunCorridor/Mita/MitaPerson Mita");
        this.SearchTransform(result, "World/Quests/Quest1 Побег по коридорам/Mita/MitaPerson Mita");

        if (GameObject.Find("World/Quest"))
        {
            if (GameObject.Find("World/Quest/Quest 1/Cutscene MitaFreakEnter") != null)
            {
                this.SearchTransform(result, "World/Quest/Quest 1/Cutscene MitaFreakEnter/Mita/MitaPerson Mita");
                this.SearchTransform(result, "World/Quest/Quest 1/Cutscene MitaFreakEnter/CreepyMita/CreepyMita");
            }
            else
            {
                this.SearchTransform(result, "World/Quest/Quest 2/CreepyMita LongNeck");                
            }
        }

        //Chibi Mita should the player wishes to include her via configuration.
        if (this.IncludeChibiMita)
        {
            Mob_ChibiMita[] chibiMitas = GameObject.FindObjectsOfType<Mob_ChibiMita>().ToArray();

            foreach (Mob_ChibiMita cmt in chibiMitas)
            {
                Animator subAnimator = cmt.gameObject.GetComponentInChildren<Animator>();
                if (subAnimator == null)
                {
                    continue;
                }

                if (!subAnimator.gameObject.active)
                {
                    continue;
                }

                Transform chbMitaTransform = subAnimator.gameObject.transform;

                if (chbMitaTransform != null)
                {
                    result.Add(chbMitaTransform);
                }
            }
        }

        //Core Mita should the player wishes to include her via configuration.
        if (this.IncludeCoreMita)
        {
            MitaCore[] coreMitas = GameObject.FindObjectsOfType<MitaCore>().ToArray();

            foreach (MitaCore mc in coreMitas)
            {
                Animator subAnimator = mc.gameObject.GetComponentInChildren<Animator>();
                if (subAnimator == null)
                {
                    continue;
                }

                if (!subAnimator.gameObject.active)
                {
                    continue;
                }

                Transform coreMitaTransform = subAnimator.gameObject.transform;

                if (coreMitaTransform != null)
                {
                    result.Add(coreMitaTransform);
                }
            }
        }

        return result.ToArray();
    }

    /// <summary>
    /// Finds Mila's glasses game object.
    /// </summary>
    /// <param name="mitaTransform"></param>
    /// <returns></returns>
    protected GameObject? FetchMilaGlassesController(Transform? mitaTransform)
    {
        if (mitaTransform == null) 
        {            
            return null; 
        }

        if (!mitaTransform.gameObject.name.ToLowerInvariant().StartsWith("mila"))
        {
            return null;
        }

        Transform glassesParentTransform = mitaTransform.parent.FindChild("Position Glasses");

        if (glassesParentTransform == null)
        {
            Debug.Log("Unable to find glasses Position.");
            return null;
        }

        return glassesParentTransform.gameObject;
    }

    /// <summary>
    /// Scales Mila's glasses accordingly.
    /// </summary>
    /// <param name="mitaTransform"></param>
    /// <param name="scale"></param>
    protected void ScaleMilaGlasses(Transform mitaTransform, float scale)
    {
        GameObject? glassesController = this.FetchMilaGlassesController(mitaTransform);

        if (glassesController != null)
        {
            Transform mgTransform = glassesController.transform;
            mgTransform.localScale = new Vector3(scale, scale, scale);

            Transform_SimpleCopyPosition tscp = glassesController.GetComponent<Transform_SimpleCopyPosition>();
            if (tscp != null)
            {
                tscp.positionAdd = scale * new Vector3(0.00f, 0.09f, 0.14f);
            }
        }
    }


    /// <summary>
    /// Finds an accessory Mita might be holding to make sure its scale can be properly reset.
    /// </summary>
    /// <param name="mitaTransform"></param>
    /// <returns></returns>
    protected void FixAccessoryObjectScale(Transform? mitaTransform, bool useParentName, string mitaNameStart, string accessoryPath, float intendedScale)
    {
        if (mitaTransform == null)
        {
            return;
        }

        string goName = useParentName ? mitaTransform.parent.gameObject.name:mitaTransform.gameObject.name;
        if (!goName.ToLowerInvariant().StartsWith(mitaNameStart))
        {
            return;
        }

        Transform accessoryTransform = mitaTransform.Find(accessoryPath);        

        if (accessoryTransform == null)
        {
            return;
        }

        if (accessoryTransform.localScale.x != intendedScale)
        {
            accessoryTransform.localScale = new Vector3(intendedScale, intendedScale, intendedScale);
        }
    }

    /// <summary>
    /// Scales Mila's glasses and other accossories accordingly.
    /// </summary>
    /// <param name="mitaTransform"></param>
    /// <param name="scale"></param>
    protected void ScaleMitaAccessories(Transform mitaTransform)
    {
        this.FixAccessoryObjectScale(mitaTransform, false, "mitaperson mita", "Head/Mita'sGlasses", 1.0f);
        this.FixAccessoryObjectScale(mitaTransform, false, "mita dream", "RightItem/CozyPillows Big", 1.0f);
    }

    /// <summary>
    /// Finds all the player copies in the door opening event.
    /// </summary>
    /// <returns></returns>
    protected Transform[] FindPlayerDoorGlitchTransforms()
    {
        GameObject? doorOpeningQuest = GameObject.Find("World/Quests/General/Открытие двери");
        if (doorOpeningQuest == null) 
        {
            return new Transform[0];
        }

        Transform[] allPlayers = doorOpeningQuest.GetComponentsInChildren<Transform>(false).Where(k => k.gameObject.name == "Person").ToArray();
        List<Transform> playerArmatures = new List<Transform>();

        foreach (Transform t in allPlayers)
        {
            if (!t.gameObject.active)
            {
                continue;
            }

            Transform armature = t.FindChild("Armature");
            if (armature != null && armature.gameObject.active)
            {
                playerArmatures.Add(armature);
            }
        }

        return playerArmatures.ToArray();
    }

    /// <summary>
    /// Sets the scale of a transform and potentially includes the collider.
    /// </summary>
    /// <param name="t"></param>
    /// <param name="scale"></param>
    /// <param name="includeCollider"></param>
    /// <returns>True if there was scaling to be done.</returns>
    protected bool SetTransformScale(Transform t, float scale, bool includeCollider = true)
    {
        if (t.localScale.x != scale)
        {
            t.localScale = new Vector3(scale, scale, scale);

            if (includeCollider && t.gameObject.TryGetComponent<CapsuleCollider>(out CapsuleCollider capsuleCollider))
            {
                // Shrinks the collider radius so that players don't get pushed away.

                float initialRadius = capsuleCollider.GetInitialRadius();
                float setRadius = initialRadius / scale;

                capsuleCollider.radius = setRadius;
            }

            return true;
        }

        return false;
    }
}
