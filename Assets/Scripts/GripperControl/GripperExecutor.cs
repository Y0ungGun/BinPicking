using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;


namespace GripperControl
{
    public class GripperExecutor : MonoBehaviour
    {
        public ArticulationBody LinkLefty;
        public ArticulationBody LinkRighty;
        public ArticulationBody JawLefty;
        public ArticulationBody JawRighty;
        public ArticulationBody LinkLeftyInner;
        public ArticulationBody LinkRightyInner;

        public float velocityRatio = 100;
        public float jointValue = 0;

        private List<float> positions = new List<float>();
        private void Start()
        {
            float velocity = 30 * (velocityRatio / 100);

            StartCoroutine(LogJointPosition());

        }
        private void Update()
        {

            ArticulationReducedSpace Lefty1 = new ArticulationReducedSpace(jointValue);
            ArticulationReducedSpace Righty1 = new ArticulationReducedSpace(-jointValue);
            LinkLefty.jointPosition = Lefty1;
            LinkRighty.jointPosition = Righty1;
            JawLefty.jointPosition = Righty1;
            JawRighty.jointPosition = Lefty1;
            LinkLeftyInner.jointPosition = Lefty1;
            LinkRightyInner.jointPosition = Righty1;

        }

        IEnumerator LogJointPosition()
        {
            while (true)
            {
                yield return new WaitForSeconds(5f);
                ArticulationReducedSpace joint = LinkLefty.jointPosition;
                LinkLefty.GetJointPositions(positions);
                Debug.Log("JointPositions = " + string.Join(", ", positions));
                Debug.Log("Single Joint Value = " + joint[0]);
            }
        }

    }
}
