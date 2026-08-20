using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OpticalFlowAccumVis : MonoBehaviour
{
    [Header("Script Inputs")]
    [SerializeField]
    private ComputeShader opticalFlowAccumShader;
    [SerializeField]
    private Material opticalFlowScaleMaterial;

    [Header("Compute Shader Inputs")]
    [SerializeField]
    private RenderTexture opticalFlowTexture;
    [SerializeField]
    private int bufferSize = 6;

    [Header("Debug/Visualization")]
    [SerializeField]
    private RawImage accumOpticalFlowImage;


    private int width = 256;
    private int height = 256;

    private int kernel;
    private RenderTexture accumOpticalFlowTexture;
    private Queue<RenderTexture> opticalFlowBuffer;

    private RenderTexture accumExportTexture;
    private Texture2D captureTexture;


    void Start()
    {            
        kernel = opticalFlowAccumShader.FindKernel("AccumulateFlow");
        width = opticalFlowTexture.width;
        height = opticalFlowTexture.height;

        opticalFlowTexture.format = RenderTextureFormat.ARGBFloat;
        opticalFlowTexture.depth = 0;
        opticalFlowTexture.enableRandomWrite = true;
        opticalFlowTexture.Create();

        accumOpticalFlowTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat);
        accumOpticalFlowTexture.enableRandomWrite = true;
        accumOpticalFlowTexture.Create();

        opticalFlowBuffer = new Queue<RenderTexture>();

        accumExportTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        accumExportTexture.enableRandomWrite = true;
        accumExportTexture.Create();

        captureTexture = new Texture2D(width, height);
    }

    void Update()
    {
        AccumulateOpticalFlow();                
    }

    private void AccumulateOpticalFlow()
    {
        var opticalFlowFrame = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat);
        opticalFlowFrame.enableRandomWrite = true;
        opticalFlowFrame.Create();
        Graphics.Blit(opticalFlowTexture, opticalFlowFrame);

        if (opticalFlowBuffer.Count < 2)
        {
            opticalFlowBuffer.Enqueue(opticalFlowFrame);
            return;
        }

        if (opticalFlowBuffer.Count <= bufferSize)
        {
            opticalFlowBuffer.Enqueue(opticalFlowFrame);
            var blankTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat);
            blankTexture.enableRandomWrite = true;
            blankTexture.Create();
            DispatchComputeShader(opticalFlowFrame, blankTexture, accumOpticalFlowTexture, opticalFlowBuffer.Count);            
            blankTexture.Release();
            Destroy(blankTexture);
        }
        else
        {
            var oldFlow = opticalFlowBuffer.Dequeue();
            opticalFlowBuffer.Enqueue(opticalFlowFrame);
            DispatchComputeShader(opticalFlowFrame, oldFlow, accumOpticalFlowTexture, opticalFlowBuffer.Count);
            oldFlow.Release();
            Destroy(oldFlow);
        }
    }
    private void DispatchComputeShader(RenderTexture addFlow, RenderTexture subFlow, RenderTexture accumFlow, int framesInBuffer)
    {
        opticalFlowAccumShader.SetTexture(kernel, "AddFlow", addFlow);
        opticalFlowAccumShader.SetTexture(kernel, "SubFlow", subFlow);
        opticalFlowAccumShader.SetTexture(kernel, "AccumulatedFlow", accumFlow);

        opticalFlowAccumShader.SetInt("Width", width);
        opticalFlowAccumShader.SetInt("Height", height);
        opticalFlowAccumShader.SetInt("FramesInBuffer", framesInBuffer);        

        opticalFlowAccumShader.Dispatch(kernel, Mathf.CeilToInt(width/8f), Mathf.CeilToInt(height/8f), 1);

        accumOpticalFlowImage.texture = accumOpticalFlowTexture;
    }

    public RenderTexture GetOpticalFlowAccumTexture()
    {
        // Scale optical flow texture so that its visible
        Graphics.Blit(accumOpticalFlowTexture, accumExportTexture, opticalFlowScaleMaterial);
        // Set render target to target texture
        var currentRT = RenderTexture.active;
        RenderTexture.active = accumExportTexture;

        // Create a new texture and read the active Render Texture into it
        captureTexture.ReadPixels(new Rect(0, 0, accumExportTexture.width, accumExportTexture.height), 0, 0);
        captureTexture.Apply(false);

        // Set render texture back to default
        RenderTexture.active = currentRT;
        return accumExportTexture;
    }

    private void OnDestroy()
    {
        foreach (var rt in opticalFlowBuffer)
        {
            rt.Release();
            Destroy(rt);
        }

        if (accumOpticalFlowTexture != null)
        {
            accumOpticalFlowTexture.Release();
            Destroy(accumOpticalFlowTexture);
        }

        if (accumExportTexture != null)
        {
            accumExportTexture.Release();
            Destroy(accumExportTexture);
        }

        if (captureTexture != null)
            Destroy(captureTexture);
    }

}
