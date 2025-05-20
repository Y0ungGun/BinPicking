using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DSRRobotControl
{
    public static class Quat2Euler
    {
        private static int ElementaryBasisIndex(char axis)
        {
            switch (axis)
            {
                case 'x': return 0;
                case 'y': return 1;
                case 'z': return 2;
                default: throw new ArgumentException("Invalid axis");
            }
        }

        private static double[][] ComputeEulerFromQuat(double[][] quat, string seq)
        {
            bool extrinsic = false;
            if (!extrinsic)
            {
                seq = new string(seq.Reverse().ToArray());
            }

            int i = ElementaryBasisIndex(seq[0]);
            int j = ElementaryBasisIndex(seq[1]);
            int k = ElementaryBasisIndex(seq[2]);

            bool isProper = i == k;

            if (isProper)
            {
                k = 3 - i - j; // get third axis
            }

            int sign = ((i - j) * (j - k) * (k - i) / 2) >= 0 ? 1 : -1;

            int numRotations = quat.Length;
            double[][] angles = new double[numRotations][];
            for (int ind = 0; ind < numRotations; ind++)
            {
                angles[ind] = new double[3];
            }

            double eps = 1e-7;

            for (int ind = 0; ind < numRotations; ind++)
            {
                double[] _angles = angles[ind];

                double a, b, c, d;
                if (isProper)
                {
                    a = quat[ind][3];
                    b = quat[ind][i];
                    c = quat[ind][j];
                    d = quat[ind][k] * sign;
                }
                else
                {
                    a = quat[ind][3] - quat[ind][j];
                    b = quat[ind][i] + quat[ind][k] * sign;
                    c = quat[ind][j] + quat[ind][3];
                    d = quat[ind][k] * sign - quat[ind][i];
                }

                double n2 = a * a + b * b + c * c + d * d;

                _angles[1] = Math.Acos(2 * (a * a + b * b) / n2 - 1);

                bool safe1 = Math.Abs(_angles[1]) >= eps;
                bool safe2 = Math.Abs(_angles[1] - Math.PI) >= eps;
                bool safe = safe1 && safe2;

                if (safe)
                {
                    double halfSum = Math.Atan2(b, a);
                    double halfDiff = Math.Atan2(-d, c);

                    _angles[0] = halfSum + halfDiff;
                    _angles[2] = halfSum - halfDiff;
                }
                else
                {
                    if (!extrinsic)
                    {
                        if (!safe)
                        {
                            _angles[0] = 0;
                        }
                        if (!safe1)
                        {
                            double halfSum = Math.Atan2(b, a);
                            _angles[2] = 2 * halfSum;
                        }
                        if (!safe2)
                        {
                            double halfDiff = Math.Atan2(-d, c);
                            _angles[2] = -2 * halfDiff;
                        }
                    }
                    else
                    {
                        if (!safe)
                        {
                            _angles[2] = 0;
                        }
                        if (!safe1)
                        {
                            double halfSum = Math.Atan2(b, a);
                            _angles[0] = 2 * halfSum;
                        }
                        if (!safe2)
                        {
                            double halfDiff = Math.Atan2(-d, c);
                            _angles[0] = 2 * halfDiff;
                        }
                    }
                }

                for (int i_ = 0; i_ < 3; i_++)
                {
                    if (_angles[i_] < -Math.PI)
                    {
                        _angles[i_] += 2 * Math.PI;
                    }
                    else if (_angles[i_] > Math.PI)
                    {
                        _angles[i_] -= 2 * Math.PI;
                    }
                }

                if (!isProper)
                {
                    _angles[2] *= sign;
                    _angles[1] -= Math.PI / 2;
                }

                if (!extrinsic)
                {
                    double temp = _angles[0];
                    _angles[0] = _angles[2];
                    _angles[2] = temp;
                }

                if (!safe)
                {
                    Console.WriteLine("Gimbal lock detected. Setting third angle to zero since it is not possible to uniquely determine all angles.");
                }
            }   
            return angles;
        }

        public static Vector3 EulerFromQuat(Quaternion r, string seq, bool degrees = true)
        {
            if (seq.Length != 3)
            {
                throw new ArgumentException($"Expected 3 axes, got {seq.Length}.");
            }

            bool intrinsic = seq.All(c => char.IsUpper(c));
            bool extrinsic = seq.All(c => char.IsLower(c));
            if (!intrinsic && !extrinsic)
            {
                throw new ArgumentException($"Expected axes from seq to be from ['x', 'y', 'z'] or ['X', 'Y', 'Z'], got {seq}.");
            }

            if (seq[0] == seq[1] || seq[1] == seq[2])
            {
                throw new ArgumentException($"Expected consecutive axes to be different, got {seq}.");
            }

            seq = seq.ToLower();

            double[][] quat = { new double[] { r.x, r.y, r.z, r.w } };

            double[][] angles = ComputeEulerFromQuat(quat, seq);

            if (degrees)
            {
                for (int i = 0; i < angles.Length; i++)
                {
                    for (int j = 0; j < angles[i].Length; j++)
                    {
                        angles[i][j] = angles[i][j] * 180 / Mathf.PI;
                    }
                }
            }
            Vector3 result = new Vector3() { x = (float)angles[0][0], y = (float)angles[0][1], z = (float)angles[0][2] };
            return result;
        }
    }
}

