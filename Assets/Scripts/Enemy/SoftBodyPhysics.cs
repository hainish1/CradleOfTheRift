using System;
using UnityEngine;



public class SoftBodyPhysics : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Transform visual;   // child mesh

    [Header("Motion")]
    [SerializeField] float velocitySmoothing = 12f; 
    [SerializeField] float maxVelForFullStretch = 6f;

    [Header("Squash")]
    [SerializeField] float squashAmount = 0.25f;    
    [SerializeField] float stiff = 10f;            
    [SerializeField] float damping = 0.85f;         
    
    [Header("Wobble")]
    [SerializeField] float tiltAmount = 0.15f;      
    [SerializeField] float wobbleShake = 0.06f;
    [SerializeField] float fallBackWobble = 0.6f;    

    Vector3 lastPos;
    Vector3 smoothedVel;
    float yScale = 1f;
    float yVel; 

    void Reset()
    {
        if (visual == null && transform.childCount > 0)
            visual = transform.GetChild(0);
    }

    void Awake()
    {
        lastPos = transform.position;
    }

    void LateUpdate()
    {
        if (!visual) return;

        
        Vector3 rawVel = (transform.position - lastPos) / Mathf.Max(Time.deltaTime, 1e-5f); // get raw vel to maintain the jellyness positions
        lastPos = transform.position;

        smoothedVel = Vector3.Lerp(smoothedVel, rawVel, 1f - Mathf.Exp(-velocitySmoothing * Time.deltaTime));
        Vector3 planarVel = new Vector3(smoothedVel.x, 0f, smoothedVel.z);
        float speed01 = Mathf.Clamp01(planarVel.magnitude / Mathf.Max(0.01f, maxVelForFullStretch));

        
        float targetY = 1f - squashAmount * speed01;


        float accel = (targetY - yScale) * stiff;
        yVel += accel * Time.deltaTime;
        yVel *= Mathf.Pow(damping, Time.deltaTime * 60f);   
        yScale += yVel * Time.deltaTime;

        yScale += Mathf.Sin(Time.time * 16.3f) * wobbleShake * 0.2f;


        float inv = 1f / Mathf.Sqrt(Mathf.Max(0.0001f, yScale));
        Vector3 targetScale = new Vector3(inv, yScale, inv);


        Quaternion tilt = Quaternion.identity;
        if (planarVel.sqrMagnitude > 1e-4f)
        {
            Vector3 dir = planarVel.normalized;
            Vector3 tiltAxis = Vector3.Cross(Vector3.up, dir); 
            float tiltAngle = -tiltAmount * planarVel.magnitude; 
            tilt = Quaternion.AngleAxis(tiltAngle, tiltAxis);
        }


        visual.localRotation = tilt;
        visual.localScale = Vector3.Lerp(visual.localScale, targetScale, 1f - Mathf.Exp(-20f * Time.deltaTime));
    }


    public void Impulse()
    {
        // fallBackWobble = strength;
        yVel -= Mathf.Abs(fallBackWobble);
    }
}
