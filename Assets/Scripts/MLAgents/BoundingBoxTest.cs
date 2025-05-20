using Assimp;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Camera = UnityEngine.Camera;

public class BoundingBoxTest : MonoBehaviour
{
    public Camera mainCamera;

    Renderer r;
    Texture2D redTexture;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        r = GetComponent<Renderer>();
        redTexture = new Texture2D(1, 1);
        redTexture.SetPixel(0, 0, Color.red);
        redTexture.Apply();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void DrawBoundingBox(Rect rect)
    {
        // �׵θ��� ������ ������ �׸���
        Debug.DrawLine(new Vector3(rect.xMin, rect.yMin, 0), new Vector3(rect.xMax, rect.yMin, 0), Color.red);
        Debug.DrawLine(new Vector3(rect.xMax, rect.yMin, 0), new Vector3(rect.xMax, rect.yMax, 0), Color.red);
        Debug.DrawLine(new Vector3(rect.xMax, rect.yMax, 0), new Vector3(rect.xMin, rect.yMax, 0), Color.red);
        Debug.DrawLine(new Vector3(rect.xMin, rect.yMax, 0), new Vector3(rect.xMin, rect.yMin, 0), Color.red);
    }
    private void OnGUI()
    {
        if (r == null || mainCamera == null) return;

        // Bounds ��������
        Bounds bounds = r.bounds;

        // 8���� �ڳ� ���
        Vector3[] corners = new Vector3[8];
        corners[0] = bounds.min;
        corners[1] = new Vector3(bounds.min.x, bounds.min.y, bounds.max.z);
        corners[2] = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z);
        corners[3] = new Vector3(bounds.min.x, bounds.max.y, bounds.max.z);
        corners[4] = new Vector3(bounds.max.x, bounds.min.y, bounds.min.z);
        corners[5] = new Vector3(bounds.max.x, bounds.min.y, bounds.max.z);
        corners[6] = new Vector3(bounds.max.x, bounds.max.y, bounds.min.z);
        corners[7] = bounds.max;

        // ��ũ�� ��ǥ�� ��ȯ
        Vector3[] screenCorners = new Vector3[8];
        for (int i = 0; i < corners.Length; i++)
        {
            screenCorners[i] = mainCamera.WorldToScreenPoint(corners[i]);
        }

        // 2D Rect ���
        float minX = screenCorners[0].x, maxX = screenCorners[0].x;
        float minY = screenCorners[0].y, maxY = screenCorners[0].y;
        for (int i = 1; i < screenCorners.Length; i++)
        {
            minX = Mathf.Min(minX, screenCorners[i].x);
            maxX = Mathf.Max(maxX, screenCorners[i].x);
            minY = Mathf.Min(minY, screenCorners[i].y);
            maxY = Mathf.Max(maxY, screenCorners[i].y);
        }

        // �� �׸���
        DrawBorder(new Rect(minX, Screen.height - maxY, maxX - minX, maxY - minY), 3);
        //Rect rect = new Rect(minX, Screen.height - maxY, maxX - minX, maxY - minY);
        //GUI.color = Color.red;
        //GUI.DrawTexture(rect, Texture2D.whiteTexture);
    }
    void DrawBorder(Rect rect, float thickness)
    {
        // ��� ��
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, rect.width, thickness), redTexture);
        // �ϴ� ��
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), redTexture);
        // ���� ��
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, thickness, rect.height), redTexture);
        // ���� ��
        GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), redTexture);
    }
}
