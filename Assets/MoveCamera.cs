using UnityEngine;

public class MoveCamera : MonoBehaviour
{
    public float moveSpeed = 5f;

    public Transform target;
    public bool followPosition = false;
    public Vector3 offset = new Vector3(0f, 2f, -5f);

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.M))
        {
            transform.position += transform.forward * moveSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.U))
        {
            transform.position += transform.up * moveSpeed * Time.deltaTime;
        }

        if (target != null)
        {
            if (followPosition)
            {
                transform.position = target.position + offset;
            }

            transform.LookAt(target);
        }
    }
}
