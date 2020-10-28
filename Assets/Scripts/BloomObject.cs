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
            materialProperties.SetFloat("_BloomRange", range);
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
