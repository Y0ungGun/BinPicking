using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MathNet.Numerics.LinearAlgebra.Double;
using Vector = MathNet.Numerics.LinearAlgebra.Vector<double>;

namespace DSRRobotControl
{
    public static class Quat2RPY
    {
        public static Vector3 Quat2rpy(Quaternion quaternion)
        {
            float q0 = quaternion.w;
            float q1 = quaternion.x;
            float q2 = quaternion.y;
            float q3 = quaternion.z;

            float roll = Mathf.Atan2(2 * q2 * q3 + 2 * q0 * q1, q3 * q3 - q2 * q2 - q1 * q1 + q0 * q0);
            float pitch = -Mathf.Asin(2 * q1 * q3 - 2 * q0 * q2);
            float yaw = Mathf.Atan2(2 * q1 * q2 + 2 * q0 * q3, q1 * q1 + q0 * q0 - q3 * q3 - q2 * q2);

            Vector3 RPY = new Vector3 (roll, pitch, yaw);

            return RPY;
        }
    }
}

