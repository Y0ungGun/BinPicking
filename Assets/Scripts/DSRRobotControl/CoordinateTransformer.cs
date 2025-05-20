using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MathNet.Numerics.LinearAlgebra.Double;
using Vector = MathNet.Numerics.LinearAlgebra.Vector<double>;


// Do not Use. Do not work properly.
namespace DSRRobotControl
{
    public static class CoordinateTransformer
    {
        public static List<double> Transformer(Transform transform)
        {
            Vector3 position = transform.localToWorldMatrix.GetColumn(3);

            Quaternion worldQuaternion = transform.rotation;
            Quaternion transformedQuat = new Quaternion { w = -worldQuaternion.w, x = -worldQuaternion.x, y = -worldQuaternion.z, z = worldQuaternion.y };
            Vector3 zyx = Quat2RPY.Quat2rpy(transformedQuat);

            List<double> result = new List<double>{
                -position.x,
                -position.z,
                position.y,
                zyx[0],
                zyx[1],
                zyx[2]
            };
            Debug.Log("START POSE=" + string.Join(",", result));
            return result;
        }
    }
}

