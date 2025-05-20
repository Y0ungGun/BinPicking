using System;
using Unity.MLAgents;
using UnityEngine;

public class MyGraspManager : MonoBehaviour
{
    private MyGraspAgent m_GraspAgent;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Academy.Instance.AutomaticSteppingEnabled = false;
        
        m_GraspAgent = GetComponentsInChildren<MyGraspAgent>()[0];
    }

    // Update is called once per frame
    void Update()
    {
        if (!m_GraspAgent.GetisActionInProgress())
        {
            m_GraspAgent.RequestDecision();
            Academy.Instance.EnvironmentStep();
        }
    }
}
