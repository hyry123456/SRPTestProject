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
        /// �洢�õ��б���Ϊ��˳���ȡ��
        /// ��Ϊ������Ҫ�����ɾ��������ʹ������
        /// </summary>
        private LinkedList<GPUPipelineBase> clustBases;

        /// <summary>
        /// ����һ����Ҫ������Ⱦ�Ķ��󣬲����ͻ�����������
        /// </summary>
        /// <param name="clustBase">����Ķ���</param>
        public void InsertRender(GPUPipelineBase clustBase)
        {
            if (clustBases == null)
                clustBases = new LinkedList<GPUPipelineBase>();
            if (clustBase == null) return;
            clustBases.AddLast(clustBase);
        }

        public void RemoveRender(GPUPipelineBase clustBase)
        {
            if (clustBase == null) return;
            clustBases.Remove(clustBase);
        }

        /// <summary>
        /// �������м����е�ClustBase��ͨ����������вü��ĺ�����ʹ�����أ�������ʹ�ú���ֻ��һ����
        /// ������䣬����֮����չҲ����
        /// </summary>
        public void DrawClustData(ScriptableRenderContext context,
            CommandBuffer buffer, ClustDrawType clustDrawSubPass, Camera camera)
        {
            if (clustBases == null) return;
            foreach(GPUPipelineBase index in clustBases)
            {
                index.DrawClustByCamera(context, buffer, clustDrawSubPass, camera);
            }
        }

        /// <summary>
        /// �������м����е�ClustBase��ͨ��������вü��ĺ�����ʹ�����أ�������ʹ�ú���ֻ��һ����
        /// ������䣬����֮����չҲ����
        /// </summary>
        public void DrawClustData(ScriptableRenderContext context,
            CommandBuffer buffer, ClustDrawType clustDrawSubPass, Matrix4x4 projectMatrix)
        {
            if (clustBases == null) return;
            foreach (GPUPipelineBase index in clustBases)
            {
                index.DrawClustByProjectMatrix(context, buffer,
                    clustDrawSubPass, projectMatrix);
            }
        }


    }
}