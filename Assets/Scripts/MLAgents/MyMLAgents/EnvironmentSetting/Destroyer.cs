using UnityEngine;

public class Destroyer : MonoBehaviour
{
    public void ClearObjects(GameObject Objects)
    {
        foreach (Transform child in Objects.transform)
        {
            Destroy(child.gameObject);
        }
    }
}
