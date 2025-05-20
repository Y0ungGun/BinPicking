using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DSRRobotControl
{
    public class Link4_1Collider : MonoBehaviour
    {
        public static bool Link4_1collision;
        public static bool Link4_1collisionDebugged;
        // public ArticulationBody link1;
        private void OnTriggerEnter(Collider collision)
        {
            if (!collision.name.EndsWith("Collider_DSR"))
            {
                Link4_1collision = true;
                Link4_1collisionDebugged = false;
            }
        }
        private void OnTriggerExit(Collider collision)
        {
            if (!collision.name.EndsWith("Collider_DSR"))
            {
                Link4_1collision = false;
                Link4_1collisionDebugged = false;
            }
        }
    }
}

