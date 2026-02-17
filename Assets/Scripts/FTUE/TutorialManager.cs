// <summary>
//   <authors>
//     Samuel Rigby
//   </authors>
//   <para>
//     Written by Samuel Rigby for GAMES 4510, University of Utah, February 2026.
//   </para>
// </summary>

using System;
using System.Collections.Generic;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    [SerializeField] private List<TutorialEvent> events = new List<TutorialEvent>();
    public static event Action<string> OnTutorialEvent;

    private void OnEnable()
    {
        OnTutorialEvent += QueryEvent;
    }

    private void OnDisable()
    {
        OnTutorialEvent -= QueryEvent;
    }

    void Start()
    {
        // Ensure all EnableObject types are disabled on start.
        foreach (TutorialEvent t in events)
        {
            if (t.TutorialEventType == tutorialEventType.EnableObject)
                t.TargetObject.SetActive(false);
        }
    }

    /// <summary>
    ///   <para>
    ///     Triggers the event of a given name when static called.
    ///   </para>
    /// </summary>
    /// <param name="eventName"> Name of event. </param>
    public static void TriggerTutorialEvent(string eventName)
    {
        OnTutorialEvent?.Invoke(eventName);
    }

    /// <summary>
    ///   <para>
    ///     Queries the list of all tutorial events when called and executes the
    ///     first match found.
    ///   </para>
    /// </summary>
    /// <param name="eventName"> Name of event. </param>
    private void QueryEvent(string eventName)
    {
        foreach (TutorialEvent e in events)
        {
            if (e.EventName == eventName)
            {
                // Do not execute event until all objects in destruct group are destroyed.
                if (e.destructGroup.Count > 0) e.destructGroup.RemoveAt(0);
                if (e.destructGroup.Count == 0) ExecuteEvent(e);
                break;
            }
        }
    }

    /// <summary>
    ///   <para>
    ///     Executes a tutorial event based on its properties.
    ///   </para>
    /// </summary>
    /// <param name="e"> TutorialEvent class with properties. </param>
    private void ExecuteEvent(TutorialEvent e)
    {
        switch (e.TutorialEventType)
        {
            case tutorialEventType.DestroyObject:
                Destroy(e.TargetObject);
                break;
            case tutorialEventType.EnableObject:
                e.TargetObject.SetActive(true);
                break;
        }
    }
}

/// <summary>
///   <para>
///     A class to represent tutorial events. Supports functionality for destroying
///     physical barriers, enabling objects in the scene, and the option to require
///     destroying a group of objects before the event can happen.
///   </para>
/// </summary>
[Serializable] public class TutorialEvent
{
    public tutorialEventType TutorialEventType;
    public GameObject TargetObject;
    public string EventName;
    
    // The event happens when all objects in destruct group are destroyed.
    public List<GameObject> destructGroup = new List<GameObject>();
}

/// <summary>
///   <para>
///     The variety of supported tutorial event types.
///   </para>
/// </summary>
public enum tutorialEventType
{
    DestroyObject,
    EnableObject
}
