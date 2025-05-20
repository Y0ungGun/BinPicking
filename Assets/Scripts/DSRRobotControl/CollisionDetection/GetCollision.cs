using UnityEngine;

public class GetCollision : MonoBehaviour
{
    public delegate void CollisionEnterDelegate(Collision collision);
    public event CollisionEnterDelegate OnCollisionEnterEvent;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnCollisionEnter(Collision collision)
    {
        // 충돌 이벤트를 외부로 알림
        OnCollisionEnterEvent?.Invoke(collision);
    }
}
