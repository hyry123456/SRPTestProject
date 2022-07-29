using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class AsyncLoad : MonoBehaviour
{
    private static AsyncLoad instance;
    public static AsyncLoad Instance
    {
        get 
        {
            if(instance == null)
            {
                GameObject gameObject = new GameObject("AsyncLoad");
                gameObject.AddComponent<AsyncLoad>();
                //����ᵼ��һ��ʼ�͹رոö������Բ����ø�����
                //gameObject.hideFlags = HideFlags.HideAndDontSave;     
                DontDestroyOnLoad(gameObject);
            }
            return instance; 
        }
    }

    /// <summary>    /// �ȴ�ջ��������������Ҫ�����ί�д洢����λ�ã������ǵȴ���    /// </summary>
    private static List<Action> commands = new List<Action>();
    /// <summary>    /// ����ջ�������߳��ж�ȡ���б����ݣ�Ȼ���������    /// </summary>
    private List<Action> localCommands = new List<Action> ();
    private AutoResetEvent resetEvent;
    private Thread thread;
    private bool isRunning;

    public bool IsRunning
    {
        get { return isRunning; }
    }

    private void Awake()
    {
        if(instance != null)
        {

            Destroy(gameObject);
            return;
        }
        instance = this;
        isRunning = true;
        resetEvent = new AutoResetEvent(false);
        thread = new Thread(Run);
        thread.Start();

    }

    public void Run()
    {

        while (isRunning)
        {
            resetEvent.WaitOne();

            lock (commands)     //�߳�������ֹ�첽���²������
            {
                localCommands.AddRange(commands);
                commands.Clear();
            }
            //������������
            foreach(var i in localCommands)
            {
                i();
            }
            localCommands.Clear();
        }
    }

    public void AddAction(Action action)
    {
        resetEvent.Set();

        lock (commands)
        {
            commands.Add(action);
        }
    }


    private void OnDisable()
    {
        isRunning = false;
    }
}
