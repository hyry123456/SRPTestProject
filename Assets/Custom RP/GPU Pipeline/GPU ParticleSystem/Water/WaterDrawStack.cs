using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace CustomRP.GPUPipeline
{
    public class WaterDrawStack
    {
        private WaterDrawStack() { }

        private static WaterDrawStack instance;
        public static WaterDrawStack Instance
        {
            get
            {
                if (instance == null) instance = new WaterDrawStack();
                return instance;
            }
        }

        private LinkedList<DrawWaterBase> waterStack;

        /// <summary>
        /// ����һ����Ҫ������Ⱦ�Ķ��󣬲����ͻ�����������
        /// </summary>
        /// <param name="clustBase">����Ķ���</param>
        public void InsertRender(DrawWaterBase waterBase)
        {
            if (waterBase == null) return;
            //͸������
            if (waterStack == null)
                waterStack = new LinkedList<DrawWaterBase>();
            waterStack.AddLast(waterBase);
        }

        /// <summary>        /// �Ƴ�����Ⱦջ        /// </summary>
        public void RemoveRender(DrawWaterBase waterBase)
        {
            if (waterBase == null) return;
            waterStack.Remove(waterBase);
        }

        public void DrawWaterByMode(ScriptableRenderContext context,
            CommandBuffer buffer, WaterDrawMode waterMode)
        {
            if (waterStack == null) return;
            foreach (DrawWaterBase index in waterStack)
            {
                index.DrawWater(context, buffer, waterMode);
            }
        }
    }
}