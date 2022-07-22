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
            //还没有读取的文件内容长度
            long leftLength = file.Length;
            //创建接收文件内容的字节数组
            byte[] buffer = new byte[1024];
            //每次读取的最大字节数
            int maxLength = buffer.Length;
            //每次实际返回的字节数长度
            int num = 0;
            //文件开始读取的位置
            int fileStart = 0;

            while (leftLength > 0)
            {
                //设置文件流的读取位置，一开始从0位置读取
                file.Position = fileStart;

                // 当读取的位置没有缓存buff的最后
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

                // 读取的游标往后移动
                fileStart += num;
                // 需要读取的长度减少
                leftLength -= num;

                str.Append(Encoding.Default.GetString(buffer));
                // 坑在这里，如果最后一次读取的长度没有超过1024，则buffer中会残留上一次读取的内容，导致            
                //最后获取的内容出错，所以每次读取后都要清空数组，使用Array.Clear方法
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
                if(i > 1) stringBuilder.Remove(0, i - 1);     //移除已经处理过的数据
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
