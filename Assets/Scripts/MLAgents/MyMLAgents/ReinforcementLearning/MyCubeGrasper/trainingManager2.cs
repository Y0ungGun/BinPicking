using MLAgents;
using Unity.MLAgents;
using UnityEngine;

namespace MyMLAgents
{
    public class trainingManager2 : MonoBehaviour
    {
        private trainer2 m_trainer;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            Academy.Instance.AutomaticSteppingEnabled = false;
            m_trainer = GetComponentsInChildren<trainer2>()[0];
            Academy.Instance.EnvironmentStep();
        }

        // Update is called once per frame
        void Update()
        {
            if (!m_trainer.GetisActionInProgress())
            {
                //Debug.LogWarning("Action is not in progress");
                m_trainer.RequestDecision();
                Academy.Instance.EnvironmentStep();
            }
        }
    }
}

