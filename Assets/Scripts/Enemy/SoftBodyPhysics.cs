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

    [Header("Safety Limits")]
    [SerializeField] float minScale = .1f; // prevent shrinking too much
    [SerializeField] float maxScale = 3f; // prevent taking up whole screen lmao
    [SerializeField] float maxVelocity = 50f; // clamp velocity to prevent explosions


    Vector3 lastPos;
    Vector3 smoothedVel;
    float yScale = 1f;
    float yVel;
    Vector3 initialScale;// store original one

    void Reset()
    {
        if (visual == null && transform.childCount > 0)
            visual = transform.GetChild(0);
    }

    void Awake()
    {
        lastPos = transform.position;
        if (visual != null)
        {
            initialScale = visual.localScale;
        }
        else
        {
            initialScale = Vector3.one;
        }
    }

    void LateUpdate()
    {
        if (!visual) return;

        float deltaTime = Mathf.Max(Time.deltaTime, 1e-5f);
        Vector3 rawVel = (transform.position - lastPos) / deltaTime; // get raw vel to maintain the jellyness positions
        lastPos = transform.position;

        // SAFETY bruh
        if (rawVel.magnitude > maxVelocity)
        {
            rawVel = rawVel.normalized * maxVelocity;
        }

        // smooth vel
        float smoothingFactor = 1f - Mathf.Exp(-velocitySmoothing * deltaTime);
        smoothedVel = Vector3.Lerp(smoothedVel, rawVel, smoothingFactor);

        Vector3 planarVel = new Vector3(smoothedVel.x, 0f, smoothedVel.z);
        float speed01 = Mathf.Clamp01(planarVel.magnitude / Mathf.Max(0.01f, maxVelForFullStretch));

        // calculate target Y scale with limits
        float targetY = 1f - squashAmount * speed01;
        targetY = Mathf.Clamp(targetY, minScale, maxScale); // SAFETY: PLEASE WORK
        
        // physics with safety is my responsibility
        float accel = (targetY - yScale) * stiff;
        yVel += accel * Time.deltaTime;

        float dampingFactor = Mathf.Pow(damping, deltaTime * 60f);
        dampingFactor = Mathf.Clamp01(dampingFactor);
        yVel *= dampingFactor;

        // SAFETY FOR VELOCITY
        yVel = Mathf.Clamp(yVel, -10f, 10f);

        yScale += yVel * Time.deltaTime;

        // SAFETY: Clamp yScale to safe bounds
        yScale = Mathf.Clamp(yScale, minScale, maxScale);

        //now add wobble
        float wobble = Mathf.Sin(Time.time * 16.3f) * wobbleShake * 0.1f; // Reduced from 0.2f
        yScale += wobble;

        // MORE FINAL safety clamp after wobble
        yScale = Mathf.Clamp(yScale, minScale, maxScale);

        float safeYScale = Mathf.Max(.1f, yScale);
        float inv = 1f / Mathf.Sqrt(safeYScale);
        inv = Mathf.Clamp(inv, minScale, maxScale); // also clamp inverse scale

        Vector3 targetScale = new Vector3(inv, yScale, inv);

        // init scale mult
        targetScale.x *= initialScale.x;
        targetScale.y *= initialScale.y;
        targetScale.z *= initialScale.z;

        // now the tilt
        Quaternion tilt = Quaternion.identity;
        if (planarVel.sqrMagnitude > 1e-4f)
        {
            Vector3 dir = planarVel.normalized;
            Vector3 tiltAxis = Vector3.Cross(Vector3.up, dir); 
            float tiltAngle = -tiltAmount * Mathf.Clamp(planarVel.magnitude, 0f, 10f); // Clamp tilt
            tilt = Quaternion.AngleAxis(tiltAngle, tiltAxis);
        }

        // apply trans with smooth interpolation
        visual.localRotation = Quaternion.Slerp(visual.localRotation, tilt, 1f - Mathf.Exp(-20f * deltaTime));
        visual.localScale = Vector3.Lerp(visual.localScale, targetScale, 1f - Mathf.Exp(-20f * deltaTime));
    }


    public void Impulse(float strength = 1f)
    {
        // SAFETY: Clamp impulse strength
        strength = Mathf.Clamp(strength, 0f, 2f);
        yVel -= Mathf.Abs(fallBackWobble * strength);
        
        // Ensure yVel doesn't go crazy
        yVel = Mathf.Clamp(yVel, -10f, 10f);
    }
}
