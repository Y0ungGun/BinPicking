using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DSRRobotControl
{
    public class CollideDetector : MonoBehaviour
    {
        public Material white_a;
        public Material gray_a;


        private Material white;
        private Material gray;

        public MeshRenderer base_;
        public MeshRenderer link1;
        public MeshRenderer link2_0;
        public MeshRenderer link2_1;
        public MeshRenderer link2_2;
        public MeshRenderer doosan_mark;
        public MeshRenderer link3;
        public MeshRenderer link4_0;
        public MeshRenderer link4_1;
        public MeshRenderer link5;
        public MeshRenderer link6;

        private bool isCollided = false;
        private int LineNum = -1;
        // private bool isLogged = false;

        // Start is called before the first frame update
        void Start()
        {
            gray = base_.material;
            white = link1.material;
        }

        // Update is called once per frame
        void Update()
        {
            if ((BaseCollider.Basecollision) || (Link1Collider.Link1collision) || (Link2Collider.Link2collision) || (Link3Collider.Link3collision) || (Link4_0Collider.Link4_0collision) || (Link4_1Collider.Link4_1collision) || (Link5Collider.Link5collision) || (Link6Collider.Link6collision))
            {
                Visualize();
                LogCollisionLine();
                isCollided = true;
            }
            else
            {
                Reset();
                isCollided = false;
            }
        }
        public bool GetIsCollided()
        {
            return isCollided;
        }
        void Visualize()
        {
            if (!BaseCollider.Basecollision)
            {
                base_.material = gray_a;
            }
            else
            {
                base_.material = gray;
            }

            if (!Link1Collider.Link1collision)
            {
                link1.material = white_a;
            }
            else
            {
                link1.material = white;
            }

            if (!Link2Collider.Link2collision)
            {
                link2_0.material = white_a;
                link2_1.material = gray_a;
                link2_2.material = white_a;
                doosan_mark.material = white_a;
            }
            else
            {
                link2_0.material = white;
                link2_1.material = gray;
                link2_2.material = white;
                doosan_mark.material = white;
            }

            if (!Link3Collider.Link3collision)
            {
                link3.material = white_a;
            }
            else
            {
                link3.material = white;
            }

            if (!Link4_0Collider.Link4_0collision && !Link4_1Collider.Link4_1collision)
            {
                link4_0.material = white_a;
                link4_1.material = white_a;
            }
            else
            {
                link4_0.material = white;
                link4_1.material = white;
            }

            if (!Link5Collider.Link5collision)
            {
                link5.material = white_a;
            }
            else
            {
                link5.material = white;
            }

            if (!Link6Collider.Link6collision)
            {
                link6.material = gray_a;
            }
            else
            {
                link6.material = gray;
            }
        }

        void Reset()
        {
            base_.material = gray;
            link1.material = white;
            link2_0.material = white;
            link2_1.material = gray;
            link2_2.material = white;
            doosan_mark.material= white;
            link3.material = white;
            link4_0.material = white;
            link4_1.material = white;
            link5.material = white;
            link6.material = gray;
        }

        void LogCollisionLine()
        {
            int moveIndex = DSRExecutor.moveIndex;
            for (int i = 0; i < CommandList.frames.Count; i++)
            {
                if (moveIndex <= CommandList.frames[i])
                {
                    if (i != LineNum)
                    {
                        LineNum = i;
                        LogCollision(LineNum);
                    }
                    break;
                }
            }
        }

        void LogCollision(int num)
        {
            Debug.LogWarning($"Collision Detected in line {num+1}, Command: {CommandList.commandNames[num]}, DesiredPosition: {CommandList.desiredPositions[num]}");
        }

    }
}

