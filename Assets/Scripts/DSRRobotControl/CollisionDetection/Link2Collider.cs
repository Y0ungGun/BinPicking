using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DSRRobotControl
{
    public class Link2Collider : MonoBehaviour
    {
        public static bool Link2collision;
        public static bool Link2collisionDebugged;
        // public ArticulationBody link1;
        private void OnTriggerEnter(Collider collision)
        {
            if (!collision.name.EndsWith("Collider_DSR"))
            {
                Link2collision = true;
                Link2collisionDebugged = false;
            }
        }
        private void OnTriggerExit(Collider collision)
        {
            if (!collision.name.EndsWith("Collider_DSR"))
            {
                Link2collision = false;
                Link2collisionDebugged = false;
            }
        }
    }
}

