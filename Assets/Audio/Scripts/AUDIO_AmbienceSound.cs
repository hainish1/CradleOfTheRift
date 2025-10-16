using UnityEngine;
// Following https://www.youtube.com/watch?v=8-SnHgtXV3k

public class AUDIO_AmbienceSound : MonoBehaviour
{
    // The area the ambience can play within.
    public Collider Area;
    // The player to track.
    public GameObject Player;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        // Using the Area, try to get the audio player as close as possible to the player.
        // This creates a fun effect of having the audio fade in and out as the player crosses the boundary.
        // Once the player enters the area, the volume of the audio should be maxed.
        Vector3 closestPoint = Area.ClosestPoint(Player.transform.position);
        // Get as close to the player as possible.
        transform.position = closestPoint;
    }
}
