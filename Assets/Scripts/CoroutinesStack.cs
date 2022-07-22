using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoroutinesStack : MonoBehaviour
{
    private static CoroutinesStack instance;
    public static CoroutinesStack Instance
    {
        get
        {
            if(instance == null)
            {
                GameObject gameObject = new GameObject("AsyncLoad");
                gameObject.AddComponent<CoroutinesStack>();     
                DontDestroyOnLoad(gameObject);
            }
            return instance;
        }
    }

    /// <summary>    /// 携程栈的插入结构体，将工作分开到多帧执行    /// </summary>
    public delegate bool CoroutinesAction();

    private Stack<CoroutinesAction> readyList = new Stack<CoroutinesAction>();
    private Stack<CoroutinesAction> conductList = new Stack<CoroutinesAction>();

    private bool isRunning;

    private void Awake()
    {
        if(instance != null)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        isRunning = true;
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        while (isRunning)
        {
            lock (readyList)
            {
                while(readyList.Count > 0)
                {
                    conductList.Push(readyList.Pop());
                }
            }

            while(conductList.Count > 0)
            {
                if (conductList.Peek()())
                {
                    conductList.Pop();
                }
                yield return null;
            }
            yield return new WaitForSeconds(1f);
        }
    }

    private void OnDisable()
    {
        isRunning = false;
    }

    public void AddReadyAction(CoroutinesAction action)
    {
        readyList.Push(action);
    }
}
