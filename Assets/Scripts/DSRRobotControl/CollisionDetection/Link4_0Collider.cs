using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DSRRobotControl
{
    public class Link4_0Collider : MonoBehaviour
    {
        public static bool Link4_0collision;
        public static bool Link4_0collisionDebugged;
        // public ArticulationBody link1;
        private void OnTriggerEnter(Collider collision)
        {
            if (!collision.name.EndsWith("Collider_DSR"))
            {
                Link4_0collision = true;
                Link4_0collisionDebugged = false;
            }
        }
        private void OnTriggerExit(Collider collision)
        {
            if (!collision.name.EndsWith("Collider_DSR"))
            {
                Link4_0collision = false;
                Link4_0collisionDebugged = false;
            }
        }
    }
}

