using System.Collections.Generic;
using UnityEngine;

//输入缓冲队列：缓存玩家在动画期间的按键输入
public class InputBuffer
{
    private Queue<string> buffer = new Queue<string>();
    private float bufferWindow = 0.3f;  //输入有效窗口（秒）
    private Dictionary<string, float> inputTimestamps = new Dictionary<string, float>();

    //缓存一个输入
    public void BufferInput(string inputName)
    {
        //如果队列中已经有相同输入，只更新时间戳，不重复入队
        if (buffer.Contains(inputName))
        {
            inputTimestamps[inputName] = Time.time;
            return;
        }

        buffer.Enqueue(inputName);
        inputTimestamps[inputName] = Time.time;
    }

    //尝试消费一个输入（如果有且未过期）
    public bool ConsumeInput(string inputName, bool ignoreExpiry = false)
    {
        if (!buffer.Contains(inputName))
            return false;

        //检查是否过期（如果要求检查的话）
        if (!ignoreExpiry && inputTimestamps.TryGetValue(inputName, out float timestamp))
        {
            if (Time.time - timestamp > bufferWindow)
            {
                RemoveInput(inputName);
                return false;
            }
        }

        RemoveInput(inputName);
        return true;
    }

    //清空所有缓冲
    public void Clear()
    {
        buffer.Clear();
        inputTimestamps.Clear();
    }

    //检查是否有待处理的输入
    public bool HasInput(string inputName)
    {
        if (!buffer.Contains(inputName)) return false;

        if (inputTimestamps.TryGetValue(inputName, out float timestamp))
        {
            return Time.time - timestamp <= bufferWindow;
        }
        return false;
    }

    private void RemoveInput(string inputName)
    {
        //重建队列，移除指定输入
        Queue<string> newBuffer = new Queue<string>();
        while (buffer.Count > 0)
        {
            string item = buffer.Dequeue();
            if (item != inputName)
                newBuffer.Enqueue(item);
        }
        buffer = newBuffer;
        inputTimestamps.Remove(inputName);
    }
}