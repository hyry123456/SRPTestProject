using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// �ļ��������࣬��������һЩͨ�ü򵥵��ļ�����
/// </summary>
public static class FileFuctions 
{
    public static void WriteFile(string path, string content)
    {
        if (File.Exists(path))
        {
            File.WriteAllText(path, content);
            return;
        }
        else
        {
            File.Create(path).Dispose();
            File.WriteAllText(path, content);
        }
    }

    public static void AppandWriteFile(string path, string appendContent)
    {
        if (File.Exists(path))
        {
            File.AppendAllText(path, appendContent);
            return;
        }
        else
        {
            File.Create(path).Dispose();
            File.AppendAllText(path, appendContent);
        }
    }


    public static string LoadAllStr(string path)
    {
        if (File.Exists(path))
        {
            return File.ReadAllText(path);
        }
        return null;
    }

    public static List<string> ClipByAngleBrackets(string allStr)
    {
        List<string> list = new List<string>();
        if (allStr == null) return null;
        for (int i = allStr.IndexOf('<'); i < allStr.Length && i != -1;)
        {
            //����������������
            int next = allStr.IndexOf('>', i);
            //��������д洢����Ϣ
            if ((i + 1) >= (next - 1))
            {
                i = allStr.IndexOf('<', next);
                list.Add("");
                continue;
            }
            string str = allStr.Substring(i + 1, next - 1 - i);
            if (next - 1 - i > 1)      //û�����ݾͲ�����
                list.Add(str);
            i = allStr.IndexOf('<', next);
        }
        return list;
    }
}
