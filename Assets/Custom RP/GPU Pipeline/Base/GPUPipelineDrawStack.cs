using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRP.GPUPipeline
{

    /// <summary>
    /// �Զ�����Ⱦ���ߵĻ���ջ����������һ��CPU��������
    /// ������ô��뵽ִ��ջ�е�Clust���л��ƣ�Ҳ����������಻����ֱ�ӵĻ��ƣ�
    /// ֻ�ǵ��ö�Ӧ����Ⱦ��������
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
        /// �洢�õ��б���Ϊ��˳���ȡ����Ϊ������Ҫ�����ɾ��������ʹ������
        /// �����һ������ʹ�õ�ջ��Ҳ���Ƿ�͸������
        /// </summary>
        private LinkedList<GPUPipelineBase> queueStack;

        /// <summary>        /// ͸��ջ        /// </summary>
        private LinkedList<GPUPipelineBase> transferStack;

        /// <summary>
        /// ����һ����Ҫ������Ⱦ�Ķ��󣬲����ͻ�����������
        /// </summary>
        /// <param name="clustBase">����Ķ���</param>
        public void InsertRender(GPUPipelineBase clustBase, bool isTansfer)
        {
            if (clustBase == null) return;
            //͸������
            if (isTansfer)
            {
                if(transferStack == null)
                    transferStack = new LinkedList<GPUPipelineBase>();
                transferStack.AddLast(clustBase);
            }
            //��͸������
            else
            {
                if (queueStack == null)
                    queueStack = new LinkedList<GPUPipelineBase>();
                queueStack.AddLast(clustBase);
            }
        }

        /// <summary>        /// �Ƴ�����Ⱦջ        /// </summary>
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
        /// �������м����е�ClustBase��ͨ����������вü��ĺ�����ʹ�����أ�������ʹ�ú���ֻ��һ����
        /// ������䣬����֮����չҲ���㣬ע�����һ������������͸�����Ƿ�͸������ջ
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
        /// �������м����е�ClustBase��ͨ��������вü��ĺ�����ʹ�����أ�������ʹ�ú���ֻ��һ����
        /// ������䣬����֮����չҲ����
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