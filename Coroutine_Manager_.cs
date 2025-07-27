using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 协程枚举类型
/// </summary>
public enum CoroutineType
{
    // 定义协程类型
    Type1,
    Type2,
    // 可以根据需要添加更多类型
}

/// <summary>
/// 协程管理器
/// </summary>
public class Coroutine_Manager_ : Singleton_Mono_<Coroutine_Manager_>
{
    /// <summary>
    /// 协程的封装类型
    /// </summary>
    private class CoroutineWrapper
    {
        public Coroutine Coroutine;// 协程句柄
        public string CoroutineName; // 协程名称
        public bool IsRunning;// 是否正在运行的标志
        public CoroutineType CoroutineType; // 协程类型
        public string CoroutineID; // 利用C#生成的唯一标识符作为协程ID

        public CoroutineWrapper()
        {
            IsRunning = false; // 默认协程不在运行状态
            CoroutineName = string.Empty; // 默认协程名称为空
            CoroutineID = Guid.NewGuid().ToString(); // 生成唯一标识符
        }
    }

    private Dictionary<CoroutineType, HashSet<CoroutineWrapper>> coroutineMap = new(); // 协程缓存字典
    private readonly object Locked_ = new object();//唯一锁对象
    /// <summary>
    /// 启动一个协程，并将其添加至指定的标签组
    /// </summary>
    /// <param name="routine">协程句柄</param>
    /// <param name="cType">协程标签</param>
    /// <param name="callBack">运行协程完毕后的回调函数</param>
    /// <param name="invokeCallbackOnSuccessOnly">是否需要在协程运行成功后才调用回调函数</param>
    /// <returns>协程句柄</returns>
    public Coroutine StartManagedCoroutine(Func<IEnumerator> routineFactory, CoroutineType cType, UnityAction callBack = null, bool invokeCallbackOnSuccessOnly = true)
    {
        // 若协程为空，则直接退出并返回null
        if (routineFactory == null)
        {
            Debug.LogError("协程函数不能为空");
            return null;
        }

        var routine = routineFactory();//生成新的实例

        //创建一个用于封装协程的对象
        var wrapper = new CoroutineWrapper
        {
            IsRunning = true,// 设置协程正在运行的标志
            CoroutineType = cType,// 设置协程类型
            CoroutineName = routine.GetType().Name // 设置协程的名称
        };

        //使用线程同步的安全操作
        lock (Locked_)
        {
            //将封装对象添加到字典中
            if (!coroutineMap.TryGetValue(cType, out var set))
            {
                set = new();
                coroutineMap[cType] = set;
            }
            set.Add(wrapper);
        }

        // 启动协程，并将协程句柄返回给封装协程对象
        var cor = StartCoroutine(WrapCoroutine(routine, cType, wrapper, invokeCallbackOnSuccessOnly, callBack));
        wrapper.Coroutine = cor;

        //返回协程句柄
        return cor;
    }

    /// <summary>
    /// 封装协程运行逻辑的迭代器
    /// </summary>
    /// <param name="routine">对应的协程句柄</param>
    /// <param name="cType">协程类型</param>
    /// <param name="wrapperCoroutine">封装</param>
    /// <param name="invokeCallbackOnSuccessOnly">是否需要在协程运行成功时才执行回调函数</param>
    /// <param name="callBack">协程运行完毕后的回调函数</param>
    /// <returns>协程迭代器</returns>
    private IEnumerator WrapCoroutine(IEnumerator routine, CoroutineType cType, CoroutineWrapper wrapperCoroutine, bool invokeCallbackOnSuccessOnly, UnityAction callBack = null)
    {
        //等待一帧，确保Wrapper.Coroutine被正确初始化
        yield return null;

        //表示协程的回调函数是否能被正常执行的标志
        bool errorOccurred = true;

        try
        {
            //等待协程运行完成
            yield return routine;
            //协程正常结束
            errorOccurred = false;
        }
        finally
        {
            //协程运行完成后，将协程标记为不在运行当中
            wrapperCoroutine.IsRunning = false;

            //添加线程同步
            lock (Locked_)
            {
                //如果该标签下的协程集合为空，则从字典中移除该标签
                if (coroutineMap.TryGetValue(cType, out var set))
                {
                    //从集合中移除该封装对象
                    if (set.Remove(wrapperCoroutine) && set.Count == 0)
                    {
                        coroutineMap.Remove(cType);
                    }
                }
                //如果协程运行过程中发生了错误
                if (errorOccurred)
                {
                    Debug.Log($"协程时出现错误");

                    // 如果不需要在协程成功后才调用回调函数，则调用回调函数
                    if (!invokeCallbackOnSuccessOnly)
                        callBack?.Invoke();
                }
                //协程执行成功时调用回调函数
                else
                {
                    callBack.Invoke();
                }
            }
        }
    }

