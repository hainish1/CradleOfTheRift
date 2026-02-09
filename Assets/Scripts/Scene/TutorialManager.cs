using System;
using System.Collections.Generic;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    [SerializeField] private List<TutorialEvent> eventsList = new List<TutorialEvent>();
    public static event Action<string> OnTutorialEvent;

    private void OnEnable()
    {
        OnTutorialEvent += ExecuteEvent;
    }

    private void OnDisable()
    {
        OnTutorialEvent -= ExecuteEvent;
    }

    public static void TriggerTutorialEvent(string eventId)
    {
        OnTutorialEvent?.Invoke(eventId);
    }

    private void ExecuteEvent(string eventId)
    {
        foreach (TutorialEvent e in eventsList)
        {
            if (e.EventID == eventId)
            {
                if (e.DestructBarrier != null) Destroy(e.DestructBarrier);
                break;
            }
        }
    }
}

[Serializable] public class TutorialEvent
{
    [field: SerializeField] public GameObject DestructBarrier { get; private set; }
    [field: SerializeField] public string EventID { get; private set; }
}
