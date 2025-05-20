using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DSRRobotControl
{
    public class Link6Collider : MonoBehaviour
    {
        public static bool Link6collision;
        public static bool Link6collisionDebugged;
        // public ArticulationBody link1;
        private void OnTriggerEnter(Collider collision)
        {
            if (!collision.name.EndsWith("Collider_DSR"))
            {
                Link6collision = true;
                Link6collisionDebugged = false;
            }
        }
        private void OnTriggerExit(Collider collision)
        {
            if (!collision.name.EndsWith("Collider_DSR"))
            {
                Link6collision = false;
                Link6collisionDebugged = false;
            }
        }
    }
}