    /// <summary>
    /// 停止并移除特定的协程封装对象
    /// </summary>
    /// <param name="wrapper">协程封装对象</param>
    private void StopSpecificWrapper(CoroutineWrapper wrapper)
    {
        // 如果wrapper为null,则直接返回
        if (wrapper == null) return;

        // 清除协程句柄
        if (wrapper.Coroutine != null)
        {
            StopCoroutine(wrapper.Coroutine); // 停止协程
            wrapper.Coroutine = null; // 清除协程句柄
            wrapper.IsRunning = false; // 标记为不在运行状态
        }
    }

    /// <summary>
    /// 从指定标签组中移除一个协程封装对象
    /// </summary>
    /// <param name="cType">协程的类型</param>
    /// <param name="set">协程封装的容器</param>
    /// <param name="wrapper">协程封装对象</param>
    private void RemoveSpecificWrapper(CoroutineType cType, HashSet<CoroutineWrapper> set, CoroutineWrapper wrapper)
    {
        // 如果wrapper为null,则直接返回
        if (wrapper == null || set == null) return;

        // 从集合中移除该封装对象
        if (set.Remove(wrapper) && set.Count == 0)
            coroutineMap.Remove(cType);

        // 输出调试信息
        Debug.Log($"协程ID: {wrapper.CoroutineID}，协程名称: {wrapper.CoroutineName}，已被非自身停止并移除");
    }

    /// <summary>
    /// 停止并移除某个特定标签下的指定协程
    /// </summary>
    /// <param name="cType">协程类型</param>
    /// <param name="coroutine">协程句柄</param>
    public void StopSpecificCoroutine(CoroutineType cType, Coroutine coroutine)
    {
        // 如果协程句柄为空，则直接返回
        lock (Locked_)
        {
            // 尝试在coroutineMap中获取指定标签的协程集合
            if (!coroutineMap.TryGetValue(cType, out var set)) return;

            // 线程遍历安全
            CoroutineWrapper targetWrapper = null;
            
            // 使用LINQ查找指定协程句柄的封装对象
            targetWrapper = set.FirstOrDefault(w => w.Coroutine == coroutine);
            
            // 尝试停止并移除特定的协程封装对象
            StopSpecificWrapper(targetWrapper);

            // 尝试移除该封装对象
            RemoveSpecificWrapper(cType, set, targetWrapper);
        }
    }

    /// <summary>
    /// 停止指定标签下的所有协程
    /// </summary>
    /// <param name="cType">协程类型</param>
    public void StopAllCoroutineByType(CoroutineType cType)
    {
        //使用线程同步的安全操作
        lock (Locked_)
        {
            //尝试在coroutineMap中获取指定标签的协程集合
            if (!coroutineMap.TryGetValue(cType, out var set))
            {
                Debug.Log($"没有找到指定标签下的协程: {cType}");
                return;
            }

            //遍历副本对应的协程集合
            foreach (var targetWrapper in set.ToList())
            {
                // 尝试停止并移除特定的协程封装对象
                StopSpecificWrapper(targetWrapper);

                // 尝试移除该封装对象
                RemoveSpecificWrapper(cType, set, targetWrapper);
            }
        }
    }

    /// <summary>
    /// 停止所有正在运行的协程
    /// </summary>
    public void StopAllManagedCoroutines()
    {
        // 如果字典为空，则直接返回
        if (coroutineMap.Count == 0)
            return;

        //使用线程同步的安全操作
        lock (Locked_)
        {
            //遍历字典中的所有标签
            foreach (var cType in coroutineMap.Keys.ToList())
            {
                StopAllCoroutineByType(cType);
            }

            Debug.Log("停止所有正在运行的协程");
        }
    }

