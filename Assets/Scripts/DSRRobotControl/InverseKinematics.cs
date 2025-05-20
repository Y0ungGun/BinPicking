using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net.NetworkInformation;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.Integration;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.LinearAlgebra.Factorization;
using System.Diagnostics;
using Matrix = MathNet.Numerics.LinearAlgebra.Matrix<double>;
using Vector = MathNet.Numerics.LinearAlgebra.Vector<double>;
//using System.Reflection.Metadata;
using System.Numerics;
using System.ComponentModel.Design;
using System.Runtime.ExceptionServices;
using MathNet.Numerics.LinearAlgebra.Storage;
using System.Runtime.InteropServices;
using System.Xml;
using UnityEngine;
namespace DSRRobotControl
{
    public class InverseKinematics
    {
        // Input: m, rad(ZYZ)
        // Output: rad(J6)
        // Get_Sol_space Input: rad(J6)
        double pi = Math.PI;
        public Vector final_solution_rad;
        public double joint1_z_off_set = 152.5 * 0.001;
        public double L1 = 34.5 * 0.001; //joint1->joint2 길이
        public double L2 = 411.0 * 0.001;//joint2->joint3 길이
        public double L3 = 368.0 * 0.001;//joint3->joint4 길이
        public double ee2p_sph_offset = 121.0 * 0.001;
        public double _90d_in_rad = 90.0 / 180 * Math.PI;
        public double _180d_in_rad = 180.0 / 180 * Math.PI;
        public double _270d_in_rad = 270.0 / 180 * Math.PI;
        public double _360d_in_rad = 360.0 / 180 * Math.PI;
        public double _450d_in_rad = 450.0 / 180 * Math.PI;

        private double debug;

