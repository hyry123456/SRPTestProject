using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRP.Clust
{

    /// <summary>
    /// �Զ�����Ⱦ���ߵĻ���ջ����������һ��CPU��������
    /// ������ô��뵽ִ��ջ�е�Clust���л��ƣ�Ҳ����������಻����ֱ�ӵĻ��ƣ�
    /// ֻ�ǵ��ö�Ӧ����Ⱦ��������
    /// </summary>
    public class ClustDrawStack
    {
        private ClustDrawStack(){ }

        private static ClustDrawStack instance;
        public static ClustDrawStack Instance
        {
            get
            {
                if(instance == null) instance = new ClustDrawStack();
                return instance;
            }
        }

        /// <summary>
        /// �洢�õ��б���Ϊ��˳���ȡ��
        /// ��Ϊ������Ҫ�����ɾ��������ʹ������
        /// </summary>
        private LinkedList<ClustBase> clustBases;

        /// <summary>
        /// ����һ��ClustBase���󣬲����ͻ�����������
        /// </summary>
        /// <param name="clustBase">����Ķ���</param>
        public void InsertClustBase(ClustBase clustBase)
        {
            if (clustBases == null)
                clustBases = new LinkedList<ClustBase>();
            if (clustBase == null) return;
            clustBases.AddLast(clustBase);
        }

        public void RemoveClustBase(ClustBase clustBase)
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
            foreach(ClustBase index in clustBases)
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
            foreach (ClustBase index in clustBases)
            {
                index.DrawClustByProjectMatrix(context, buffer,
                    clustDrawSubPass, projectMatrix);
            }
        }


    }
}