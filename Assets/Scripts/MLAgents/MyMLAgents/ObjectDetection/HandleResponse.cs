using UnityEngine;
using System.IO;
using System.Collections.Generic;
using Assimp.Unmanaged;
namespace MyMLAgents
{
    // Response of Object Detection model Utilization
    public static class HandleResponse
    {
        public static int FindTargetBoundingBoxIndex(List<float[]> detections, Camera cam, GameObject target)
        {
            float crop_x = (cam.pixelWidth - 736) / 2f;  
            float crop_y = (cam.pixelHeight - 736) / 2f;

            Transform targetTransform = target.transform;
            Vector3 targetViewportPos = cam.WorldToViewportPoint(targetTransform.position);
            float screenX = targetViewportPos.x * cam.pixelWidth - crop_x;
            float screenY = targetViewportPos.y * cam.pixelHeight - crop_y;
            Vector2 targetScaledPos = new Vector2(screenX, 736 - screenY);

            float minDist = float.MaxValue;
            int bestMatchIndex = -1;
            List<Vector2> tt = new List<Vector2>();
            for (int i = 0; i < detections.Count; i++)
            {
                float[] detection = detections[i];
                int x1 = (int)detection[0];
                int y1 = (int)detection[1];
                int x2 = (int)detection[2];
                int y2 = (int)detection[3];

                int x_cen = (x2 + x1) / 2;
                int y_cen = (y2 + y1) / 2;
                Vector2 detectionPos = new Vector2(x_cen, y_cen);
                tt.Add(detectionPos);

                Vector2 aa = new Vector2((int)targetScaledPos.x, (int)targetScaledPos.y);
                float dist = Vector2.Distance(aa, detectionPos);

                if (dist < minDist)
                {
                    minDist = dist;
                    bestMatchIndex = i;
                }
            }

            return bestMatchIndex;
        }
        public static void CreateBoundingBoxPNG(float[] targetDetection, Camera cam)
        {
            float x1 = targetDetection[0];
            float y1 = 736 - targetDetection[1];
            float x2 = targetDetection[2];
            float y2 = 736 - targetDetection[3];
            float x = (x1 + x2) / 2;
            float y = (y1 + y2) / 2;
            float w = 120;
            float h = 120;
            float x0 = x - w / 2;
            float y0 = y - h / 2;   
            RenderTexture renderTexture = new RenderTexture(1280, 740, 16);
            cam.targetTexture = renderTexture;
            cam.Render();

            Texture2D fullTexture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
            RenderTexture.active = renderTexture;
            fullTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            fullTexture.Apply();
            RenderTexture.active = null;

            int cropSize = 736;
            int centerX = renderTexture.width / 2;
            int centerY = renderTexture.height / 2;
            int startX = centerX - (cropSize / 2);
            int startY = centerY - (cropSize / 2);

            Texture2D croppedTexture = new Texture2D(cropSize, cropSize, TextureFormat.RGB24, false);
            croppedTexture.SetPixels(fullTexture.GetPixels(startX, startY, cropSize, cropSize));
            croppedTexture.Apply();
            // BoundingBox를 그리기 위한 화면 좌표 계산
            Rect boundingBox = new Rect(x0, y0, w, h);

            // 이미지에 BoundingBox 그리기 (빨간색으로 표시)
            DrawBoundingBox(croppedTexture, boundingBox);


            // 이미지 저장
            // SaveImageAsPNG(croppedTexture, "HandleResponse.png");
            cam.targetTexture = null;
            RenderTexture.active = null;
            Object.Destroy(renderTexture);
        }
        private static void DrawBoundingBox(Texture2D image, Rect boundingBox)
        {
            // BoundingBox의 외곽선을 이미지에 그리기
            Color[] colors = image.GetPixels();

            // 상단 선 그리기
            for (int x = (int)boundingBox.xMin; x < boundingBox.xMax; x++)
                image.SetPixel(x, (int)boundingBox.yMin, Color.red);

            // 하단 선 그리기
            for (int x = (int)boundingBox.xMin; x < boundingBox.xMax; x++)
                image.SetPixel(x, (int)boundingBox.yMax, Color.red);

            // 좌측 선 그리기
            for (int y = (int)boundingBox.yMin; y < boundingBox.yMax; y++)
                image.SetPixel((int)boundingBox.xMin, y, Color.red);

            // 우측 선 그리기
            for (int y = (int)boundingBox.yMin; y < boundingBox.yMax; y++)
                image.SetPixel((int)boundingBox.xMax, y, Color.red);
        }

        private static void SaveImageAsPNG(Texture2D image, string filePath)
        {
            // 텍스처를 PNG로 저장
            byte[] pngData = image.EncodeToPNG();
            File.WriteAllBytes(filePath, pngData);
            //Debug.Log("Saved PNG to: " + filePath);
        }
    }

}