        public Matrix allsol;
        public InverseKinematics()
        {
        }
        public InverseKinematics(Vector xyzabc, int sol)
        {
            this.final_solution_rad = this.IK_Doosan(xyzabc, sol);
        }
        public List<double> InverseKinematicsResult()
        {
            List<double> result = new List<double>();
            result = new List<double> { final_solution_rad[0], final_solution_rad[1], final_solution_rad[2], final_solution_rad[3], final_solution_rad[4], final_solution_rad[5] };

            return result;
        }
        public Matrix DH_Mat_rad(double theta, double r, double l, double alpha)
        {
            var x = DenseMatrix.OfArray(new double[,]
            {
            {cos(theta), -sin(theta)*cos(alpha), sin(theta)*sin(alpha), l*cos(theta) },
            { sin(theta), cos(theta) * cos(alpha), - cos(theta) * sin(alpha), l* sin(theta) },
            { 0, sin(alpha), cos(alpha), r },
            { 0, 0, 0, 1 }
                });
            return x;
        }
        public Vector IK_Doosan(Vector xyzabc, int sol)
        {
            Vector q_set = DenseVector.OfArray(new double[] { });
            var x = xyzabc[0];
            var y = xyzabc[1];
            var z = xyzabc[2] - joint1_z_off_set;
            var a = xyzabc[3];
            var b = xyzabc[4];
            var c = xyzabc[5];
            var eul = DenseVector.OfArray(new double[] { (a), (b), (c) });
            var pos = DenseVector.OfArray(new double[] { x, y, z });
            var T = matrix3x3to4x4(eul2rotm(eul, "ZYZ"), pos);

            var p_sph = T * DenseVector.OfArray(new double[] { 0, 0, -ee2p_sph_offset, 1 }); // position of spherical joint
            var B = sqrt(Math.Pow(p_sph[0], 2) + Math.Pow(p_sph[1], 2));
            var beta = atan(p_sph[0] / p_sph[1]);
            var tmp1 = acos(L1 / B);

            var q11r = tmp1 - beta;
            var q12r = -tmp1 - beta;
            var q13r = tmp1 - beta + pi;
            var q14r = -tmp1 - beta + pi;

            var q1r = DenseVector.OfArray(new double[] { q11r, q12r, q13r, q14r });

            Vector v1_l = DenseVector.OfArray(new double[] { });
            Vector v1_r = DenseVector.OfArray(new double[] { });
            for (int i = 0; i < q1r.Count; i++)
            {
                double _tmp = p_sph[0] / Math.Cos(q1r[i]) + L1 * Math.Tan(q1r[i]);
                v1_l = Vector.Build.DenseOfEnumerable(v1_l.Concat(new[] { _tmp }));
                // v1_l = p_sph(1)./cos(q1r)+31/5.*tan(q1r);
                _tmp = p_sph[1] / Math.Sin(q1r[i]) - L1 / Math.Tan(q1r[i]);
                v1_r = Vector.Build.DenseOfEnumerable(v1_r.Concat(new[] { _tmp }));
                // v1_r = p_sph(2)./ sin(q1r) - 31 / 5./ tan(q1r);
            }
            Vector q1rn = DenseVector.OfArray(new double[] { });
            for (int ii = 0; ii < 4; ii++)
            {
                double tolerance = 0.000001; 
                if (Math.Abs(v1_l[ii] - v1_r[ii]) < tolerance * Math.Max(Math.Abs(v1_l[ii]), Math.Abs(v1_r[ii])))
                {
                    q1rn = Vector.Build.DenseOfEnumerable(q1rn.Concat(new[] { q1r[ii] }));
                    //q1rn.Append(v1_l[ii]);
                }
            }
            q1rn = DenseVector.OfArray(new double[] { q1rn[0], q1rn[0], q1rn[0], q1rn[0], q1rn[1], q1rn[1], q1rn[1], q1rn[1] });
            Vector q1d = q1rn * 180 / pi;
            // Get q2
            Vector D = zeros(2); //zeros(2, 1);
            Vector E = zeros(2); //zeros(2, 1);
            Vector beta2 = zeros(8); //zeros(8, 1);
            Vector q2rn = zeros(8); //zeros(8, 1);

            for (int ii = 0; ii < 2; ii++)
            {
                D[ii] = p_sph[0] / cos(q1rn[4 * ii]) + L1 * tan(q1rn[4 * ii]);
                E[ii] = (Math.Pow(L3, 2) - Math.Pow(L2, 2) - Math.Pow(D[ii], 2) - Math.Pow(p_sph[2], 2)) / (2 * L2);
                beta2[2 * ii] = atan(p_sph[2] / D[ii]);
                beta2[2 * ii + 1] = beta2[2 * ii] + pi;

                var tmp2 = E[ii] / sqrt(Math.Pow(D[ii], 2) + Math.Pow(p_sph[2], 2));

                if (Math.Abs(tmp2) - 1 < 0.001 && Math.Abs(tmp2) - 1 > 0)
                {
                    if (tmp2 > 0)
                    { tmp2 = acos(1); }
                    else
                    { tmp2 = acos(-1); }
                }
                else
                {
                    tmp2 = acos(E[ii] / sqrt(Math.Pow(D[ii], 2) + Math.Pow(p_sph[2], 2)));
                }

                debug = E[ii] / sqrt(Math.Pow(D[ii], 2) + Math.Pow(p_sph[2], 2));
                q2rn[4 * ii] = tmp2 - beta2[2 * ii];
                q2rn[4 * ii + 1] = tmp2 - beta2[2 * ii + 1];
                q2rn[4 * ii + 2] = -tmp2 - beta2[2 * ii];
                q2rn[4 * ii + 3] = -tmp2 - beta2[2 * ii + 1];
            }
            Vector s23 = zeros(8); //zeros(8, 1);
            Vector c23 = zeros(8);// DenseVector.OfArray(new double[] { 0, 0, 0, 0, 0, 0, 0, 0 }); //zeros(8, 1);

            Vector s23_ans = DenseVector.OfArray(new double[] { }); //[];
            Vector c23_ans = DenseVector.OfArray(new double[] { }); //[];
            Vector q2rn_ans = DenseVector.OfArray(new double[] { });//[];
            Vector q1rn_ans = DenseVector.OfArray(new double[] { });//[];

            for (int ii = 0; ii < 8; ii++)
            {
                s23[ii] = (p_sph[0] / cos(q1rn[ii]) + L1 * tan(q1rn[ii]) - L2 * cos(q2rn[ii])) / L3;
                c23[ii] = (p_sph[2] + L2 * sin(q2rn[ii])) / L3;

                var test = Math.Abs(Math.Pow(s23[ii], 2) + Math.Pow(c23[ii], 2) - 1);
                if (Math.Abs(Math.Pow(s23[ii], 2) + Math.Pow(c23[ii], 2) - 1) < 0.001)
                {
                    q2rn_ans = Vec_Append(q2rn_ans, q2rn[ii]); //= [q2rn_ans; q2rn(ii)];
                    s23_ans = Vec_Append(s23_ans, s23[ii]); //= [s23_ans; s23(ii)];
                    c23_ans = Vec_Append(c23_ans, c23[ii]); //= [c23_ans; c23(ii)];
                    q1rn_ans = Vec_Append(q1rn_ans, q1rn[ii]); //= [q1rn_ans; q1rn(ii)];
                }

            }
            Vector q2d = (q2rn_ans + (pi / 2)) * 180 / pi;

            var t23_ans = zeros(0); // t23_ans = s23_ans./ c23_ans;
            for (int ii = 0; ii < s23_ans.Count; ii++)
            {
                t23_ans = Vec_Append(t23_ans, s23_ans[ii] / c23_ans[ii]);
            }

            Vector q3rn1 = DenseVector.OfArray(new double[] { }); // q3rn1 = atan(t23_ans) - q2rn_ans;
            for (int ii = 0; ii < s23_ans.Count; ii++)
            {
                q3rn1 = Vec_Append(q3rn1, atan(t23_ans[ii]) - q2rn_ans[ii]);
            }

            Vector q3rn2 = q3rn1 + pi;

            q1rn_ans = Vec_Expend(q1rn_ans, q1rn_ans);//q1rn_ans = [q1rn_ans; q1rn_ans];
            q2rn_ans = Vec_Expend(q2rn_ans, q2rn_ans);//q2rn_ans = [q2rn_ans; q2rn_ans];
            Vector q3rn = DenseVector.OfArray(new double[] { }); //q3rn = [q3rn1;q3rn2];
            q3rn = Vec_Expend(q3rn, q3rn1);
            q3rn = Vec_Expend(q3rn, q3rn2);



            Vector v3l = DenseVector.OfArray(new double[] { });
            for (int ii = 0; ii < q2rn_ans.Count; ii++)
            {
                v3l = Vec_Append(v3l, L3 * cos(q2rn_ans[ii] + q3rn[ii]));
            }

            Vector v3r = DenseVector.OfArray(new double[] { });
            for (int ii = 0; ii < q2rn_ans.Count; ii++)
            {
                v3r = Vec_Append(v3r, p_sph[2] + L2 * sin(q2rn_ans[ii]));

            }
            Vector q1n_ans2 = zeros(0); //DenseVector.OfArray(new double[] { });// = [];
            Vector q2n_ans2 = zeros(0); //DenseVector.OfArray(new double[] { });// = [];
            Vector q3n_ans2 = zeros(0); //DenseVector.OfArray(new double[] { });// = [];

            for (int ii = 0; ii < q1rn_ans.Count; ii++)
            {
                double tolerance = 0.000001;  // 절대적인 임계값
                if (Math.Abs(v3l[ii] - v3r[ii]) < tolerance * Math.Max(Math.Abs(v3l[ii]), Math.Abs(v3r[ii])))
                {
                    q1n_ans2 = Vec_Append(q1n_ans2, q1rn_ans[ii]); //= [q1n_ans2; q1rn_ans(ii)];
                    q2n_ans2 = Vec_Append(q2n_ans2, q2rn_ans[ii]);//= [q2n_ans2; q2rn_ans(ii)];
                    q3n_ans2 = Vec_Append(q3n_ans2, q3rn[ii]); //= [q3n_ans2; q3rn(ii)];
                }
            }
            Vector q2r = q2n_ans2 + pi / 2;
            Vector q3r = q3n_ans2 - pi / 2;

            q1d = q1n_ans2 * 180 / pi;
            q2d = q2r * 180 / pi;
            Vector q3d = q3r * 180 / pi;


            Matrix q123r = zeros(4, 3);//rem([q1d q2d q3d], 360);
            q123r.SetColumn(0, q1n_ans2);
            q123r.SetColumn(1, q2r);
            q123r.SetColumn(2, q3r);
            q123r = q123r.Remainder(_360d_in_rad);

            // q123d(q123d > 180) = q123d(q123d > 180)-360;
            //q123d(q123d < -180) = q123d(q123d < -180) + 360;

            for (int i = 0; i < q123r.RowCount; i++)
            {

                for (int j = 0; j < q123r.ColumnCount; j++)
                {
                    if (q123r[i, j] > _180d_in_rad)
                    {
                        q123r[i, j] = q123r[i, j] - _360d_in_rad;
                    }
                    else if (q123r[i, j] < -_180d_in_rad)
                    {
                        q123r[i, j] = q123r[i, j] + _360d_in_rad;
                    }
                }
            }
            // transpose하는 파트 차원 주의해서 코드 짤 것
            q123r = q123r.Transpose();
            var q123456_ans = zeros(6, 8);

            for (int ii = 0; ii < 4; ii++)//ii = 1:4
            {
                var q1 = q123r[0, ii];
                var q2 = q123r[1, ii];
                var q3 = q123r[2, ii];

                Matrix T1 = DH_Mat_rad(q1, 0.0, 0.0, -_90d_in_rad);
                Matrix T2 = DH_Mat_rad(q2 - _90d_in_rad, L1, L2, 0);
                Matrix T3 = DH_Mat_rad(q3 + _90d_in_rad, 0, 0, _90d_in_rad);

                Matrix U = T3.Inverse() * T2.Inverse() * T1.Inverse() * T;//inv(T3) * inv(T2) * inv(T1) * T;
                var q5_ans1 = 0.0;
                for (int jj = 0; jj < 2; jj++) //jj = 1:2
                {

                    if (jj == 0)
                    {
                        q5_ans1 = acos(U[2, 2]);
                    }
                    else
                    {
                        q5_ans1 = -1 * acos(U[2, 2]);
                    }

                    // dertimine q4
                    var q4_ans11 = asin(U[1, 2] / sin(q5_ans1));
                    var q4_ans12 = pi - q4_ans11;
                    var q4_ans1 = 0.0;
                    if (abs(U[0, 3] - (ee2p_sph_offset * cos(q4_ans11) * sin(q5_ans1))) < 0.0001)
                    { q4_ans1 = q4_ans11; }
                    else
                    {
                        q4_ans1 = q4_ans12;
                    }


                    // determine q6
                    var q6_ans11 = asin(U[2, 1] / sin(q5_ans1));
                    var q6_ans12 = pi - q6_ans11;
                    var q6_ans1 = 0.0;
                    if (abs(U[2, 0] - (-cos(q6_ans11) * sin(q5_ans1))) < 0.0001)
                    { q6_ans1 = q6_ans11; }
                    else
                    {
                        q6_ans1 = q6_ans12;
                    }


                    q123456_ans[0, (ii) * 2 + jj] = q1;
                    q123456_ans[1, (ii) * 2 + jj] = q2;
                    q123456_ans[2, (ii) * 2 + jj] = q3;
                    q123456_ans[3, (ii) * 2 + jj] = q4_ans1;
                    q123456_ans[4, (ii) * 2 + jj] = q5_ans1;
                    q123456_ans[5, (ii) * 2 + jj] = q6_ans1;
                }
                q123456_ans = q123456_ans.Remainder(_360d_in_rad);
                for (int i = 0; i < q123456_ans.RowCount; i++)
                {

                    for (int j = 0; j < q123456_ans.ColumnCount; j++)
                    {
                        if (q123456_ans[i, j] > _180d_in_rad)
                        {
                            q123456_ans[i, j] = q123456_ans[i, j] - _360d_in_rad;
                        }
                        else if (q123456_ans[i, j] < -_180d_in_rad)
                        {
                            q123456_ans[i, j] = q123456_ans[i, j] + _360d_in_rad;
                        }
                    }
                }
            }
            allsol = q123456_ans;
            q_set = find_q_set(q123456_ans, sol);
            //sol_bit = bitget(sol, 3:-1:1);

            return q_set;
            //ik_doosan.m 함수 끝    
        }
        public Vector find_q_set(Matrix solutions, int solspace)
        {
            List<int> sol = getAllSolutionSpace(solutions);

            int idEqualsol = sol.IndexOf(solspace);

            return solutions.Column(idEqualsol);
        }
        public Matrix getAllSolutions()
        {
            return allsol;
        }
        public List<int> getAllSpace()
        {
            return getAllSolutionSpace(allsol);
        }
        public int getSingleSolutionSpace(Vector JArray)
        {
            // 상대경로로 수정

            string urdfPath = Path.Combine(Application.dataPath, "Scripts/DSRRobotControl/urdf/m1509.urdf");
            ForwardKinematics FK = new ForwardKinematics(urdfPath);
            for (int i = 0; i < 6; i++)
            {
                FK.JointAngles[i] = JArray[i];
            }
            FK.ForwardKinematicsSolver();
            List<double> currentP = FK.ForwardKinematicsResult();
            currentP[3] = FK.Joints.Last().ZYZ.Z1; currentP[4] = FK.Joints.Last().ZYZ.Y2; currentP[5] = FK.Joints.Last().ZYZ.Z3;

            Vector input = DenseVector.OfArray(new double[] { currentP[0], currentP[1], currentP[2], currentP[3], currentP[4], currentP[5] });
            InverseKinematics IK = new InverseKinematics(input, 0);
            Matrix allsolutions = IK.getAllSolutions();
            List<int> allspaces = IK.getAllSpace();

            List<double> norms = new List<double>();
            for (int i = 0; i < allsolutions.ColumnCount; i++)
            {
                norms.Add(Cal_L2Norm(allsolutions.Column(i), JArray));
            }

            return allspaces[norms.IndexOf(norms.Min())];
        }
        public List<int> getAllSolutionSpace(Matrix solutions)
        {
            // sol1, sol3 판단 부분
            List<int> solspaces = new List<int>{ 0, 0, 0, 0, 0, 0, 0, 0 };

            List<int> Lefty = new List<int>();
            List<int> Righty = new List<int>();
            for (int i = 0; i < solutions.ColumnCount; i++)
            {
                Vector q123456_ans = solutions.Column(i);
                var q11 = q123456_ans[0];
                var q22 = q123456_ans[1];
                var q33 = q123456_ans[2];
                // Joint 1
                double r1 = 0, p1 = 0, y1 = 0;    // 회전 (rpy)
                double x1 = 0, y1_pos = 0, z1 = 0;  // 위치

                // Joint 2
                double r2 = 0, p2 = -Math.PI / 2, y2 = -Math.PI / 2;
                double x2 = 0, y2_pos = L1, z2 = 0;

                // Joint 3
                double r3 = 0, p3 = 0, y3 = Math.PI / 2;
                double x3 = L2, y3_pos = 0, z3 = 0;
                // Joint 4
                double r4 = pi / 2, p4 = 0, y4 = 0;
                double x4 = 0, y4_pos = -L3, z4 = 0;

                Matrix T1 = rpy_to_matrix(r1, p1, y1, x1, y1_pos, z1);
                Matrix T2 = rpy_to_matrix(r2, p2, y2, x2, y2_pos, z2);
                Matrix T3 = rpy_to_matrix(r3, p3, y3, x3, y3_pos, z3);
                Matrix T4 = rpy_to_matrix(r4, p4, y4, x4, y4_pos, z4);
                Matrix rot1 = RotationMatrix4x4("Z", q11);
                Matrix rot2 = RotationMatrix4x4("Z", q22);
                Matrix rot3 = RotationMatrix4x4("Z", q33);
                Matrix T1234 = T1 * rot1 * T2 * rot2 * T3 * rot3 * T4;
                Vector p_sph = DenseVector.OfArray(new double[] { T1234[0, 3], T1234[1, 3], T1234[2, 3] });

                var sph_ang = atan2(p_sph[1], p_sph[0]) - _90d_in_rad;
                var diff = q11 - sph_ang;
                if (diff > _180d_in_rad)
                {
                    diff = diff - _360d_in_rad;
                }
                if (diff > 0)
                {
                    solspaces[i] += 0;
                    Lefty.Add(i);
                }
                else
                {
                    solspaces[i] += 4;
                    Righty.Add(i);
                }

                if (q123456_ans[4] >= 0)
                { solspaces[i] += 0; }
                else
                { solspaces[i] += 1; }
            }

            // sol2 판단 부분

            List<Tuple<int, double>> CosLefty = new List<Tuple<int, double>>();
            foreach (int id in Lefty)
            {
                Vector q123456 = solutions.Column(id);
                var q22 = q123456[1];
                double cosq22 = Math.Cos(q22);
                CosLefty.Add(Tuple.Create(id, cosq22));
            }
            CosLefty.Sort((a,b) => a.Item2.CompareTo(b.Item2));
            solspaces[CosLefty[0].Item1] += 0;
            solspaces[CosLefty[1].Item1] += 0;
            solspaces[CosLefty[2].Item1] += 2;
            solspaces[CosLefty[3].Item1] += 2;

            List<Tuple<int, double>> CosRighty = new List<Tuple<int, double>>();
            foreach (int id in Righty)
            {
                Vector q123456 = solutions.Column(id);
                var q22 = q123456[1];
                double cosq22 = Math.Cos(q22);
                CosRighty.Add(Tuple.Create(id, cosq22));
            }
            CosRighty.Sort((a, b) => a.Item2.CompareTo(b.Item2));
            solspaces[CosRighty[0].Item1] += 0;
            solspaces[CosRighty[1].Item1] += 0;
            solspaces[CosRighty[2].Item1] += 2;
            solspaces[CosRighty[3].Item1] += 2;

            return solspaces;
        }
        public Matrix rpy_to_matrix(double r, double p, double y, double x, double y_pos, double z)
        {
            Matrix Rz = RotationMatrix4x4("Z", y);
            Matrix Ry = RotationMatrix4x4("Y", p);
            Matrix Rx = RotationMatrix4x4("X", r);


            Matrix R = Rz * Ry * Rx;
            Matrix pos = DenseMatrix.OfArray(new double[,]{
                    { 0, 0, 0, x},
                    { 0, 0, 0, y_pos},
                    { 0, 0, 0, z},
                    { 0, 0, 0, 0}
                    });

            Matrix T = R + pos;
            return T;
        }
        private Matrix RotationMatrix4x4(string axiz, double AngleInRadian)
        {
            Matrix matrix2return = DenseMatrix.OfArray(new double[,] { });
            switch (axiz)
            {
                case "X":
                    matrix2return = DenseMatrix.OfArray(new double[,] { {1.0, 0.0, 0.0, 0.0 },
                { 0, (float)Math.Cos(AngleInRadian), (float)-Math.Sin(AngleInRadian), 0 },
                { 0, (float)Math.Sin(AngleInRadian), (float)Math.Cos(AngleInRadian), 0 },
                { 0, 0, 0, 1 } });
                    break;
                case "Y":
                    matrix2return = DenseMatrix.OfArray(new double[,] {
                { (float)Math.Cos(AngleInRadian), 0, (float)Math.Sin(AngleInRadian), 0 },
                { 0, 1, 0, 0 },
                { (float)-Math.Sin(AngleInRadian), 0, (float)Math.Cos(AngleInRadian), 0 },
                { 0, 0, 0, 1 }});
                    break;
                case "Z":
                    matrix2return = DenseMatrix.OfArray(new double[,] {
                { (float)Math.Cos(AngleInRadian), (float)-Math.Sin(AngleInRadian), 0, 0 },
                { (float)Math.Sin(AngleInRadian), (float)Math.Cos(AngleInRadian), 0, 0 },
                { 0, 0, 1, 0 },
               { 0, 0, 0, 1 }});
                    break;
                case "x":
                    RotationMatrix4x4("X", AngleInRadian);
                    break;
                case "y":
                    RotationMatrix4x4("Y", AngleInRadian);
                    break;
                case "z":
                    RotationMatrix4x4("Z", AngleInRadian);
                    break;

            }
            return matrix2return;
        }

