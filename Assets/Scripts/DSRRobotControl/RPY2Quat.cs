using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DSRRobotControl
{
    public static class RPY2Quat
    {
        // rpy should be Rad.
        public static Quaternion rpy2Quat(Vector3 rpy)
        {
            float phi = rpy[0] / 2;
            float theta = rpy[1] / 2;
            float psi = rpy[2] / 2;

            float cphi = Mathf.Cos(phi);
            float sphi = Mathf.Sin(phi);
            float ctheta = Mathf.Cos(theta);
            float stheta = Mathf.Sin(theta);
            float cpsi = Mathf.Cos(psi);
            float spsi = Mathf.Sin(psi);

            float q0 = cphi * ctheta * cpsi + sphi * stheta * spsi;
            float q1 = sphi * ctheta * cpsi - cphi * stheta * spsi;
            float q2 = cphi * stheta * cpsi + sphi * ctheta * spsi;
            float q3 = cphi * ctheta * spsi - sphi * stheta * cpsi;

            if (q0 < 0)
            {
                q0 = -q0;
                q1 = -q1;
                q2 = -q2;
                q3 = -q3;
            }

            Quaternion quat = new Quaternion(q1, q2, q3, q0);

            return quat;
        }
    }
}

