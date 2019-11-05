//*************************************************************************
//	创建日期:	2019/11/5 16:23:17
//	文件名称:	InstanceColor
//  创 建 人:   zhudianyu	
//  Email   :   1462415060@qq.com
//	版权所有:	
//	说    明:	
//*************************************************************************
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;


class InstanceColor : MonoBehaviour
{
    [SerializeField]
    Color color = Color.white;

    static MaterialPropertyBlock propertyBlock;

    static int colorID = Shader.PropertyToID("_Color");
    private void Awake()
    {
        OnValidate();
    }
    private void OnValidate()
    {
        if(propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }
        propertyBlock.SetColor(colorID, color);
        GetComponent<MeshRenderer>().SetPropertyBlock(propertyBlock);
    }
}

