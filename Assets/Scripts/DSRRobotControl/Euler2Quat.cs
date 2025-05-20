using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MathNet.Numerics.LinearAlgebra.Double;
using Vector = MathNet.Numerics.LinearAlgebra.Vector<double>;

namespace DSRRobotControl
{
    public static class Euler2Quat
    {
        public static double[] Transformer(Transform transform)
        {
            double[] result = new double[6];

            Vector3 position = transform.localToWorldMatrix.GetColumn(3);

            Quaternion worldQuaternion = transform.rotation;
            Quaternion transformedQuat = new Quaternion { w = worldQuaternion.w, x = -worldQuaternion.x, y = -worldQuaternion.z, z = -worldQuaternion.y };
            Vector3 zyx = Quat2RPY.Quat2rpy(transformedQuat);
            

            result[0] = position.x * 1000;
            result[1] = position.z * 1000;
            result[2] = position.y * 1000;
            result[3] = zyx[0];
            result[4] = zyx[1];
            result[5] = zyx[2];


            return result;
        }

        public static Quaternion ZYX2Quat(Vector3 ZYX)
        {
            Quaternion qZ = Quaternion.AngleAxis(ZYX.z, Vector3.forward);
            Quaternion qY = Quaternion.AngleAxis(ZYX.y, Vector3.up);
            Quaternion qX = Quaternion.AngleAxis(ZYX.x, Vector3.right);

            Quaternion q = qZ * qY * qX;

            return q;
        }

        public static Quaternion ZYZ2Quat(Vector3 ZYZ)
        {
            Quaternion qZ1 = Quaternion.AngleAxis(ZYZ.x * 180/Mathf.PI, Vector3.forward); 
            Quaternion qY = Quaternion.AngleAxis(ZYZ.y * 180/Mathf.PI, Vector3.up);         
            Quaternion qZ2 = Quaternion.AngleAxis(ZYZ.z * 180 / Mathf.PI, Vector3.forward);  

            Quaternion q = qZ1 * qY * qZ2;

            return q;
        }
        public static Quaternion ZXY2Quat(Vector3 ZXY)
        {
            Quaternion qZ = Quaternion.AngleAxis(ZXY.z, Vector3.forward); 
            Quaternion qX = Quaternion.AngleAxis(ZXY.x, Vector3.right);  
            Quaternion qY = Quaternion.AngleAxis(ZXY.y, Vector3.up);      
            
            Quaternion q = qZ * qX * qY;

            return q;
        }
    }
}
