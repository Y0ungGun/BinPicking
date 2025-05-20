using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DSRRobotControl
{
    public class Link1Collider : MonoBehaviour
    {
        public static bool Link1collision;
        public static bool Link1collisionDebugged;
        // public ArticulationBody link1;
        private void OnTriggerEnter(Collider collision)
        {
            if (!collision.name.EndsWith("Collider_DSR"))
            {
                Link1collision = true;
                Link1collisionDebugged = false;
            }
        }
        private void OnTriggerExit(Collider collision)
        {
            if (!collision.name.EndsWith("Collider_DSR"))
            {
                Link1collision = false;
                Link1collisionDebugged = false;
            }
        }
    }
}

