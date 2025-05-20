using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.IO;
using System.Xml.Linq;
using Matrix4x4 = System.Numerics.Matrix4x4;
using Vector3 = System.Numerics.Vector3;
using System.Linq;

namespace DSRRobotControl
{
    //Input: rad(J6)
    //Output: m, rad(RPY)
    class ForwardKinematics
    {
        public List<Joint> Joints;
        public List<Matrix4x4> T;
        public List<Matrix4x4> Q;
        public int FK_flag = 0;
        public int numofjoint;
        public double[] JointAngles;
        public double[] Changed_JointAngles;



        public ForwardKinematics(string urdfFilePath)
        {

            if (this.FK_flag == 0)
            {
                this.Joints = LoadURDF(urdfFilePath);
            }
            this.T = GetTMatrix(this.Joints, FK_flag);
            this.Q = GetQMatrix(this.T);
            for (int i = 0; i < this.Joints.Count; i++)
            {
                (this.Joints[i].GlobalRoll, this.Joints[i].GlobalPitch, this.Joints[i].GlobalYaw) = RotationMatrixToRPY(this.Q[i]);
                (this.Joints[i].ZYZ.Z1, this.Joints[i].ZYZ.Y2, this.Joints[i].ZYZ.Z3) = RotationMatrixToZYZ(this.Q[i]);
            }
            numofjoint = CountJoint();
            this.JointAngles = new double[numofjoint];
            this.Changed_JointAngles = new double[numofjoint];
        }
        public int CountJoint()
        {
            int x = 0;

            for (int i = 0; i < this.Joints.Count; i++)
                if (Joints[i].Type == "revolute")
                {
                    x++;
                }
            return x;
        }
        public void ForwardKinematicsSolver()
        {
            this.FK_flag = 1;
            var i = 0;
            foreach (Joint joint in this.Joints)
            {
                if (joint.Type == "revolute")
                {
                    joint.Joint_angle = JointAngles[i];
                    i++;
                }
            }
            this.T = GetTMatrix(this.Joints, FK_flag);
            this.Q = GetQMatrix(this.T);
            for (int j = 0; j < this.Joints.Count; j++)
            {
                (this.Joints[j].GlobalRoll, this.Joints[j].GlobalPitch, this.Joints[j].GlobalYaw) = RotationMatrixToRPY(this.Q[j]);
                (this.Joints[j].ZYZ.Z1, this.Joints[j].ZYZ.Y2, this.Joints[j].ZYZ.Z3) = RotationMatrixToZYZ(this.Q[j]);
            }
        }

        public List<double> ForwardKinematicsResult()
        {
            var x = this.Q.Last().M14;
            var y = this.Q.Last().M24;
            var z = this.Q.Last().M34;
            var _r = this.Joints.Last().GlobalRoll;
            var _p = this.Joints.Last().GlobalPitch;
            var _y = this.Joints.Last().GlobalYaw;
            return new List<double> { x, y, z, _r, _p, _y };
        }



        private List<Joint> LoadURDF(string urdfFilePath)
        {
            var joints = new List<Joint>();
            XDocument urdf = XDocument.Load(urdfFilePath);

            foreach (var jointElement in urdf.Descendants("joint"))
            {
                string name = jointElement.Attribute("name").Value;
                string type = jointElement.Attribute("type").Value;
                double _x = 0, _y = 0, _z = 0;
                double _rx = 0, _ry = 0, _rz = 0;
                double _x_axis = 0, _y_axis = 0, _z_axis = 0;
                string rot_axis = "Z";

                var originElement = jointElement.Element("origin");
                if (originElement != null)
                {
                    var xyz = originElement.Attribute("xyz").Value.Split(' ');
                    _x = double.Parse(xyz[0]);
                    _y = double.Parse(xyz[1]);
                    _z = double.Parse(xyz[2]);
                    var rpy = originElement.Attribute("rpy").Value.Split(' ');
                    _rx = double.Parse(rpy[0]);
                    _ry = double.Parse(rpy[1]);
                    _rz = double.Parse(rpy[2]);

                    if (type == "revolute")
                    {
                        var axis = jointElement.Element("axis").Attribute("xyz").Value.Split(' ');
                        _x_axis = double.Parse(axis[0]);
                        _y_axis = double.Parse(axis[1]);
                        _z_axis = double.Parse(axis[2]);
                        if (_x_axis == 1)
                        {
                            rot_axis = "X";
                        }
                        else if (_y_axis == 1)
                        {
                            rot_axis = "Y";
                        }
                        else
                        {
                            rot_axis = "Z";
                        }

                    }

                }

                joints.Add(new Joint
                {
                    Name = name,
                    Type = type,
                    Position = new Vector3((float)_x, (float)_y, (float)_z),
                    Rotation = new Vector3((float)_rx, (float)_ry, (float)_rz),
                    Joint_axis = rot_axis,
                    ZYZ = new EulerZYZ()

                });
            }

            return joints;
        }

