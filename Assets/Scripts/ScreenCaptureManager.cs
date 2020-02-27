using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCvSharp;

public class ScreenCaptureManager : MonoBehaviour
{
    private static ScreenCaptureManager _instance;

    public static ScreenCaptureManager Instance { get { return _instance; } }

    public MeshRenderer _renderer;

    [SerializeField] uDesktopDuplication.Texture uddTexture;
    [SerializeField] int x = 0;
    [SerializeField] int y = 0;
    [SerializeField] int w = 2560;
    [SerializeField] int h = 1440;

    public Texture2D texture;
    Color32[] colors;

    uDesktopDuplication.Monitor monitor;
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }
    }

    void CreateTextureIfNeeded()
    {
        if (!texture || texture.width != w || texture.height != h)
        {
            colors = new Color32[w * h];
            texture = new Texture2D(w, h, TextureFormat.ARGB32, false);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        CreateTextureIfNeeded();
    }

    // Update is called once per frame
    void Update()
    {
        CreateTextureIfNeeded();

        // must be called (performance will be slightly down).
        uDesktopDuplication.Manager.primary.useGetPixels = true;

        monitor = uddTexture.monitor;
        if (!monitor.hasBeenUpdated) return;
    }

    public Color32[] GetPixelsResizedTo1080()
    {
        var pixels = monitor.GetPixels(x , y, w, h);
        Mat fullsize = OpenCvSharp.Unity.PixelsToMat(pixels, w, h, true, true, 0);
        Mat shrunkImage = fullsize.Resize(new Size(1920, 1080));
        var shrunkColors = OpenCvSharp.Unity.MatToTexture(shrunkImage);

        return shrunkColors.GetPixels32();
    }

    public Color32[] GetIconIn1080Screen(IconPosition icon)
    {
        var pixels = monitor.GetPixels(x, y, w, h);
        Mat fullsize = OpenCvSharp.Unity.PixelsToMat(pixels, w, h, true, true, 0);
        Mat shrunkImage = fullsize.Resize(new Size(1920, 1080));
        OpenCvSharp.Rect cropped_roi = new OpenCvSharp.Rect(icon.x, icon.y, icon.width, icon.height);
        var croppedImage = new Mat(shrunkImage, cropped_roi);
        var croppedTexture = OpenCvSharp.Unity.MatToTexture(croppedImage);
        return croppedTexture.GetPixels32();
    }
}
