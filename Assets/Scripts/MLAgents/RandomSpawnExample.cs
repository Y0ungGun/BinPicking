using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class RandomSpawnExample : MonoBehaviour
{
    private Vector3 positionRangeMax;
    private Vector3 positionRangeMin;
    private GameObject target;
    private Camera mainCamera;

    Renderer r;
    Texture2D redTexture;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mainCamera = GameObject.Find("Display2").GetComponent<Camera>();    
        target = this.gameObject;
        positionRangeMax = GameObject.Find("Corner_max").transform.position;
        positionRangeMin = GameObject.Find("Corner_min").transform.position;

        r = GetComponent<Renderer>();
        redTexture = new Texture2D(1, 1);
        redTexture.SetPixel(0, 0, Color.red);
        redTexture.Apply();

        //InvokeRepeating("RandomizeTargetPosition", 5f, 5f);
    }

    // Update is called once per frame
    void Update()
    {

    }
    public void RandomizeTargetPosition()
    {
        float randomX = Random.Range(positionRangeMin.x, positionRangeMax.x);
        float randomY = Random.Range(positionRangeMin.y, positionRangeMax.y);
        float randomZ = Random.Range(positionRangeMin.z, positionRangeMax.z);

        target.transform.position = new Vector3(randomX, randomY, randomZ);

        float randomRotationX = Random.Range(0f, 360f); // Random rotation on X-axis
        float randomRotationY = Random.Range(0f, 360f); // Random rotation on Y-axis
        float randomRotationZ = Random.Range(0f, 360f); // Random rotation on Z-axis

        target.transform.rotation = Quaternion.Euler(randomRotationX, randomRotationY, randomRotationZ);
    }
    void DrawBoundingBox(Rect rect)
    {
        // 테두리만 빨간색 선으로 그리기
        Debug.DrawLine(new Vector3(rect.xMin, rect.yMin, 0), new Vector3(rect.xMax, rect.yMin, 0), Color.red);
        Debug.DrawLine(new Vector3(rect.xMax, rect.yMin, 0), new Vector3(rect.xMax, rect.yMax, 0), Color.red);
        Debug.DrawLine(new Vector3(rect.xMax, rect.yMax, 0), new Vector3(rect.xMin, rect.yMax, 0), Color.red);
        Debug.DrawLine(new Vector3(rect.xMin, rect.yMax, 0), new Vector3(rect.xMin, rect.yMin, 0), Color.red);
    }
    private void OnGUI()
    {
        if (r == null || mainCamera == null) return;

        // Bounds 가져오기
        Bounds bounds = r.bounds;

        // 8개의 코너 계산
        Vector3[] corners = new Vector3[8];
        corners[0] = bounds.min;
        corners[1] = new Vector3(bounds.min.x, bounds.min.y, bounds.max.z);
        corners[2] = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z);
        corners[3] = new Vector3(bounds.min.x, bounds.max.y, bounds.max.z);
        corners[4] = new Vector3(bounds.max.x, bounds.min.y, bounds.min.z);
        corners[5] = new Vector3(bounds.max.x, bounds.min.y, bounds.max.z);
        corners[6] = new Vector3(bounds.max.x, bounds.max.y, bounds.min.z);
        corners[7] = bounds.max;

        // 스크린 좌표로 변환
        Vector3[] screenCorners = new Vector3[8];
        for (int i = 0; i < corners.Length; i++)
        {
            screenCorners[i] = mainCamera.WorldToScreenPoint(corners[i]);
        }

        // 2D Rect 계산
        float minX = screenCorners[0].x, maxX = screenCorners[0].x;
        float minY = screenCorners[0].y, maxY = screenCorners[0].y;
        for (int i = 1; i < screenCorners.Length; i++)
        {
            minX = Mathf.Min(minX, screenCorners[i].x);
            maxX = Mathf.Max(maxX, screenCorners[i].x);
            minY = Mathf.Min(minY, screenCorners[i].y);
            maxY = Mathf.Max(maxY, screenCorners[i].y);
        }

        // 선 그리기
        DrawBorder(new Rect(minX, Screen.height - maxY, maxX - minX, maxY - minY), 3);
        //Rect rect = new Rect(minX, Screen.height - maxY, maxX - minX, maxY - minY);
        //GUI.color = Color.red;
        //GUI.DrawTexture(rect, Texture2D.whiteTexture);
    }
    void DrawBorder(Rect rect, float thickness)
    {
        // 상단 변
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, rect.width, thickness), redTexture);
        // 하단 변
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), redTexture);
        // 좌측 변
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, thickness, rect.height), redTexture);
        // 우측 변
        GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), redTexture);
    }
}
