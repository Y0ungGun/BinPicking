using Unity.MLAgents;
using UnityEngine;

public class MyGameManager : MonoBehaviour
{
    private MyArmAgent m_ArmAgent;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Academy.Instance.AutomaticSteppingEnabled = false;
        
        m_ArmAgent = GetComponentsInChildren<MyArmAgent>()[0];
    }

    // Update is called once per frame
    void Update()
    {
        if (!m_ArmAgent.GetisActionInProgress())
        {
            m_ArmAgent.RequestDecision();
            Academy.Instance.EnvironmentStep();
        }
    }
}
