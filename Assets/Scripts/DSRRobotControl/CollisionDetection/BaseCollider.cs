using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DSRRobotControl
{
    public class BaseCollider : MonoBehaviour
    {
        public static bool Basecollision;
        public static bool BasecollisionDebugged;
        // public ArticulationBody link1;
        private void OnTriggerEnter(Collider collision)
        {
            if (!collision.name.EndsWith("Collider_DSR"))
            {
                Basecollision = true;
                BasecollisionDebugged = false;
            }
        }
        private void OnTriggerExit(Collider collision)
        {
            if (!collision.name.EndsWith("Collider_DSR"))
            {
                Basecollision = false;
                BasecollisionDebugged = false;
            }
        }
    }
}

