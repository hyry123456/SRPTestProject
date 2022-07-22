using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class TestReady : MonoBehaviour
{
    void Start()
    {
        string path = Application.streamingAssetsPath + "/ClustData/ClustTest.clustData";

        StringBuilder str = new StringBuilder();

        using (FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            //��û�ж�ȡ���ļ����ݳ���
            long leftLength = file.Length;
            //���������ļ����ݵ��ֽ�����
            byte[] buffer = new byte[1024];
            //ÿ�ζ�ȡ������ֽ���
            int maxLength = buffer.Length;
            //ÿ��ʵ�ʷ��ص��ֽ�������
            int num = 0;
            //�ļ���ʼ��ȡ��λ��
            int fileStart = 0;

            while (leftLength > 0)
            {
                //�����ļ����Ķ�ȡλ�ã�һ��ʼ��0λ�ö�ȡ
                file.Position = fileStart;

                // ����ȡ��λ��û�л���buff�����
                if (leftLength < maxLength)
                {
                    num = file.Read(buffer, 0, Convert.ToInt32(leftLength));
                }
                else
                {
                    num = file.Read(buffer, 0, maxLength);
                }

                if (num == 0)
                {
                    break;
                }

                // ��ȡ���α������ƶ�
                fileStart += num;
                // ��Ҫ��ȡ�ĳ��ȼ���
                leftLength -= num;

                str.Append(Encoding.Default.GetString(buffer));
                // �������������һ�ζ�ȡ�ĳ���û�г���1024����buffer�л������һ�ζ�ȡ�����ݣ�����            
                //����ȡ�����ݳ�������ÿ�ζ�ȡ��Ҫ������飬ʹ��Array.Clear����
                Array.Clear(buffer, 0, 1024);
                ReadyStringBuilder(ref str);
            }
        }
        //Debug.Log(str.ToString());

    }

    private void ReadyStringBuilder(ref StringBuilder stringBuilder)
    {
        List<string> list = new List<string>();
        string str = stringBuilder.ToString();
        int i;
        for( i=str.IndexOf('<'); i < str.Length && i != -1;)
        {
            int next = str.IndexOf('>', i);
            if(next == -1)
            {
                if(i > 1) stringBuilder.Remove(0, i - 1);     //�Ƴ��Ѿ������������
                break;
            }

            if(next -1 - i > 1)
            {
                string str2 = str.Substring(i + 1, next - 1 - i);
                list.Add(str2);
            }
            i = str.IndexOf('<', next);

        }

        foreach(string index in list)
        {
            //Debug.Log(index);
        }
    }

}
