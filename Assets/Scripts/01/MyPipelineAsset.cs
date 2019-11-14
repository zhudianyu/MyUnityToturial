using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental;
using UnityEngine.Experimental.Rendering;

[CreateAssetMenu(menuName = "Rendering/My Pipeline")]
public class MyPipelineAsset : RenderPipelineAsset
{
    [SerializeField]
    bool dynamicBatching;

    [SerializeField]
    bool instancing;

    public enum ShadowMapSize {
        _256 = 256,
        _512 = 512,
        _1024 = 1024,
        _2048 = 2048,
        _4096 = 4096
    }
    
    public enum ShadowCascades {
        Zero = 0,
        Two = 2,
        Four = 4
    }

    
    [SerializeField, HideInInspector]
    float twoCascadesSplit = 0.25f;

    [SerializeField, HideInInspector]
    Vector3 fourCascadesSplit = new Vector3(0.067f, 0.2f, 0.467f);
    
    [SerializeField]
    float shadowDistance = 100f;

    [SerializeField] private ShadowMapSize shadowMapSize = ShadowMapSize._1024;

    
    [SerializeField]
    ShadowCascades shadowCascades = ShadowCascades.Four;
    void Start()
    {
        
    }
    protected override IRenderPipeline InternalCreatePipeline()
    {
        Vector3 shadowCascadeSplit = shadowCascades == ShadowCascades.Four ?
            fourCascadesSplit : new Vector3(twoCascadesSplit, 0f);
        return new MyPipeline(
            dynamicBatching, instancing, (int)shadowMapSize, shadowDistance,
            (int)shadowCascades, shadowCascadeSplit
        );
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
