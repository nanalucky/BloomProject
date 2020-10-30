﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum RenderFace
{
    Front = 2,
    Back = 1,
    Both = 0
};


[ExecuteInEditMode]
public class BloomObject : MonoBehaviour
{
    public bool fromParent = false;

    [Range(0.0f, 5.0f)]
    public float range = 1.0f;
    public Vector4 textureMultiplierRGBA = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
    public RenderFace renderFace = RenderFace.Front;
 

    private void Awake()
    {
        UpdateRange();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void copyParam(BloomObject bo)
    {
        range = bo.range;
        textureMultiplierRGBA = bo.textureMultiplierRGBA;
        renderFace = bo.renderFace;
    }

    void UpdateRange()
    {
        // add BloomObject to child
        Transform[] children = GetComponentsInChildren<Transform>(true);
        if (children.Length > 1)
        {
            for(int i = 1; i < children.Length; ++i)
            {
                Transform child = children[i];
                BloomObject bo = child.GetComponent<BloomObject>();
                if(!bo)
                {
                    bo = child.gameObject.AddComponent<BloomObject>();
                    bo.fromParent = true;
                }

                if (bo && bo.fromParent)
                {
                    BloomObject boInParent = child.parent.GetComponentInParent<BloomObject>();
                    bo.copyParam(boInParent);
                }
            }
        }

        // save params to renderer's PropertyBlock
        Renderer renderer = GetComponent<Renderer>();
        if (renderer)
        {
            MaterialPropertyBlock materialProperties = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(materialProperties);

            // transfer range[0, 5] to _BloomFactor[0.2f, 1]
            materialProperties.SetFloat("_BloomFactor", range * 0.16f + 0.2f);
            materialProperties.SetVector("_BaseMapMultiplier", textureMultiplierRGBA);
            materialProperties.SetFloat("_Cull", (float)renderFace);

            if (renderer.sharedMaterial)
            {
                if(renderer.sharedMaterial.HasProperty("_BaseMap"))
                    materialProperties.SetTexture("_BaseMap", renderer.sharedMaterial.GetTexture("_BaseMap"));
                if(renderer.sharedMaterial.HasProperty("_BaseColor"))
                    materialProperties.SetColor("_BaseColor", renderer.sharedMaterial.GetColor("_BaseColor"));
            }

            renderer.SetPropertyBlock(materialProperties);
        }

    }

#if UNITY_EDITOR
    private void Update()
    {
        UpdateRange();
    }
#endif

    private void OnDisable()
    {
        range = 1.0f;
        UpdateRange();
    }
}
