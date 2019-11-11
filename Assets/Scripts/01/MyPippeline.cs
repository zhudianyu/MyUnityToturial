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
    const int maxVisibleLights = 16;
    //方向光
    static int visibleLightColorsId = Shader.PropertyToID("_VisibleLightColors");
    Vector4[] visibleLightColors = new Vector4[maxVisibleLights];
    //点光源 方向和位置
    static int visibleLightDirectionsOrPositionsId =Shader.PropertyToID("_VisibleLightDirectionsOrPositions");
    Vector4[] visibleLightDirectionsOrPositions = new Vector4[maxVisibleLights];
    //衰减
    static int visibleLightAttenuationId = Shader.PropertyToID("_VisibleLightAttenuations");
    Vector4[] visibleLightAtteuations = new Vector4[maxVisibleLights];

    //聚光灯
    static int visibleLightSpotDirectionsId = Shader.PropertyToID("_VisibleLightSpotDirections");
    Vector4[] visibleLightSpotDirections = new Vector4[maxVisibleLights];

    static int lightIndicesOffsetAndCountID =
    Shader.PropertyToID("unity_LightIndicesOffsetAndCount");

    private static int shadowMapId = Shader.PropertyToID("_ShadowMap");

    private static int worldToShadowMatrixId = Shader.PropertyToID("_WorldToShadowMatrix");
    private RenderTexture shadowMap;

    private int shadowMapSize;
    public MyPippeline(bool dynamicBatching,bool instancing,int _shadowMapSize)
    {
        GraphicsSettings.lightsUseLinearIntensity = true;
        if(dynamicBatching)
        {
            drawFlags = DrawRendererFlags.EnableDynamicBatching;
        }
        if(instancing)
        {
            drawFlags |= DrawRendererFlags.EnableInstancing;
        }

        this.shadowMapSize = _shadowMapSize;
    }
    CullResults cull;
    CommandBuffer buffer = new CommandBuffer
    {
        name = "Render Camera"
    };

    private CommandBuffer shadowBuffer = new CommandBuffer()
    {
        name = "Render Shadows"
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
        //设置阴影
        RenderShadows(rendercontext);
        
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
        //设置unity 用float4来存储灯光索引
        if(cull.visibleLights.Count > 0)
        {//避免没有光的时候 unity crash
            drawSetting.rendererConfiguration = RendererConfiguration.PerObjectLightIndices8;
        }

        var filterSetting = new FilterRenderersSettings(true) {
            renderQueueRange = RenderQueueRange.opaque
        };
      
        rendercontext.DrawRenderers(cull.visibleRenderers, ref drawSetting, filterSetting);
        rendercontext.DrawSkybox(cam);
        drawSetting.sorting.flags = SortFlags.CommonTransparent;
        filterSetting.renderQueueRange = RenderQueueRange.transparent;
     
        rendercontext.DrawRenderers(cull.visibleRenderers, ref drawSetting, filterSetting);

        DrawDefaultPipeLine(rendercontext, cam);
        if(cull.visibleLights.Count > 0)
        {
            ConfigureLights();
        }
        else
        {
            buffer.SetGlobalVector(lightIndicesOffsetAndCountID, Vector4.zero);
        }

        buffer.EndSample("Render Camera");
        //向gpu传送数据
        buffer.SetGlobalVectorArray(visibleLightColorsId, visibleLightColors);
        buffer.SetGlobalVectorArray(visibleLightDirectionsOrPositionsId, visibleLightDirectionsOrPositions);
        buffer.SetGlobalVectorArray(visibleLightAttenuationId, visibleLightAtteuations);
        buffer.SetGlobalVectorArray(visibleLightSpotDirectionsId, visibleLightSpotDirections);
        rendercontext.ExecuteCommandBuffer(buffer);

        buffer.Clear();
        rendercontext.Submit();

        if (shadowMap)
        {
            RenderTexture.ReleaseTemporary(shadowMap);
            shadowMap = null;
        }

    }

    void RenderShadows(ScriptableRenderContext context)
    {
        shadowMap = RenderTexture.GetTemporary(shadowMapSize, shadowMapSize, 16, RenderTextureFormat.Shadowmap);
        shadowMap.filterMode = FilterMode.Bilinear;
        shadowMap.wrapMode = TextureWrapMode.Clamp;
        
        CoreUtils.SetRenderTarget(shadowBuffer,shadowMap,RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, ClearFlag.Depth);
        shadowBuffer.BeginSample("Render Shadow");
        context.ExecuteCommandBuffer(shadowBuffer);
        shadowBuffer.Clear();
        
        
        shadowBuffer.EndSample("Render Shadow");
        context.ExecuteCommandBuffer(shadowBuffer);
        shadowBuffer.Clear();

        Matrix4x4 viewMatrix, projectionMatrix;
        ShadowSplitData splitData;
        cull.ComputeSpotShadowMatricesAndCullingPrimitives(0, out viewMatrix, out projectionMatrix, out splitData);
        shadowBuffer.SetViewProjectionMatrices(viewMatrix,projectionMatrix);
        if (SystemInfo.usesReversedZBuffer)
        {
//            projectionMatrix.m20 = -projectionMatrix.m20;
                                                           //            projectionMatrix.m21 = -projectionMatrix.m21;
                                                           //            projectionMatrix.m22 = -projectionMatrix.m22;
                                                           //            projectionMatrix.m23 = -projectionMatrix.m23;
        }

       // var scaleOffset = Matrix4x4.TRS(Vector3.one * 0.5f, Quaternion.identity, Vector3.one * 0.5f);
        
        var scaleOffset = Matrix4x4.identity;
        scaleOffset.m00 = scaleOffset.m11 = scaleOffset.m22 = 0.5f;
        scaleOffset.m03 = scaleOffset.m13 = scaleOffset.m23 = 0.5f;
        Matrix4x4 worldToShadowMatrix = (viewMatrix * projectionMatrix)*scaleOffset;
        shadowBuffer.SetGlobalMatrix(worldToShadowMatrixId,worldToShadowMatrix);
        shadowBuffer.SetGlobalTexture(shadowMapId,shadowMap);
        context.ExecuteCommandBuffer(shadowBuffer);
        shadowBuffer.Clear();
        
        var shadowSettings = new DrawShadowsSettings(cull,0);
        context.DrawShadows(ref shadowSettings);
     
    
    }
    
     
    void ConfigureLights()
    {
        
        for (int i = 0; i <cull.visibleLights.Count;i++)
        {
            if(i == maxVisibleLights)
            {
                break;
            }
            VisibleLight light = cull.visibleLights[i];
            visibleLightColors[i] = light.finalColor;
            Vector4 attenuation = Vector4.zero;
            attenuation.w = 1f;
            if(light.lightType == LightType.Directional)
            {
                Vector4 v = light.localToWorld.GetColumn(2);
                v.x = -v.x;
                v.y = -v.y;
                v.z = -v.z;
                //光的方向
                visibleLightDirectionsOrPositions[i] = v;
            }
            else
            {
                visibleLightDirectionsOrPositions[i] = light.localToWorld.GetColumn(3);
                attenuation.x = 1f / Mathf.Max(light.range * light.range, 0.00001f);
                if(light.lightType == LightType.Spot)
                {
                    Vector4 v = light.localToWorld.GetColumn(2);
                    v.x = -v.x;
                    v.y = -v.y;
                    v.z = -v.z;
                    visibleLightSpotDirections[i] = v;
                    float outerRad = Mathf.Deg2Rad * 0.5f * light.spotAngle;
                    float outerCos = Mathf.Cos(outerRad);
                    float outerTan = Mathf.Tan(outerRad);
                    float innderCos = Mathf.Cos(Mathf.Atan(((64f-18f) / 64f) * outerTan));

                    float angleRange = Mathf.Max(innderCos - outerCos, 0.001f);
                    attenuation.z = 1f / angleRange;
                    attenuation.w = -outerCos * attenuation.z;
                }
            }
            visibleLightAtteuations[i] = attenuation;
        }
        if (cull.visibleLights.Count > maxVisibleLights)
        {


            int[] lightIndices = cull.GetLightIndexMap();
            for (int i = maxVisibleLights; i < cull.visibleLights.Count; i++)
            {
                lightIndices[i] = -1;
            }
            cull.SetLightIndexMap(lightIndices);
        }
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
