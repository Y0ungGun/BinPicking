using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GripperGWS;

namespace GripperGWS
{
    public class TargetContact : MonoBehaviour
    {
        public int n = 8;
        public bool isContact = false;
        private Rigidbody rb;
        private float lambda = 1.0f; // 특성 길이(초기값 1.0, Start에서 자동 계산)
        private Collision lastCollision = null;
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            // lambda를 물체의 bounding box 대각선으로 자동 설정
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                lambda = 1 / col.bounds.size.magnitude;
            }
        }

        private void OnCollisionStay(Collision collision)
        {
            if (collision.gameObject.name.Contains("Jaw"))
            {
                lambda = 1 / collision.collider.bounds.size.magnitude;
                isContact = true;
                lastCollision = collision; // collision 정보만 저장
            }
        }

        private void OnCollisionExit(Collision collision)
        {
            isContact = false;
            lastCollision = null;
        }

        public void GetWrenches()
        {
            if (!isContact || lastCollision == null) return;

            List<Vector6> Wrenches = new List<Vector6>();
            List<Vector3> forces = new List<Vector3>();
            List<Vector3> moments = new List<Vector3>();

            float minDist = 0.001f;
            Rigidbody rb = GetComponent<Rigidbody>();

            foreach (ContactPoint contact in lastCollision.contacts)
            {
                Vector3 contactPoint = contact.point;
                bool isDuplicate = false;
                foreach (var prev in forces)
                {
                    if ((prev - contactPoint).sqrMagnitude < minDist * minDist)
                    {
                        isDuplicate = true;
                        break;
                    }
                }
                if (isDuplicate) continue;

                Vector3 centerOfMass = rb.worldCenterOfMass;
                float NormalForce = 1;
                Vector3 NormalForceVector = contact.normal;
                float FrictionForce = 0.5f * NormalForce;

                for (int k = 0; k < n; k++)
                {
                    float angle = 2 * Mathf.PI / n * k;
                    Vector3 tangent = Vector3.Cross(contact.normal, Vector3.up).normalized;
                    Vector3 bitangent = Vector3.Cross(contact.normal, tangent).normalized;
                    Vector3 FrictionForceVector = (tangent * Mathf.Cos(angle) + bitangent * Mathf.Sin(angle)) * FrictionForce;
                    Vector3 force = FrictionForceVector + NormalForceVector;
                    Vector3 moment = Vector3.Cross(contactPoint - centerOfMass, force) * lambda;
                    Vector6 Wrench = new Vector6(force, moment);
                    Wrenches.Add(Wrench);
                    forces.Add(force);
                    moments.Add(moment);
                }
            }
            WrenchCollector.Instance.UpdateForces(lastCollision.collider, forces);
            WrenchCollector.Instance.UpdateMoments(lastCollision.collider, moments);
        }
    }
}