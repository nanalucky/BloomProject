﻿using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal
{
    public class BloomObjectInterface: MonoBehaviour
    {
        int originLayer;

        public void beforeRender(int bloomMaskLayer)
        {
            originLayer = gameObject.layer;
            gameObject.layer = bloomMaskLayer;
        }

        public void afterRender()
        {
            gameObject.layer = originLayer;
        }
    }


    public class BloomMaskPass : ScriptableRenderPass
    {
        public static string bloomMaskLayerName = "BloomMask";
        FilteringSettings m_FilteringSettingsOpaque;
        FilteringSettings m_FilteringSettingsTransparent;
        const string m_ProfilerTag = "Bloom Mask";
        ProfilingSampler m_ProfilingSampler;

        Material m_BloomMaskMaterial;

        List<ShaderTagId> m_ShaderTagIdList = new List<ShaderTagId>();

        RenderStateBlock m_RenderStateBlock;
        RenderTargetHandle m_Destination;
        RenderTextureDescriptor m_RenderTextureDescriptor;

        public BloomMaskPass(string[] shaderTags, PostProcessData data)
        {
            m_ProfilingSampler = new ProfilingSampler(m_ProfilerTag);
            this.renderPassEvent = RenderPassEvent.AfterRenderingSkybox;

            Material Load(Shader shader)
            {
                if (shader == null)
                {
                    Debug.LogError($"Missing shader. {GetType().DeclaringType.Name} render pass will not execute. Check for missing reference in the renderer resources.");
                    return null;
                }

                return CoreUtils.CreateEngineMaterial(shader);
            }

            m_BloomMaskMaterial = Load(data.shaders.bloomMaskPS);
            m_FilteringSettingsOpaque = new FilteringSettings(RenderQueueRange.opaque, LayerMask.GetMask(bloomMaskLayerName));
            m_FilteringSettingsTransparent = new FilteringSettings(RenderQueueRange.transparent, LayerMask.GetMask(bloomMaskLayerName));

            if (shaderTags != null && shaderTags.Length > 0)
            {
                foreach (var passName in shaderTags)
                    m_ShaderTagIdList.Add(new ShaderTagId(passName));
            }
            else
            {
                m_ShaderTagIdList.Add(new ShaderTagId("SRPDefaultUnlit"));
                m_ShaderTagIdList.Add(new ShaderTagId("UniversalForward"));
                m_ShaderTagIdList.Add(new ShaderTagId("LightweightForward"));
            }

            m_RenderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
        }

        public void Setup(RenderTextureDescriptor baseDescriptor, RenderTargetHandle destination)
        {
            m_RenderTextureDescriptor = baseDescriptor;
            m_RenderTextureDescriptor.depthBufferBits = 0;
            m_RenderTextureDescriptor.msaaSamples = 1;
            m_RenderTextureDescriptor.graphicsFormat = RenderingUtils.SupportsGraphicsFormat(GraphicsFormat.R8_UNorm, FormatUsage.Linear | FormatUsage.Render)
                ? GraphicsFormat.R8_UNorm
                : GraphicsFormat.B8G8R8A8_UNorm;

            m_Destination = destination;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            cmd.GetTemporaryRT(m_Destination.id, m_RenderTextureDescriptor, FilterMode.Bilinear);

            RenderTargetIdentifier identifier = m_Destination.Identifier();
            ConfigureTarget(identifier);
            ConfigureClear(ClearFlag.All, Color.black);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            int layer = LayerMask.NameToLayer(bloomMaskLayerName);
            BloomObjectInterface[] maskObjects = Object.FindObjectsOfType<BloomObjectInterface>();
            if(maskObjects.Length > 0)
            {
                foreach (BloomObjectInterface obj in maskObjects)
                    obj.beforeRender(layer);
            }

            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                ref CameraData cameraData = ref renderingData.cameraData;

                SortingCriteria sortingCriteriaOpaque = renderingData.cameraData.defaultOpaqueSortFlags;
                DrawingSettings drawingSettings = CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, sortingCriteriaOpaque);
                drawingSettings.overrideMaterial = m_BloomMaskMaterial;
                drawingSettings.overrideMaterialPassIndex = 0;
                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref m_FilteringSettingsOpaque,
                    ref m_RenderStateBlock);

                SortingCriteria sortingCriteriaTransparent = SortingCriteria.CommonTransparent;
                drawingSettings = CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, sortingCriteriaTransparent);
                drawingSettings.overrideMaterial = m_BloomMaskMaterial;
                drawingSettings.overrideMaterialPassIndex = 1;
                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref m_FilteringSettingsTransparent,
                    ref m_RenderStateBlock);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            if (maskObjects.Length > 0)
            {
                foreach (BloomObjectInterface obj in maskObjects)
                    obj.afterRender();
            }
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
                throw new ArgumentNullException("cmd");

            cmd.ReleaseTemporaryRT(m_Destination.id);
        }
    }
}
