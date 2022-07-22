using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRP.GPUPipeline
{

    /// <summary>
    /// 自定义渲染管线的绘制栈，用于类似一般CPU的批处理，
    /// 逐个调用传入到执行栈中的Clust进行绘制，也就是所这个类不进行直接的绘制，
    /// 只是调用对应的渲染方法而已
    /// </summary>
    public class GPUPipelineDrawStack
    {
        private GPUPipelineDrawStack(){ }

        private static GPUPipelineDrawStack instance;
        public static GPUPipelineDrawStack Instance
        {
            get
            {
                if(instance == null) instance = new GPUPipelineDrawStack();
                return instance;
            }
        }

        /// <summary>
        /// 存储用的列表，因为是顺序读取，因为经常需要插入和删除，所以使用链表
        /// 这个是一般物体使用的栈，也就是非透明物体
        /// </summary>
        private LinkedList<GPUPipelineBase> queueStack;

        /// <summary>        /// 透明栈        /// </summary>
        private LinkedList<GPUPipelineBase> transferStack;

        /// <summary>
        /// 插入一个需要进行渲染的对象，插入后就会正常绘制了
        /// </summary>
        /// <param name="clustBase">插入的对象</param>
        public void InsertRender(GPUPipelineBase clustBase, bool isTansfer)
        {
            if (clustBase == null) return;
            //透明物体
            if (isTansfer)
            {
                if(transferStack == null)
                    transferStack = new LinkedList<GPUPipelineBase>();
                transferStack.AddLast(clustBase);
            }
            //非透明物体
            else
            {
                if (queueStack == null)
                    queueStack = new LinkedList<GPUPipelineBase>();
                queueStack.AddLast(clustBase);
            }
        }

        /// <summary>        /// 移除出渲染栈        /// </summary>
        public void RemoveRender(GPUPipelineBase clustBase, bool isTansfer)
        {
            if (clustBase == null) return;
            if (isTansfer)
            {
                transferStack.Remove(clustBase);
            }
            else
                queueStack.Remove(clustBase);
        }

        /// <summary>
        /// 调用所有加载中的ClustBase的通过摄像机进行裁剪的函数，使用重载，看见的使用函数只有一个，
        /// 方便记忆，而且之后拓展也方便，注意最后一个参数，调用透明还是非透明处理栈
        /// </summary>
        public void DrawClustData(ScriptableRenderContext context,
            CommandBuffer buffer, ClustDrawType clustDrawSubPass, Camera camera, bool isGeometry)
        {
            if (isGeometry)
            {
                if (queueStack == null) return;
                foreach (GPUPipelineBase index in queueStack)
                {
                    index.DrawClustByCamera(context, buffer, clustDrawSubPass, camera);
                }
            }
            else
            {
                if (transferStack == null) return;
                foreach(GPUPipelineBase index in transferStack)
                {
                    index.DrawClustByCamera(context, buffer, clustDrawSubPass, camera);
                }
            }
        }

        /// <summary>
        /// 调用所有加载中的ClustBase的通过矩阵进行裁剪的函数，使用重载，看见的使用函数只有一个，
        /// 方便记忆，而且之后拓展也方便
        /// </summary>
        public void DrawClustData(ScriptableRenderContext context, CommandBuffer buffer,
             ClustDrawType clustDrawSubPass, Matrix4x4 projectMatrix, bool isGeometry)
        {
            if (isGeometry)
            {
                if (queueStack == null) return;
                foreach (GPUPipelineBase index in queueStack)
                {
                    index.DrawClustByProjectMatrix(context, buffer,
                        clustDrawSubPass, projectMatrix);
                }
            }
            else
            {
                if (transferStack == null) return;
                foreach (GPUPipelineBase index in transferStack) 
                {
                    index.DrawClustByProjectMatrix(context, buffer,
                        clustDrawSubPass, projectMatrix);
                }
            }

        }


    }
}