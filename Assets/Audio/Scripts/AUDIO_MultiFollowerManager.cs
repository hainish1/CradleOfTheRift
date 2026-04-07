using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class AUDIO_MultiFollowerManager : MonoBehaviour
{
    private List<AUDIO_AmbientFollower> followers = new List<AUDIO_AmbientFollower>();
    [SerializeField]
    private AkAmbient ambientPlayer;
    private Transform player;
    
    void Start()
    {
        // Get a list of all follower children.
        AUDIO_AmbientFollower[] allFollowers = GameObject.FindObjectsByType<AUDIO_AmbientFollower>(FindObjectsSortMode.None);
        foreach (var follower in allFollowers)
        {
            if (follower.transform.IsChildOf(this.transform))
            {
                followers.Add(follower);
            }
        }
        // Get the player.
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        AUDIO_AmbientFollower follower = GetClosestFollower();
        // Bestow the audio player onto the closest follower.
        // ambientPlayer.transform.parent = follower.transform;
        ambientPlayer.transform.SetParent(follower.transform, false);
    }

    private AUDIO_AmbientFollower GetClosestFollower()
    {
        // Loop through the followers and find the one closest to the player.
        float minDistance = float.MaxValue;
        AUDIO_AmbientFollower closesetFollower = null;
        foreach(var follower in followers)
        {
            float distance = Vector3.Distance(follower.transform.position, player.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closesetFollower = follower;
            }
        }
        return closesetFollower;
    }
}
