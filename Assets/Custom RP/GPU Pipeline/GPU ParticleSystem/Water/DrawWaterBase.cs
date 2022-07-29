using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRP.GPUPipeline
{
    public enum WaterDrawMode
    {
        Width = 0,
        DepthAndNormal = 1,
    }

    public abstract class DrawWaterBase : MonoBehaviour
    {
        public abstract void DrawWater(ScriptableRenderContext context,
            CommandBuffer buffer, WaterDrawMode drawType);

        protected void ExecuteBuffer(ref CommandBuffer buffer, ScriptableRenderContext context)
        {
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }


    }
}