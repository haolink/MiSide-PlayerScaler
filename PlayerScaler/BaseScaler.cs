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
}