        private double Cal_L2Norm(Vector sol, Vector qnear)
        {
            double norm;
            List<double> diff = new List<double> { 0, 0, 0, 0, 0, 0 };

            diff[0] = sol[0] - qnear[0];
            diff[1] = sol[1] - qnear[1];
            diff[2] = sol[2] - qnear[2];
            diff[3] = sol[3] - qnear[3];
            diff[4] = sol[4] - qnear[4];
            diff[5] = sol[5] - qnear[5];

            for (int i = 0; i < 6; i++)
            {
                if (diff[i] > MathF.PI)
                {
                    diff[i] = 2 * MathF.PI - diff[i];
                }
            }

            norm = 0;
            for (int i = 0; i < 6; i++)
            {
                norm += diff[i] * diff[i];
            }

            return Math.Sqrt(norm);
        }
        public double abs(double x)
        {
            return Math.Abs(x);

        }


        public Matrix zeros(int x, int y)
        {
            //x=row
            //y=column
            // x = row, y = column
            var _matrix = DenseMatrix.Build.Dense(x, y); // 크기를 지정하여 빈 행렬 생성
            return _matrix;
        }

        public Vector Vec_Append(Vector _vector, double x)
        {

            _vector = Vector.Build.DenseOfEnumerable(_vector.Concat(new[] { x }));
            return _vector;
        }

