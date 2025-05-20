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
        // �浹 �̺�Ʈ�� �ܺη� �˸�
        OnCollisionEnterEvent?.Invoke(collision);
    }
}
