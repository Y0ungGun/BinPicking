using MIConvexHull;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using GripperGWS;
using MathNet.Numerics.LinearAlgebra;

namespace GripperGWS
{
    public class WrenchConvexHull : MonoBehaviour
    {
        private WrenchCollector wrenchCollector;
        private TargetContact targetContact;

        private void Start()
        {
            wrenchCollector = WrenchCollector.Instance;

        }


        /// <summary>
        /// 외부에서 호출하여 GWS(Epsilon) 계산 및 시각화까지 수행하는 메서드
        /// </summary>
        /// <returns>계산된 epsilon 값</returns>
        public float GetEpsilon()
        {
            // targetContact를 새롭게 찾음
            if (targetContact == null || !targetContact.isContact) return 0f;
            // TargetContact에서 WrenchCollector에 force/moment를 갱신
            targetContact.GetWrenches();

            // WrenchCollector에서 force/moment 리스트를 받아옴
            List<Vector3> forces = wrenchCollector.GetAllForces();
            List<Vector3> moments = wrenchCollector.GetAllMoments();
            if (forces == null || moments == null || forces.Count == 0 || moments.Count == 0) return 0f;
            int count = Mathf.Min(forces.Count, moments.Count);
            if (count == 0) return 0f;

            // Wrench 리스트 생성
            List<Wrench> wrenches = new List<Wrench>();
            for (int i = 0; i < count; i++)
            {
                wrenches.Add(new Wrench(forces[i], moments[i]));
            }
            wrenches = wrenches
                .Select(w => new Wrench(
                    new Vector3(
                        (float)Math.Round(w.Position[0], 6),
                        (float)Math.Round(w.Position[1], 6),
                        (float)Math.Round(w.Position[2], 6)),
                    new Vector3(
                        (float)Math.Round(w.Position[3], 6),
                        (float)Math.Round(w.Position[4], 6),
                        (float)Math.Round(w.Position[5], 6))))
                .OrderBy(w => string.Join("_", w.Position.Select(x => x.ToString("G6"))))
                .ToList();

            // Epsilon 계산
            var convexHull = ConvexHull.Create(wrenches);
            float epsilon = 0f;
            if (convexHull.ErrorMessage == "")
            {
                double eps = CalculateEpsilon(convexHull);
                epsilon = (float)eps * 1000f;
                Debug.Log($"Epsilon, Radius:{eps}, {epsilon}");
            }

            return epsilon;
        }

        private static double CalculateEpsilon(ConvexHullCreationResult<Wrench, DefaultConvexFace<Wrench>> hullResult)
        {
            double epsilon = double.MaxValue;

            // Convex Hull의 각 face에 대해 Chebyshev 반지름 계산
            foreach (var face in hullResult.Result.Faces)
            {
                var normal = CalculateNormal(face.Vertices.Select(v => v.Position).ToList());
                var samplePoint = face.Vertices.First().Position;
                double b = VectorDot(normal, samplePoint);

                double distance = Math.Abs(b / VectorNorm(normal));

                epsilon = Math.Min(epsilon, distance);
            }

            return epsilon;
        }


        private static double[] CalculateNormal(List<double[]> vertices)
        {
            var mat = Matrix<double>.Build.Dense(vertices.Count, 6);
            for (int i = 0; i < vertices.Count; i++)
            {
                for (int j = 0; j < 6; j++)
                {
                    mat[i, j] = vertices[i][j];
                }
            }

            var origin = mat.Row(0);
            for (int i = 0; i < mat.RowCount; i++)
                mat.SetRow(i, mat.Row(i) - origin);

            var svd = mat.Svd(true);
            Vector<double> normal = svd.VT.Row(svd.VT.RowCount - 1);

            return normal.ToArray();
        }

        private static double VectorNorm(double[] vector)
        {
            return Math.Sqrt(vector.Select(x => x * x).Sum());
        }

        private static double VectorDot(double[] v1, double[] v2)
        {
            return v1.Zip(v2, (x, y) => x * y).Sum();
        }

        public void SetTargetContact(GameObject target)
        {
            targetContact = target.GetComponent<TargetContact>();
        }
    }
}
