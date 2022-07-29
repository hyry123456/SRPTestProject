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
        /// 插入一个需要进行渲染的对象，插入后就会正常绘制了
        /// </summary>
        /// <param name="clustBase">插入的对象</param>
        public void InsertRender(DrawWaterBase waterBase)
        {
            if (waterBase == null) return;
            //透明物体
            if (waterStack == null)
                waterStack = new LinkedList<DrawWaterBase>();
            waterStack.AddLast(waterBase);
        }

        /// <summary>        /// 移除出渲染栈        /// </summary>
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