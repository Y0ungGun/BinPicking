using UnityEngine;

public class RandomSpawnTester : MonoBehaviour
{
    public bool debug;

    public GameObject obj1;
    public GameObject obj2;
    public GameObject obj3;
    public GameObject obj4;

    private RandomSpawnExample script1;
    private RandomSpawnExample script2;
    private RandomSpawnExample script3;
    private RandomSpawnExample script4;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        debug = true;
        script1 = obj1.GetComponent<RandomSpawnExample>();
        script2 = obj2.GetComponent<RandomSpawnExample>();
        script3 = obj3.GetComponent<RandomSpawnExample>();
        script4 = obj4.GetComponent<RandomSpawnExample>();
    }

    // Update is called once per frame
    void Update()
    {
        if (debug)
        {
            script1.RandomizeTargetPosition();
            script2.RandomizeTargetPosition();
            script3.RandomizeTargetPosition();
            script4.RandomizeTargetPosition();

            debug = false;
        }
    }
}