    /// <summary>
    /// 检查指定标签下是否有任何协程正在运行
    /// </summary>
    /// <param name="cType">协程类型</param>
    /// <returns>是否有正在运行的协程</returns>
    public bool IsAnyCoroutineRunning(CoroutineType cType)
    {
        //如果字典中没有该标签，则直接返回false 
        if (!coroutineMap.TryGetValue(cType, out var wrapperSet))
            return false;

        //使用副本进行遍历,（HashSet的只读遍历方式是安全的）
        return wrapperSet.Any(wrapper => wrapper.IsRunning);

    }

    /// <summary>
    /// 统计指定协程类型下正在运行的协程数量
    /// </summary>
    /// <param name="cType">协程类型</param>
    /// <returns>指定类型下正在运行的协程数量</returns>
    public int GetRunningCoroutineCount(CoroutineType cType)
    {
        //判断字典中是否存在该标签的协程集合
        if (!coroutineMap.TryGetValue(cType, out var wrapperSet))
        {
            Debug.Log($"没有找到指定标签下的协程: {cType}");
            return 0;
        }

        //筛选出正在运行的协程，并返回其数量（LINQ写法）
        return wrapperSet.Count(wrapper => wrapper.IsRunning);
    }

    /// <summary>
    /// 获取被协程管理器管理的所有协程的总数
    /// </summary>
    /// <returns></returns>
    public int GetTotalRunningCoroutineCount()
    {
        //使用LINQ统计所有标签下正在运行的协程数量
        return coroutineMap.Values.Sum(set => set.Count(wrapper => wrapper.IsRunning));
    }

    /// <summary>
    /// 打印具体的协程信息
    /// </summary>
    /// <param name="cType">协程类型</param> 
    /// <param name="set">协程封装对象的集合</param>
    /// <param name="filterRunningOnly">是否需要筛选出正在运行的协程</param>
    private void OutputCoroutineDetails(CoroutineType cType, HashSet<CoroutineWrapper> set, bool filterRunningOnly = false)
    {
        Debug.Log($"协程类型: {cType}, 协程数量: {set.Count}");

        //遍历每个标签下的协程封装对象
        foreach (var wrapperCoroutine in set)
        {
            //根据是否需要筛选出正在运行的协程进行输出
            if (filterRunningOnly)
            {
                if (wrapperCoroutine.IsRunning)
                {
                    Debug.Log($"协程ID：{wrapperCoroutine.CoroutineID}，协程名称: {wrapperCoroutine.CoroutineName}, 正在运行: {wrapperCoroutine.IsRunning}");
                }
            }
            else
            {
                Debug.Log($"协程ID： {wrapperCoroutine.CoroutineID} ，协程名称: : {wrapperCoroutine.CoroutineName}, 正在运行: {wrapperCoroutine.IsRunning}");
            }
        }
    }

    /// <summary>
    /// 根据对应的协程类型输出正在被管理的协程信息
    /// </summary>
    /// <param name="cType">协程类型</param>
    /// <param name="filterRunningOnly">是否需要筛选出正在运行的协程</param>
    public void LogCoroutineInfoByType(CoroutineType cType, bool filterRunningOnly = false)
    {
        //使用线程同步的安全操作
        lock (Locked_)
        {   
            coroutineMap.TryGetValue(cType, out var set);

            //如果没有找到对应的协程集合，则输出提示信息
            if (set == null)
            {
                Debug.Log($"没有找到指定标签下的协程: {cType}");
                return;
            }

            //输出具体的协程信息
            OutputCoroutineDetails(cType, set, filterRunningOnly);
        }
    }

    /// <summary>
    /// 输出所有正在被管理的协程信息
    /// <paramref name="filterRunningOnly">是否需要筛选出正在运行的</param>
    /// </summary>
    public void LogAllCoroutineInfos(bool filterRunningOnly = false)
    {
        // 如果字典为空，则直接返回
        if (coroutineMap.Count == 0)
        {
            Debug.Log("没有正在运行的协程");
            return;
        }

        Debug.Log("正在运行的协程列表:");

        //使用线程同步的安全操作
        lock (Locked_)
        {   
            foreach (var kvp in coroutineMap)
            {
                //输出具体的协程信息
                OutputCoroutineDetails(kvp.Key, kvp.Value, filterRunningOnly);
            }
        }
    }
}