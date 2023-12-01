using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows.WebCam;

public class CameraManager : MonoBehaviour
{
    GameObject m_Canvas = null;
    Renderer m_CanvasRenderer = null;
    PhotoCapture m_PhotoCaptureObj;
    CameraParameters m_CameraParameters;
    bool m_CapturingPhoto = false;
    Texture2D m_Texture = null;

    public int numberPictures = 0;

    [SerializeField] GameObject mainCamera;
    [SerializeField] Shader holographicShader;

    private ProjectionManager projectionManager;

    private void Awake()
    {
        projectionManager = GameObject.Find("MixedRealityPlayspace/Projection Manager").GetComponent<ProjectionManager>();
    }

    private void Start()
    {
        Initialize();
    }

    public void ReleaseCamera()
    {
        m_PhotoCaptureObj.StopPhotoModeAsync(OnStopPhotoMode);
    }

    void OnStopPhotoMode(PhotoCapture.PhotoCaptureResult result)
    {
        Debug.Log("Released the camera.");
        m_PhotoCaptureObj.Dispose();
        m_PhotoCaptureObj = null;
    }
    public void Initialize()
    {
        List<Resolution> resolutions = new List<Resolution>(PhotoCapture.SupportedResolutions);
        Resolution selectedResolution = resolutions[0];

        m_CameraParameters = new CameraParameters(WebCamMode.PhotoMode);
        m_CameraParameters.cameraResolutionWidth = selectedResolution.width;
        m_CameraParameters.cameraResolutionHeight = selectedResolution.height;

        m_CameraParameters.hologramOpacity = 0.0f;
        m_CameraParameters.pixelFormat = CapturePixelFormat.BGRA32;

        m_Texture = new Texture2D(selectedResolution.width, selectedResolution.height, TextureFormat.BGRA32, false);

        PhotoCapture.CreateAsync(false, OnCreatedPhotoCaptureObject);
    }

    void OnCreatedPhotoCaptureObject(PhotoCapture captureObject)
    {
        m_PhotoCaptureObj = captureObject;
        m_PhotoCaptureObj.StartPhotoModeAsync(m_CameraParameters, OnStartPhotoMode);
    }

    void OnStartPhotoMode(PhotoCapture.PhotoCaptureResult result)
    {
    }

    void OnPhotoCaptured(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
    {
        numberPictures++;
        if (m_Canvas == null)
        {
            m_Canvas = GameObject.CreatePrimitive(PrimitiveType.Quad);
            m_Canvas.name = "PhotoCaptureCanvas";
            m_CanvasRenderer = m_Canvas.GetComponent<Renderer>() as Renderer;
            m_CanvasRenderer.material = new Material(holographicShader);
        }


        Matrix4x4 cameraToWorldMatrix;
        photoCaptureFrame.TryGetCameraToWorldMatrix(out cameraToWorldMatrix);
        Debug.Log($"world matrix: {cameraToWorldMatrix}");


        Matrix4x4 worldToCameraMatrix = cameraToWorldMatrix.inverse;

        Matrix4x4 projectionMatrix;
        photoCaptureFrame.TryGetProjectionMatrix(out projectionMatrix);

        if (Application.isEditor)
        {
            cameraToWorldMatrix = Camera.main.cameraToWorldMatrix;
            projectionMatrix = Camera.main.projectionMatrix;
        }



        photoCaptureFrame.UploadImageDataToTexture(m_Texture);
        m_Texture.wrapMode = TextureWrapMode.Clamp;

        m_CanvasRenderer.sharedMaterial.SetTexture("_MainTex", m_Texture);
        m_CanvasRenderer.sharedMaterial.SetMatrix("_WorldToCameraMatrix", worldToCameraMatrix);
        m_CanvasRenderer.sharedMaterial.SetMatrix("_CameraProjectionMatrix", projectionMatrix);
        m_CanvasRenderer.sharedMaterial.SetFloat("_VignetteScale", 0.0f);


        // Position the canvas object slightly in front
        // of the real world web camera.
        Vector3 position = cameraToWorldMatrix.GetColumn(3) - cameraToWorldMatrix.GetColumn(2);

        // Rotate the canvas object so that it faces the user.
        Quaternion rotation = Quaternion.LookRotation(-cameraToWorldMatrix.GetColumn(2), cameraToWorldMatrix.GetColumn(1));

        m_Canvas.transform.position = position;
        m_Canvas.transform.rotation = rotation;

        m_Canvas.SetActive(false);

        byte[] bytes = m_Texture.EncodeToJPG(90);

        //UIManager.HololensBytesJPG = bytes;
        //UIManager.Singleton.SaveCameraMatrix(cameraToWorldMatrix, projectionMatrix, m_Canvas);
        Debug.Log("Photo captured. Calling saveCameraMatrix");
        projectionManager.saveCameraMatrix(cameraToWorldMatrix, projectionMatrix, m_Canvas, bytes);

        m_CapturingPhoto = false;
    }
    public void TakePhoto()
    {
        //Animations and sounds located in MenuSystemHandler code
        m_CapturingPhoto = true;
        m_PhotoCaptureObj.TakePhotoAsync(OnPhotoCaptured);
    }

    public void TakeDelayedPhoto()
    {
        StartCoroutine(delayPhoto());
    }

    IEnumerator delayPhoto()
    {
        yield return new WaitForSeconds(3f);
        if (m_CapturingPhoto)
        {
            yield break;
        }

        m_CapturingPhoto = true;
        m_PhotoCaptureObj.TakePhotoAsync(OnPhotoCaptured);
    }
}
