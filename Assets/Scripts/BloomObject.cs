using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BloomObject : MonoBehaviour
{
    [Range(0.0f, 5.0f)]
    public float range = 1.0f;

    private int originLayer;

    private void copyParam(BloomObject bo)
    {
        range = bo.range;
    }

    private void Awake()
    {
        // add BloomObject to child
        Transform [] children = GetComponentsInChildren<Transform>(true);
        if(children.Length > 1)
        {
            foreach (Transform child in children)
            {
                if (!child.GetComponent<BloomObject>())
                {
                    BloomObject boInParent = child.GetComponentInParent<BloomObject>();
                    BloomObject bo = child.gameObject.AddComponent<BloomObject>();
                    bo.copyParam(boInParent);
                }
            }
        }

        // save params to renderer's PropertyBlock
        Renderer renderer = GetComponent<Renderer>();
        if(renderer)
        {
            MaterialPropertyBlock materialProperties = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(materialProperties);

            // transfer range[0, 5] to _BloomFactor[0.2f, 1]
            materialProperties.SetFloat("_BloomFactor", range * 0.16f + 0.2f);

            if (renderer.sharedMaterial)
            {
                materialProperties.SetTexture("_BaseMap", renderer.sharedMaterial.GetTexture("_BaseMap"));
                materialProperties.SetColor("_BaseColor", renderer.sharedMaterial.GetColor("_BaseColor"));
            }

            renderer.SetPropertyBlock(materialProperties);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void beforeRender(int bloomLayer)
    {
        originLayer = gameObject.layer;
        gameObject.layer = bloomLayer;
    }

    public void afterRender()
    {
        gameObject.layer = originLayer;
    }
}
