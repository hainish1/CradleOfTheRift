using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMeleeAttack : MonoBehaviour
{
    [Header("whats forward")]
    [SerializeField] private Transform forwardSource;           
    [SerializeField] private float heightOffset = 0.9f;         

    [Header("Hit Box (where things can get hit)")]
    [SerializeField] private float boxWidth = 1.8f;             
    [SerializeField] private float boxHeight = 1.6f;            
    [SerializeField] private float boxArea = 2.6f;             
    [Header("Melee Timing stuff")]
    [SerializeField] private float cooldown = 0.35f;
    [SerializeField] private float prep = 0.05f;                
    [SerializeField] private float activeTime = 0.08f;          
    [Header("Attack Impact phys stuff")]
    [SerializeField] private float pushForce = 12f;
    [SerializeField] private float upwardForce = 0f;
    [SerializeField] private LayerMask hitMask = ~0;
    [Header("Debug")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private Color gizmoColor = Color.blue;

    // input
    private InputSystem_Actions input;
    private InputSystem_Actions.PlayerActions actions;
    private InputAction meleeAction;

    // state
    private float nextAllowedTime;
    private bool isPrepping;
    private float prepEndsAt;
    private float activeUntil;

    private readonly Collider[] overlapBuffer = new Collider[32]; // ts is for checking who got hit
    private readonly HashSet<Transform> hitThisSwing = new();


    // calculating the fwd
    private Transform Forward => forwardSource ? forwardSource : transform;

    void OnEnable()
    {
        if (input == null)
        {
            input = new InputSystem_Actions();
        }
        actions = input.Player;
        meleeAction = actions.Melee;
        meleeAction?.Enable();
    }

    void OnDisable()
    {
        meleeAction?.Disable();
    }

    void Update()
    {
        // Start 
        if (meleeAction != null && meleeAction.WasPressedThisFrame())
            TryStartMelee();

        if (isPrepping && Time.time >= prepEndsAt)
        {
            isPrepping = false;                 
            activeUntil = Time.time + activeTime;
            hitThisSwing.Clear();
            DoHit();                            
        }

        
        if (Time.time < activeUntil)
            DoHit();
    }

    private void TryStartMelee()
    {
        if (Time.time < nextAllowedTime) return;
        nextAllowedTime = Time.time + cooldown;

        if (prep > 0f)
        {
            isPrepping = true;
            prepEndsAt = Time.time + prep;
        }
        else
        {
            activeUntil = Time.time + activeTime;
            hitThisSwing.Clear();
            DoHit();
        }
    }

    private void DoHit()
    {
        // make the box in front of the player
        var t = Forward;
        Vector3 up = t.up;
        Vector3 fwd = t.forward;
        Vector3 right = t.right;

        
        Vector3 origin = t.position + up * heightOffset + fwd * (boxArea * 0.5f);
        Vector3 halfExtents = new Vector3(boxWidth, boxHeight, boxArea) * 0.5f;
        Quaternion orientation = Quaternion.LookRotation(fwd, up); // where is the box looking and whats up

        int count = Physics.OverlapBoxNonAlloc(
            origin, halfExtents, overlapBuffer, orientation, hitMask, QueryTriggerInteraction.Ignore); // only overlap with hitmask things

        for (int i = 0; i < count; i++)
        {
            var c = overlapBuffer[i]; // get stuff into the array
            if (c == null) continue; // check if theres hit

            
            Transform root = c.attachedRigidbody ? c.attachedRigidbody.transform : c.transform; // check for attached rigidbody, this is need in enemy for pushing it back
            if (root == transform || hitThisSwing.Contains(root)) continue;
      
            Vector3 to = (root.position - (t.position + up * heightOffset));
            to.y = 0f;
            Vector3 direction = (to.sqrMagnitude > 0.001f) ? to.normalized : fwd; // direction of hit

            if (c.attachedRigidbody && !c.attachedRigidbody.isKinematic) // make sure object hitting is not kinematic
            {
                Vector3 force = direction * pushForce + up * upwardForce;
                c.attachedRigidbody.AddForce(force, ForceMode.Impulse);
            }

            hitThisSwing.Add(root);
        }
    }

    // Gizmos
    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        var t = Forward ? Forward : transform;
        Vector3 up = t.up;
        Vector3 fwd = t.forward;

        Vector3 origin = t.position + up * heightOffset + fwd * (boxArea * 0.5f);
        Vector3 size = new Vector3(boxWidth, boxHeight, boxArea); // size fo box to draw
        Quaternion rot = Quaternion.LookRotation(fwd, up); // rotation of box

        var prevColor = Gizmos.color;
        var prevMatrix = Gizmos.matrix;

        Gizmos.color = gizmoColor;
        Gizmos.matrix = Matrix4x4.TRS(origin, rot, Vector3.one);
        Gizmos.DrawCube(Vector3.zero, size);


        Gizmos.matrix = prevMatrix;
        Gizmos.color = prevColor;
    }

}
