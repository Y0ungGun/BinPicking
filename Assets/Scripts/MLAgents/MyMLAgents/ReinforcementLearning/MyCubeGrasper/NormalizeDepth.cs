using UnityEngine;
using System.IO;
using System;
using System.Text.RegularExpressions;

//exr형식의 render texture를 textrue 2d로 바로 저장하는 것이 되지 않아서 compute shader와 함께 png파일로 0~255로 정규화하고 정규화한 png를 저장하려고 했지만 실패

public class NormalizeDepth : MonoBehaviour
{
    public Camera depthCamera;
    public ComputeShader computeShader;
    
    private RenderTexture depthTexture;
    private RenderTexture resultTexture;
    public event Action OnDepthCaptureComplete;  // Depth 캡처 완료 이벤트 추가

    void Start()
    {
        // RenderTexture 설정
        depthTexture = new RenderTexture(1024, 1024, 24, RenderTextureFormat.Depth)
        {
            enableRandomWrite = false,
            name = "Depth Texture (dynamic)"
        };
        depthTexture.Create();

        // 결과를 저장할 RenderTexture
        // resultTexture = new RenderTexture(1024, 1024, 24, RenderTextureFormat.ARGB32) // PNG에 저장하기 위해 ARGB32 포맷 사용
        resultTexture = new RenderTexture(1024, 1024, 0, RenderTextureFormat.RFloat) // RFloat
        {
            enableRandomWrite = true,
            name = "Result Texture (dynamic)"
        };
        resultTexture.Create();

        depthCamera.targetTexture = depthTexture;
    }

    void Update()
    {
        // 카메라 렌더링 후, Compute Shader를 사용하여 연산 수행
        if (depthCamera != null)
        {
            depthCamera.Render(); //씬을 렌더링하여 깊이 텍스처를 캡처.

            int kernelHandle = computeShader.FindKernel("CSMain"); //compute shader에서 실행할 커널을 찾음
            computeShader.SetTexture(kernelHandle, "Source", depthTexture); //source texture로 depthtexture를 설정함.
            computeShader.SetTexture(kernelHandle, "Result", resultTexture); // result texture로 result textrue를 설정함.

            // Compute Shader 실행
            computeShader.Dispatch(kernelHandle, depthTexture.width / 8, depthTexture.height / 8, 1); //GPU에서는 병렬로 계산해야 하니 스레드로 나누어 GPU의 컴퓨팅 유닛에 전달됨.

            
            // 이제 resultTexture에는 정규화된 깊이 값이 저장됩니다

            // RenderTexture에서 Texture2D로 데이터 복사

            SaveRenderTextureToPNG(resultTexture);
        }
    }
    //-------------------------------------------------------------------

    public void Initialize()
    {
        // RenderTexture 설정
        depthTexture = new RenderTexture(1024, 1024, 24, RenderTextureFormat.Depth)
        {
            enableRandomWrite = false,
            name = "Depth Texture (dynamic)"
        };
        depthTexture.Create();

        // 결과를 저장할 RenderTexture
        // resultTexture = new RenderTexture(1024, 1024, 24, RenderTextureFormat.ARGB32) // PNG 저장을 위해 ARGB32 사용
        resultTexture = new RenderTexture(1024, 1024, 0, RenderTextureFormat.RFloat) // RFloat
        {
            enableRandomWrite = true,
            name = "Result Texture (dynamic)"
        };
        resultTexture.Create();

        depthCamera.targetTexture = depthTexture;
    }
    
    public RenderTexture BeginDepthCapture(bool capture = true)
    {
        resultTexture = CaptureDepthImage(capture);
        OnDepthCaptureComplete?.Invoke(); // 캡처 완료 이벤트 호출

        return resultTexture;
    }

    public RenderTexture CaptureDepthImage(bool capture = true)
    {
        if (depthCamera != null)
        {
            depthCamera.Render();

            int kernelHandle = computeShader.FindKernel("CSMain");
            computeShader.SetTexture(kernelHandle, "Source", depthTexture);
            computeShader.SetTexture(kernelHandle, "Result", resultTexture);

            // Compute Shader 실행
            computeShader.Dispatch(kernelHandle, depthTexture.width / 8, depthTexture.height / 8, 1);
            if (capture)
            {
                SaveRenderTextureToPNG(resultTexture);
                SaveRenderTextureToEXR(resultTexture);
            }
            
        }
        return resultTexture;
    }


    public Texture2D SaveRenderTextureToPNG(RenderTexture rt, bool shouldSave = true)
    {
        RenderTexture.active = rt;
        // Texture2D texture = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, false);
        Texture2D texture = new Texture2D(rt.width, rt.height, TextureFormat.RFloat, false);  //RFloat
        texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        texture.Apply();
        RenderTexture.active = null;

        if (shouldSave)
        {
            string absolutePath = "D:/MyProject/DepthImages/";
            if (!Directory.Exists(absolutePath))
            {
                Directory.CreateDirectory(absolutePath);
            }
            string fileName = $"{absolutePath}NormalizedDepth_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
            byte[] bytes = texture.EncodeToPNG();
            File.WriteAllBytes(fileName, bytes);
            Debug.Log($"RenderTexture saved as PNG file at {fileName}");
        }
        return texture;
    }


    public Texture2D SaveRenderTextureToEXR(RenderTexture rt, bool shouldSave = true)
    {
        RenderTexture.active = rt;

        // EXR 파일 저장을 위한 Texture2D (RFloat 사용)
        Texture2D texture = new Texture2D(rt.width, rt.height, TextureFormat.RFloat, false);
        texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        texture.Apply();
        RenderTexture.active = null;

        if (shouldSave)
        {
            string absolutePath = "D:/MyProject/DepthImages_exr/";
            if (!Directory.Exists(absolutePath))
            {
                Directory.CreateDirectory(absolutePath);
            }
            string fileName = $"{absolutePath}NormalizedDepth_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}.exr";
            byte[] bytes = texture.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat);
            File.WriteAllBytes(fileName, bytes);
            Debug.Log($"RenderTexture saved as EXR file at {fileName}");
        }
        return texture;
    }

}
