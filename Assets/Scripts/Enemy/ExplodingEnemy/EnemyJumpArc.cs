using UnityEngine;
using UnityEngine.AI;

public class EnemyJumpArc : MonoBehaviour
{
    public NavMeshAgent agent;
    private Vector3 arcStart, arcEnd;
    private float arcHeight;
    private float arcDuration;
    private float arcTimer = 0;
    private bool isArcing = false;

    public void LaunchAsArc(Vector3 end, float height, float duration)
    {
        agent = agent ?? GetComponent<NavMeshAgent>();
        if (agent) agent.enabled = false;

        arcStart = transform.position;
        arcEnd = end;
        arcHeight = height;
        arcDuration = duration;
        arcTimer = 0;
        isArcing = true;

    }

    void Update()
    {
        if (!isArcing) return;

        arcTimer += Time.deltaTime;
        float t = Mathf.Clamp01(arcTimer / arcDuration);
        Vector3 pos = Vector3.Lerp(arcStart, arcEnd, t);
        pos.y += Mathf.Sin(Mathf.PI *t) * arcHeight;
        transform.position = pos;

        // face direction of movement
        Vector3 look = arcEnd - arcStart; look.y = 0;
        if (look.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(look);
        }
        

        if(t >= 1f)
        {
            isArcing = false;
            if (agent)
            {
                transform.position = arcEnd;
                agent.enabled = true;

                if (agent.isOnNavMesh)
                {
                    agent.Warp(transform.position);
                    agent.isStopped = false;
                }
            }
        }
    }
}