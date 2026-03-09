// <summary>
//   <authors>
//     Samuel Rigby
//   </authors>
//   <para>
//     Written by Samuel Rigby for GAMES 4510, University of Utah.
//   </para>
// </summary>

using System;
using System.Collections.Generic;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    [SerializeField] private List<TutorialEvent> events = new();
    public static Action<string> OnTutorialEvent;

    private void OnEnable()
    {
        OnTutorialEvent += QueryEvent;
    }

    private void OnDisable()
    {
        OnTutorialEvent -= QueryEvent;
    }

    /// <summary>
    ///   <para>
    ///     Queries the list of all tutorial events and executes the
    ///     first match found.
    ///   </para>
    /// </summary>
    /// <param name="eventName"> Name of event. </param>
    private void QueryEvent(string eventName)
    {
        foreach (TutorialEvent tutorialEvent in events)
        {
            if (tutorialEvent.EventName != eventName) continue;

            // Do not execute event until all objects in destruct group are destroyed.
            if (tutorialEvent.DestructGroup.Count > 0) tutorialEvent.DestructGroup.RemoveAt(0);
            if (tutorialEvent.DestructGroup.Count == 0) ExecuteTasks(tutorialEvent);
            return;
        }
        Debug.LogWarning($"No tutorial event of name \"{eventName}\" exists.");
    }

    /// <summary>
    ///   <para>
    ///     Executes the tasks of a tutorial event.
    ///   </para>
    /// </summary>
    /// <param name="tutorialEvent"> TutorialEvent class. </param>
    private void ExecuteTasks(TutorialEvent tutorialEvent)
    {
        foreach (TutorialTask task in tutorialEvent.Tasks)
        {
            if (task.TargetObject == null) continue; // Skip if target object is null.
            switch (task.TaskType)
            {
                case TutorialTaskType.EnableObject:
                    task.TargetObject.SetActive(true);
                    break;
                case TutorialTaskType.DisableObject:
                    task.TargetObject.SetActive(false);
                    break;
                case TutorialTaskType.DestroyObject:
                    Destroy(task.TargetObject);
                    break;
                case TutorialTaskType.TeleportObject:
                    TeleportTarget(task);
                    break;
                default:
                    break;
            }
        }
    }

    /// <summary>
    ///   <para>
    ///     Teleports a target object to a target position.
    ///   </para>
    /// </summary>
    /// <param name="task"> TutorialTask class. </param>
    private void TeleportTarget(TutorialTask task)
    {
        CharacterController characterController = task.TargetObject.GetComponent<CharacterController>();
        Rigidbody rigidbody = task.TargetObject.GetComponent<Rigidbody>();
        if ((characterController && !rigidbody) || (!characterController && !rigidbody)) // If CharacterController or neither.
        {
            if (characterController != null) characterController.enabled = false; // Disable CharacterController to allow direct position modification.
            NonRigidbodyTeleport(task);
            if (characterController != null) characterController.enabled = true; // Re-enable CharacterController after teleport.
        }
        else if (rigidbody && !characterController) // If Rigidbody.
            RigidbodyTeleport(task, rigidbody);
    }

    /// <summary>
    ///   <para>
    ///     Handles teleportation for objects with a CharacterController and objects lacking
    ///     both a CharacterController and Rigidbody.
    ///   </para>
    /// </summary>
    /// <param name="task"> TutorialTask class. </param>
    private void NonRigidbodyTeleport(TutorialTask task)
    {
        if (task.TeleportType == TutorialTeleportType.Manual)
            task.TargetObject.transform.SetPositionAndRotation(task.TargetPosition, Quaternion.Euler(task.TargetOrientation));
        else if (task.TeleportType == TutorialTeleportType.Object)
            task.TargetObject.transform.SetPositionAndRotation(task.TargetTransform.position, task.TargetTransform.rotation);
    }

    /// <summary>
    ///   <para>
    ///     Handles teleportation for objects with a Rigidbody.
    ///   </para>
    /// </summary>
    /// <param name="task"> TutorialTask class. </param>
    /// <param name="rigidbody"> The target object's Rigidbody. </param>
    private void RigidbodyTeleport(TutorialTask task, Rigidbody rigidbody)
    {
        if (task.TeleportType == TutorialTeleportType.Manual)
            rigidbody.transform.SetPositionAndRotation(task.TargetPosition, Quaternion.Euler(task.TargetOrientation));
        else if (task.TeleportType == TutorialTeleportType.Object)
            rigidbody.transform.SetPositionAndRotation(task.TargetTransform.position, task.TargetTransform.rotation);
    }
}

/// <summary>
///   <para>
///     A class to represent tutorial events. Supports the option to require
///     destroying a group of objects before an event's tasks can execute.
///   </para>
/// </summary>
[Serializable]
public class TutorialEvent
{
    [Tooltip("Specific name of a tutorial event that can be triggered.")] public string EventName;
    [Tooltip("Event will not trigger until everything in the destruct group is destroyed.")] public List<GameObject> DestructGroup = new();
    [Tooltip("Tasks that will be performed when the event is triggered.")] public List<TutorialTask> Tasks = new();
}

/// <summary>
///   <para>
///     A class to represent tutorial Tasks. Supports functionality for
///     enabling/disabling, destroying and teleporting objects in the scene.
///   </para>
/// </summary>
[Serializable]
public class TutorialTask
{
    public TutorialTaskType TaskType;
    [Tooltip("Object to be affected by the chosen task type.")] public GameObject TargetObject;
    [Tooltip("Teleport to a manually typed position and orientaton, or to an empty object in the scene.")] public TutorialTeleportType TeleportType;
    public Vector3 TargetPosition;
    public Vector3 TargetOrientation;
    public Transform TargetTransform;
}

/// <summary>
///   <para>
///     The variety of supported tutorial task types.
///   </para>
/// </summary>
public enum TutorialTaskType
{
    EnableObject,
    DisableObject,
    DestroyObject,
    TeleportObject
}

/// <summary>
///   <para>
///     The variety of teleport position input types.
///   </para>
/// </summary>
public enum TutorialTeleportType
{
    Manual,
    Object
}