        public Vector zeros(int x)
        {
            //x=row
            //y=column
            Vector _vector = DenseVector.Build.Dense(x);

            return _vector;
        }
        public double cosd(double x)
        {
            x = x * Math.PI / 180;
            return Math.Cos(x);
        }
        public double sind(double x)
        {
            x = x * Math.PI / 180;
            return Math.Sin(x);
        }
        public double cos(double x)
        {
            return Math.Cos(x);
        }
        public double sin(double x)
        {
            return Math.Sin(x);
        }
        public double sqrt(double x)
        {
            return Math.Sqrt(x);
        }
        public double atan(double x)
        {
            return Math.Atan(x);
        }
        public double acos(double x)
        {
            return Math.Acos(x);
        }
        public double asin(double x)
        {
            return Math.Asin(x);
        }
        public double tan(double x)
        {
            return Math.Tan(x);
        }

        public double atan2(double y, double x)
        {
            return Math.Atan2(y, x);
        }
        public double atan2d(double x, double y)
        {
            var temp = Math.Atan2(x, y);
            return temp * 180 / Math.PI;
        }
        public Vector Vec_Expend(Vector x, Vector y)
        {
            for (int i = 0; i < y.Count; i++)
            {
                x = Vec_Append(x, y[i]);
            }

            return x;
        }
        public double deg2rad(double deg)
        {
            var rad = deg / 180 * Math.PI;
            return rad;
        }
        public double rad2deg(double rad)
        {
            var deg = rad * 180 / Math.PI;
            return deg;
        }
        public Matrix eul2rotm(Vector eul, string sequence)
        {
            var rotm = DenseMatrix.Create(3, 3, 0);
            var s_1 = sin(eul[0]); //theta_z or theta_z1
            var c_1 = cos(eul[0]);
            var s_2 = sin(eul[1]); //theta_y
            var c_2 = cos(eul[1]);
            var s_3 = sin(eul[2]); // theta_x or theta_z2
            var c_3 = cos(eul[2]);
            switch (sequence)
            {
                case "ZYX":
                    //            | c_1 * c_2    c_1* s_2*s_3 - s_1 * c_3    c_1* s_2*c_3 + s_1 * s_3 |
                    // R(Theta) = | s_1 * c_2    s_1* s_2*s_3 + c_1 * c_3    s_1* s_2*c_3 - c_1 * s_3 |
                    //            | -s_2                  c_2* s_3                  c_2* c_3|
                    rotm[0, 0] = c_1 * c_2;
                    rotm[0, 1] = c_1 * s_2 * s_3 - s_1 * c_3;
                    rotm[0, 2] = c_1 * s_2 * c_3 + s_1 * s_3;

                    rotm[1, 0] = s_1 * c_2;
                    rotm[1, 1] = s_1 * s_2 * s_3 + c_1 * c_3;
                    rotm[1, 2] = s_1 * s_2 * c_3 - c_1 * s_3;

                    rotm[2, 0] = -s_2;
                    rotm[2, 1] = c_2 * s_3;
                    rotm[2, 2] = c_2 * c_3;
                    break;
                case "ZYZ":
                    //            | c_1 * c_2 * c_3 - s_1 * s_3 - c_1 * c_2 * s_3 - s_1 * c_3    c_1* s_2|
                    // R(Theta) = | s_1 * c_2 * c_3 + c_1 * s_3 - s_1 * c_2 * s_3 + c_1 * c_3    s_1* s_2|
                    //            | -s_2 * c_3                  s_2* s_3        c_2 |
                    rotm[0, 0] = c_1 * c_2 * c_3 - s_1 * s_3;
                    rotm[0, 1] = -c_1 * c_2 * s_3 - s_1 * c_3;
                    rotm[0, 2] = c_1 * s_2;

                    rotm[1, 0] = s_1 * c_2 * c_3 + c_1 * s_3;
                    rotm[1, 1] = -s_1 * c_2 * s_3 + c_1 * c_3;
                    rotm[1, 2] = s_1 * s_2;

                    rotm[2, 0] = -s_2 * c_3;
                    rotm[2, 1] = s_2 * s_3;
                    rotm[2, 2] = c_2;
                    break;
            }

            return rotm;

        }
        private Matrix matrix3x3to4x4(Matrix _3x3, Vector pos)
        {
            var x = DenseMatrix.OfArray(new double[,]
            {
        {_3x3[0,0], _3x3[0,1], _3x3[0,2],pos[0]}, // 첫 번째 행
        {_3x3[1,0], _3x3[1,1], _3x3[1,2],pos[1]}, // 두 번째 행
        {_3x3[2,0], _3x3[2,1], _3x3[2,2],pos[2]}, // 세 번째 행
        { 0,0,0,1}
            });


            return x;
        }

    }
}


