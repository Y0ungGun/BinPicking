using MLAgents;
using System;
using Unity.MLAgents;
using UnityEngine;

namespace MyMLAgents
{
    public class MyImageGraspManager : MonoBehaviour
    {
        private MyImageGraspAgent m_ImageGraspAgent;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            Academy.Instance.AutomaticSteppingEnabled = false;

            m_ImageGraspAgent = GetComponentsInChildren<MyImageGraspAgent>()[0];
        }

        // Update is called once per frame
        void Update()
        {
            if (!m_ImageGraspAgent.GetisActionInProgress())
            {
                m_ImageGraspAgent.RequestDecision();
                Academy.Instance.EnvironmentStep();
            }
        }
    }
}
