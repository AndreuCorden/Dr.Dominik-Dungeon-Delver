using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Main menu uses a split viewport: the 3D camera only draws the left ~58%.
/// A full-screen background camera must clear color first; otherwise the right UI
/// panel keeps whatever was last rendered (gameplay, credits, etc.).
/// </summary>
public static class MainMenuCameraSetup
{
    const string BackgroundCameraName = "MainMenuBackgroundCamera";
    const float MainCameraViewportWidth = 0.58f;

    public static void Apply()
    {
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = Color.black;

        EnsureBackgroundCamera();
        ConfigureMainCamera();
    }

    static void EnsureBackgroundCamera()
    {
        GameObject bgObject = GameObject.Find(BackgroundCameraName);
        if (bgObject == null)
            bgObject = new GameObject(BackgroundCameraName);

        if (bgObject.GetComponent<Camera>() == null)
            bgObject.AddComponent<Camera>();

        if (bgObject.GetComponent<UniversalAdditionalCameraData>() == null)
            bgObject.AddComponent<UniversalAdditionalCameraData>();

        Camera bgCam = bgObject.GetComponent<Camera>();
        bgCam.depth = -10f;
        bgCam.clearFlags = CameraClearFlags.SolidColor;
        bgCam.backgroundColor = Color.black;
        bgCam.cullingMask = 0;
        bgCam.rect = new Rect(0f, 0f, 1f, 1f);
        bgCam.orthographic = true;
        bgCam.orthographicSize = 1f;
        bgCam.nearClipPlane = 0.3f;
        bgCam.farClipPlane = 1000f;

        AudioListener listener = bgObject.GetComponent<AudioListener>();
        if (listener != null)
            Object.Destroy(listener);
    }

    static void ConfigureMainCamera()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
            return;

        if (mainCam.GetComponent<UniversalAdditionalCameraData>() == null)
            mainCam.gameObject.AddComponent<UniversalAdditionalCameraData>();

        mainCam.orthographic = true;
        mainCam.clearFlags = CameraClearFlags.SolidColor;
        mainCam.backgroundColor = Color.black;
        mainCam.depth = -1f;
        mainCam.rect = new Rect(0f, 0f, MainCameraViewportWidth, 1f);
    }
}
