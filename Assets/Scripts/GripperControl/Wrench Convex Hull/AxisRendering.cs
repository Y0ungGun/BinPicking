using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GripperGWS
{
    public class AxisRendering : MonoBehaviour
    {
        private float axisLength = 0.5f; // 축 길이
        private Vector3 origin1 = new Vector3(0, 0, 9f); // 축 1 시작점
        private Vector3 origin2 = new Vector3(0, 0, 13f);

        private LineRenderer xAxisLine1;
        private LineRenderer yAxisLine1;
        private LineRenderer zAxisLine1;
        private LineRenderer xAxisLine2;
        private LineRenderer yAxisLine2;
        private LineRenderer zAxisLine2;
        // Start is called before the first frame update
        void Start()
        {
            xAxisLine1 = CreateLineRenderer(Color.red);
            xAxisLine2 = CreateLineRenderer(Color.red);
            xAxisLine1.SetPositions(new Vector3[] { origin1, origin1 + Vector3.right * axisLength });
            xAxisLine2.SetPositions(new Vector3[] { origin2, origin2 + Vector3.right * axisLength });

            // Y�� LineRenderer
            yAxisLine1 = CreateLineRenderer(Color.green);
            yAxisLine2 = CreateLineRenderer(Color.green);
            yAxisLine1.SetPositions(new Vector3[] { origin1, origin1 + Vector3.up * axisLength });
            yAxisLine2.SetPositions(new Vector3[] { origin2, origin2 + Vector3.up * axisLength });

            // Z�� LineRenderer
            zAxisLine1 = CreateLineRenderer(Color.blue);
            zAxisLine2 = CreateLineRenderer(Color.blue);
            zAxisLine1.SetPositions(new Vector3[] { origin1, origin1 + Vector3.forward * axisLength });
            zAxisLine2.SetPositions(new Vector3[] { origin2, origin2 + Vector3.forward * axisLength });
        }

        private LineRenderer CreateLineRenderer(Color color)
        {
            // GameObject ����
            GameObject lineObj = new GameObject("AxisLine");
            lineObj.transform.parent = transform;

            // LineRenderer ������Ʈ �߰�
            LineRenderer lineRenderer = lineObj.AddComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("Sprites/Default")); // �⺻ Shader
            lineRenderer.startWidth = 0.02f; // ���� �β�
            lineRenderer.endWidth = 0.02f;   // �� �β�
            lineRenderer.startColor = color; // ���� ����
            lineRenderer.endColor = color;   // �� ����
            lineRenderer.positionCount = 2;  // �� ������ ���� ����
            lineRenderer.useWorldSpace = true; // ���� ��ǥ�� ���

            return lineRenderer;
        }
    }
}
