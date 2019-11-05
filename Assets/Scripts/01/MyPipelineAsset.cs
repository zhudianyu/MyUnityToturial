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
    // Start is called before the first frame update
    void Start()
    {
        
    }
    protected override IRenderPipeline InternalCreatePipeline()
    {
        return new MyPippeline(dynamicBatching,instancing);
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
