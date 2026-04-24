using UnityEngine;

public class SpinOrOrbit : MonoBehaviour
{
    public Vector3 axis = Vector3.up;
    public float speed = 20f;

    void Update()
    {
        transform.Rotate(axis.normalized, speed * Time.deltaTime, Space.Self);
    }
}