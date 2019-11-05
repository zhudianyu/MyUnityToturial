using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using Conditional = System.Diagnostics.ConditionalAttribute;
public class MyPippeline : RenderPipeline
{
    DrawRendererFlags drawFlags;
    public MyPippeline(bool dynamicBatching,bool instancing)
    {
        if(dynamicBatching)
        {
            drawFlags = DrawRendererFlags.EnableDynamicBatching;
        }
        if(instancing)
        {
            drawFlags |= DrawRendererFlags.EnableInstancing;
        }
    }
    CullResults cull;
    CommandBuffer buffer = new CommandBuffer
    {
        name = "Render Camera"
    };

    Material errorMaterial;
    public override void Render(ScriptableRenderContext renderContext, Camera[] cameras)
    {
        base.Render(renderContext, cameras);
        foreach(var cam in cameras)
        {
            Render(renderContext, cam);
        }
      
    }
    void Render(ScriptableRenderContext rendercontext, Camera cam)
    {
         ScriptableCullingParameters cullingParameters;
        //设置剔除参数
       if(!CullResults.GetCullingParameters(cam, out cullingParameters))
        {
            return;
        }
        cull = CullResults.Cull(ref cullingParameters, rendercontext);

        rendercontext.SetupCameraProperties(cam);
      
        CameraClearFlags clearFlags = cam.clearFlags;
        buffer.BeginSample("Render Camera");
        buffer.ClearRenderTarget((clearFlags&CameraClearFlags.Depth) != 0, (clearFlags&CameraClearFlags.Color)!= 0, cam.backgroundColor);
        //把ugui 显示在scene视图
#if UNITY_EDITOR
        if (cam.cameraType == CameraType.SceneView)
        {
            ScriptableRenderContext.EmitWorldGeometryForSceneView(cam);
        }
#endif
        // buffer.Release();

        var drawSetting = new DrawRendererSettings(cam,new ShaderPassName("SRPDefaultUnlit"));
        drawSetting.sorting.flags = SortFlags.CommonOpaque;
        drawSetting.flags = drawFlags;

        var filterSetting = new FilterRenderersSettings(true) {
            renderQueueRange = RenderQueueRange.opaque
        };
      
        rendercontext.DrawRenderers(cull.visibleRenderers, ref drawSetting, filterSetting);
        rendercontext.DrawSkybox(cam);
        drawSetting.sorting.flags = SortFlags.CommonTransparent;
        filterSetting.renderQueueRange = RenderQueueRange.transparent;
     
        rendercontext.DrawRenderers(cull.visibleRenderers, ref drawSetting, filterSetting);

        DrawDefaultPipeLine(rendercontext, cam);


        buffer.EndSample("Render Camera");
        rendercontext.ExecuteCommandBuffer(buffer);

        buffer.Clear();
        rendercontext.Submit();

    }
    [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
    void DrawDefaultPipeLine(ScriptableRenderContext context,Camera camera)
    {
        if(errorMaterial == null)
        {
            Shader errorShader = Shader.Find("Hidden/InternalErrorShader");
            errorMaterial = new Material(errorShader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
        }
        var drawSetting = new DrawRendererSettings(
        
            camera,new ShaderPassName("ForwardBase")
        );
        drawSetting.SetShaderPassName(1, new ShaderPassName("PrepassBase"));
        drawSetting.SetShaderPassName(2, new ShaderPassName("Always"));
        drawSetting.SetShaderPassName(3, new ShaderPassName("Vertex"));
        drawSetting.SetShaderPassName(4, new ShaderPassName("VertexLMRGBM"));
        drawSetting.SetShaderPassName(5, new ShaderPassName("VertexLM"));
        drawSetting.SetOverrideMaterial(errorMaterial, 0);
        var filterSetting = new FilterRenderersSettings(true);
        context.DrawRenderers(cull.visibleRenderers, ref drawSetting, filterSetting);
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