        private List<Matrix4x4> GetTMatrix(List<Joint> joints, int flag)
        {
            var matrices = new List<Matrix4x4>();

            foreach (Joint joint in joints)
            {
                var rx = RotationMatrix4x4("X", joint.Rotation.X);
                var ry = RotationMatrix4x4("Y", joint.Rotation.Y);
                var rz = RotationMatrix4x4("Z", joint.Rotation.Z);

                var Joint_rot = RotationMatrix4x4(joint.Joint_axis, 0);

                if (flag == 1)
                {
                    Joint_rot = RotationMatrix4x4(joint.Joint_axis, joint.Joint_angle);

                }
                var pos = new Matrix4x4(
                    0, 0, 0, (float)joint.Position.X,
                    0, 0, 0, (float)joint.Position.Y,
                    0, 0, 0, (float)joint.Position.Z,
                    0, 0, 0, 0
                    );

                var transform = pos + rz * ry * rx * Joint_rot;
                matrices.Add(transform);
            }

            return matrices;
        }

        private List<Matrix4x4> GetQMatrix(List<Matrix4x4> t)
        {
            var matrices = new List<Matrix4x4>();


            for (int i = 0; i < t.Count; i++)
            {
                Matrix4x4 temp = Matrix4x4.Identity;
                for (int j = 0; j <= i; j++)
                {
                    temp *= t[j];
                }
                matrices.Add(temp);
            }
            return matrices;
        }


        private Matrix4x4 RotationMatrix4x4(string axiz, double AngleInRadian)
        {
            Matrix4x4 matrix2return = new Matrix4x4();
            switch (axiz)
            {
                case "X":
                    matrix2return = new Matrix4x4(
                    1, 0, 0, 0,
                    0, (float)Math.Cos(AngleInRadian), (float)-Math.Sin(AngleInRadian), 0,
                    0, (float)Math.Sin(AngleInRadian), (float)Math.Cos(AngleInRadian), 0,
                    0, 0, 0, 1);
                    break;
                case "Y":
                    matrix2return = new Matrix4x4(
                    (float)Math.Cos(AngleInRadian), 0, (float)Math.Sin(AngleInRadian), 0,
                    0, 1, 0, 0,
                    (float)-Math.Sin(AngleInRadian), 0, (float)Math.Cos(AngleInRadian), 0,
                    0, 0, 0, 1);
                    break;
                case "Z":
                    matrix2return = new Matrix4x4(
                    (float)Math.Cos(AngleInRadian), (float)-Math.Sin(AngleInRadian), 0, 0,
                    (float)Math.Sin(AngleInRadian), (float)Math.Cos(AngleInRadian), 0, 0,
                    0, 0, 1, 0,
                    0, 0, 0, 1);
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

        public double[] EE_m_rad()
        {
            var x = this.Q.Last().M14;
            var y = this.Q.Last().M24;
            var z = this.Q.Last().M34;
            var _r = this.Joints.Last().GlobalRoll;
            var _p = this.Joints.Last().GlobalPitch;
            var _y = this.Joints.Last().GlobalYaw;
            return new double[] { x, y, z, _r, _p, _y };
        }

        public static (double roll, double pitch, double yaw) RotationMatrixToRPY(Matrix4x4 matrix)
        {
            double sy = Math.Sqrt(matrix.M11 * matrix.M11 + matrix.M21 * matrix.M21);

            bool singular = sy < 1e-6;

            double roll, pitch, yaw;
            if (!singular)
            {
                roll = Math.Atan2(matrix.M32, matrix.M33);
                pitch = Math.Atan2(-matrix.M31, sy);
                yaw = Math.Atan2(matrix.M21, matrix.M11);
            }
            else
            {
                roll = Math.Atan2(-matrix.M23, matrix.M22);
                pitch = Math.Atan2(-matrix.M31, sy);
                yaw = 0;
            }

            return (roll, pitch, yaw);
        }
        public static (double phi, double theta, double psi) RotationMatrixToZYZ(Matrix4x4 matrix)
        {
            double sy = Math.Sqrt(matrix.M11 * matrix.M11 + matrix.M21 * matrix.M21);
            double phi, theta, psi;
            theta = Math.Acos(matrix.M33);
            bool singular = Math.Abs(theta) < 1e-6;


            if (singular)
            {
                phi = 0;
                psi = Math.Atan2(matrix.M21, matrix.M11);
            }
            else if (Math.Abs(theta - Math.PI) < 1e-6)
            {
                phi = 0;
                psi = Math.Atan2(matrix.M21, matrix.M11);
            }
            else
            {
                // Regular case
                phi = Math.Atan2(matrix.M23, matrix.M13);
                psi = Math.Atan2(matrix.M32, -matrix.M31);
            }
            return (phi, theta, psi);
        }

        public class Joint
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public Vector3 Position { get; set; }
            public Vector3 Rotation { get; set; }
            public double GlobalRoll { get; set; }
            public double GlobalPitch { get; set; }
            public double GlobalYaw { get; set; }
            public double Joint_angle { get; set; }

            public string Joint_axis { get; set; }
            public EulerZYZ ZYZ { get; set; }

        }
        public class EulerZYZ
        {
            public EulerZYZ()
            {
                Z1 = 0.0;
                Y2 = 0.0;
                Z3 = 0.0;
            }
            public double Z1 { get; set; }
            public double Y2 { get; set; }
            public double Z3 { get; set; }

        }
    }
}
    



