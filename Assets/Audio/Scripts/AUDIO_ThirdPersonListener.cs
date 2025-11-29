using UnityEngine;
using System;
using System.Collections.Generic;

// FROM: https://alessandrofama.com/tutorials/wwise/unity/third-person-listener
// ALL CREDIT TO Alessandro Famà!!

// IF SOMETHING GOES WRONG ITS BECAUSE Transform player IS NOT SET!!!
// You gotta go into debug mode for whatever reason to set the variable...
[ExecuteInEditMode]
public class AUDIO_ThirdPersonListener : AkGameObj
{
    public Transform player;
    
    void Start()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player").transform;
        }
    }

    public override Vector3 GetPosition()
    {
        return player.GetComponent<AkGameObj>().GetPosition();
    }
}