using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImpulseTest : MonoBehaviour
{
    private float nextLogTime = 0f;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnCollisionStay(Collision collision)
    {
        if (Time.time >= nextLogTime)
        {
            Vector3 impulse = collision.impulse;
            Debug.Log("Impulse: " + impulse.x + ", " + impulse.y + ", " + impulse.z);

            nextLogTime = Time.time + 3f; // 3초 후에 다시 로그 출력 가능
        }
        foreach (ContactPoint contact in collision.contacts)
        {
            Debug.DrawRay(contact.point, contact.normal * 100, Color.white);
        }
    }
}
