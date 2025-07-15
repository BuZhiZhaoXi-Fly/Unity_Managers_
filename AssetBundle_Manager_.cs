using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// AB包管理器
/// </summary>
public class AssetBundle_Manager_ : SingletonAutoMono<AssetBundle_Manager_>
{
    #region 变量
    private Dictionary<string, AssetBundle> abDic = new();//用于管理AB的容器
    private Dictionary<string, UnityEngine.Object> objDic = new();//用于存储被加载资源的容器

    private AssetBundle abMain = null;//主包
    private AssetBundleManifest abMainManifest = null;//主包依赖

    //TODO:这里需要将abPath改成自己的AB包文件夹路径
    private string abPath = Application.streamingAssetsPath;//AB包文件夹路径
    public string ABPath
    {
        set => abPath = value;
        get => abPath;
    }

    //TODO:主包名称,这里可以根据实际情况修改主包名称
    private string ABMainName
    {
        get 
        {
#if UNITY_IOS
    return "IOS";
#elif UNITY_ANDROID
    return "Android";
#else
    return "AB Package";
        }
    }
    #endregion

    #region 同步加载AB包
    /// <summary>
    /// 加载主包以及主包配置文件
    /// </summary>
    private bool LoadMainAB()
    {
        //加载主包
        if (abMain == null)
        {   
            //尝试加载AB包
            string path = Path.Combine(ABPath, ABMainName);
            abMain = AssetBundle.LoadFromFile(path);

            //如果加载失败 则输出错误信息
            if (abMain == null)
            {
                Debug.LogError($"主包加载失败！路径：{path}");
                return false;
            }
        }

        //加载主包的依赖配置文件
        if (abMainManifest == null)
        {
            //尝试加载主包的AssetBundleManifest 
            abMainManifest = abMain.LoadAsset<AssetBundleManifest>("AssetBundleManifest");

            //如果加载失败 则输出错误信息
            if (abMainManifest == null)
            {
                Debug.LogError("主包的AssetBundleManifest加载失败！");
                return false;
            }
        }

        //如果主包和主包配置文件都加载成功 则返回true 
        return true;
    }

    /// <summary>
    /// 加载目标包
    /// </summary>
    /// <param name="targetABName">目标包名称</param>
    /// <returns>目标AB包</returns>
    private AssetBundle LoadTargetAB(string targetABName)
    {
        //如果主包和主包配置文件没有加载成功 则返回null
        if (!LoadMainAB())
        {
            Debug.LogError("主包加载失败，无法加载目标包");
            return null;
        }

        //获取所有依赖的名称
        string[] dependenciesStrs = abMainManifest.GetAllDependencies(targetABName);
        //加载所有依赖
        foreach (string dependencyStr in dependenciesStrs)
        {
            //如果依赖包没有加载过 则加载依赖包
            if (!abDic.ContainsKey(dependencyStr))
            {
                //尝试加载依赖包
                var dependencyAB = AssetBundle.LoadFromFile(Path.Combine(ABPath, dependencyStr));
                //如果加载失败 则输出错误信息
                if (dependencyAB == null)
                {
                    Debug.LogError($"依赖包加载失败！路径：{Path.Combine(ABPath, dependencyStr)}");
                    continue;
                }
                //将加载的依赖包存入字典
                abDic.TryAdd(dependencyStr, dependencyAB);
            }
        }

        //加载目标AB包
        //如果目标AB包没有加载过 则加载目标AB包
        if (!abDic.TryGetValue(targetABName, out var targetAB))
        {
            targetAB = AssetBundle.LoadFromFile(Path.Combine(ABPath, targetABName));

            //如果加载失败 则输出错误信息
            if (targetAB == null)
            {
                Debug.LogError($"目标AB包加载失败！路径：{Path.Combine(ABPath, targetABName)}");
                return null;
            }

            //将加载的目标AB包存入字典
            abDic.TryAdd(targetABName, targetAB);
        }

        //返回加载的目标AB包
        return targetAB;
    }

    /// <summary>
    /// 生成资源Key
    /// </summary>
    /// <param name="abName">AB包</param>
    /// <param name="resName">资源包</param>
    /// <param name="suffix">后缀</param>
    /// <returns>资源Key</returns>
    private string GenerateObjKey(string abName, string resName, string suffix = "")
    {
        return $"{abName}_{resName}_{suffix}_";
    }

    /// <summary>
    /// 加载资源的接口
    /// </summary>
    /// <param name="objKey">目标资源Key</param>
    /// <param name="targetABName">目标AB包</param>
    /// <param name="targetResName">目标资源名称</param>
    /// <param name="callBack">回调函数</param>
    /// <returns>目标资源</returns>
    private UnityEngine.Object LoadResInternal(string objKey, string targetABName, string targetResName,Func<AssetBundle, UnityEngine.Object> callBack)
    {
        //如果资源容器中没有该资源
        if (!objDic.TryGetValue(objKey, out UnityEngine.Object obj) || obj == null)
        {
            //则加载目标AB包
            AssetBundle ab = LoadTargetAB(targetABName);
            //如果加载失败 则返回null
            if (ab == null)
            {
                Debug.LogError($"目标AB包加载失败：{targetABName}");
                return null;
            }
            //加载目标资源
            obj = callBack(ab);
            //如果加载失败 则输出错误信息
            if (obj == null)
            {
                Debug.LogError($"资源加载失败：AB包：{targetABName}，资源名：{targetResName}");
                return null;
            }
            else
            {
                //将加载的资源存入字典
                objDic[objKey] = obj;
            }
        }

        return obj;
    }

    /// <summary>
    /// 同步加载资源（以名称的形式加载资源）
    /// </summary>
    /// <param name="targetABName">目标包</param>
    /// <param name="targetResName">目标资源</param>
    /// <returns>资源</returns>
    public UnityEngine.Object LoadABRes(string targetABName, string targetResName)
    {
        // 资源容器中存储的资源名称 
        string objKey = GenerateObjKey(targetABName, targetResName, "Name");
        // 获取资源
        return LoadResInternal(objKey, targetABName, targetResName, (ab) => ab.LoadAsset(targetResName));
    }

    /// <summary>
    /// 同步加载资源（以Type的形式加载资源）
    /// </summary>
    /// <param name="targetABName">目标AB包名称</param>
    /// <param name="targetResName">目标资源名称</param>
    /// <param name="type">目标资源类型</param>
    /// <returns>资源</returns>
    public UnityEngine.Object LoadABRes(string targetABName, string targetResName, System.Type type)
    {
        // 资源容器中存储的资源名称 
        string objKey = GenerateObjKey(targetABName, targetResName, "Type");
        // 获取资源
        return LoadResInternal(objKey, targetABName, targetResName, (ab) => ab.LoadAsset(targetResName, type));
    }

    /// <summary>
    /// 同步加载资源（以泛型加载资源）
    /// </summary>
    /// <typeparam name="T">目标资源的类型（泛型）</typeparam>
    /// <param name="targetABName">目标AB包名称</param>
    /// <param name="targetResName">目标资源名称</param>
    /// <returns>资源</returns>
    public T LoadABRes<T>(string targetABName, string targetResName) where T : UnityEngine.Object
    {
        // 资源容器中存储的资源名称 
        string objKey = GenerateObjKey(targetABName, targetResName, "Generics");
        // 获取资源
        return LoadResInternal(objKey, targetABName, targetResName, (ab) => ab.LoadAsset<T>(targetResName)) as T;
    }
    #endregion

    #region 异步加载AB包

    /// <summary>
    /// 用于将带回调的协程包装成 Task
    /// </summary>
    private Task<T> WrapCoroutine<T>(Func<UnityAction<T>, string, IEnumerator> coroutineStarter, string path = null)
    {
        var tcs = new TaskCompletionSource<T>();
        StartCoroutine(coroutineStarter((result) => tcs.SetResult(result), path));
        return tcs.Task;
    }

    /// <summary>
    /// 加载主包以及主包配置文件
    /// </summary>
    private IEnumerator LoadMainABAsyncCor(UnityAction<UnityEngine.Object> tcsCallBack = null, string path = null)
    {
        //加载主包
        if (abMain == null)
        {
            //尝试加载AB包
            AssetBundleCreateRequest abcr = AssetBundle.LoadFromFileAsync(path);
            //等待AssetBundleCreateRequest完成
            yield return abcr;
            //如果加载失败 则输出错误信息
            if (abcr.assetBundle == null)
            {
                Debug.LogError($"AssetBundleCreateRequest加载失败！");
                yield break;
            }
            
            //获取加载的主包
            abMain = abcr.assetBundle;
        }

        //加载主包的依赖配置文件
        if (abMainManifest == null)
        {
            //尝试加载主包的AssetBundleManifest
            AssetBundleRequest abr = abMain.LoadAssetAsync<AssetBundleManifest>("AssetBundleManifest");
            yield return abr;
            if (abr.asset == null)
            {
                Debug.LogError("AssetBundleRequest加载失败！"); 
                yield break;
            }

            abMainManifest = abr.asset as AssetBundleManifest;
        }

        //如果主包和主包配置文件都加载成功 则执行回调函数
        tcsCallBack?.Invoke(abMain);
    }

    /// <summary>
    /// 异步加载目标AB包的协程
    /// </summary>
    /// <param name="callBack">回调函数</param>
    /// <param name="targetABName">目标AB包</param>
    /// <returns></returns>
    private IEnumerator LoadFromFileAsyncCor(UnityAction<UnityEngine.AssetBundle> callBack = null, string targetABName = null)
    {
        //加载目标AB包
        AssetBundleCreateRequest abcr = AssetBundle.LoadFromFileAsync(targetABName);
        //等待AssetBundle加载完成
        yield return abcr;
        //如果加载失败 则输出错误信息
        if (abcr.assetBundle == null)
        {
            Debug.LogError($"目标AssetBundleCreateRequest加载失败!");
            yield break;
        }

        //获取加载的AssetBundle
        AssetBundle ab = abcr.assetBundle;

        //如果有回调函数 则执行回调
        callBack?.Invoke(ab);
    }

    /// <summary>
    /// 异步加载目标AB包
    /// </summary>
    /// <param name="targetABName">目标AB包名称</param>
    private async Task<AssetBundle> LoadTargetABAsync(string targetABName)
    {
        await WrapCoroutine<UnityEngine.Object>(LoadMainABAsyncCor, Path.Combine(ABPath, ABMainName));

        //获取所有依赖的名称
        string[] dependenciesStrs = abMainManifest.GetAllDependencies(targetABName);
        //加载所有依赖
        foreach (string dependencyStr in dependenciesStrs)
        {
            if (!abDic.ContainsKey(dependencyStr))
            {
                //尝试加载依赖包
                var denpendencyAB = await WrapCoroutine<AssetBundle>(LoadFromFileAsyncCor, Path.Combine(ABPath, dependencyStr));
                //如果加载失败 则输出错误信息
                if (denpendencyAB == null)
                {
                    Debug.LogError($"依赖包加载失败！路径：{Path.Combine(ABPath, dependencyStr)}");
                    continue;
                }
                //将加载的依赖包存入字典
                abDic.TryAdd(dependencyStr, denpendencyAB);
            }
        }
        //加载目标AB包
        if (!abDic.TryGetValue(targetABName, out AssetBundle ab) || ab == null)
        {   
            string path = Path.Combine(ABPath, targetABName);
            ab = await WrapCoroutine<AssetBundle>(LoadFromFileAsyncCor, path);
            //如果加载失败 则输出错误信息
            if (ab == null)
            {
                Debug.LogError($"目标AB包加载失败！路径：{path}");
                return null;
            }
            //将加载的目标AB包存入字典
            abDic.TryAdd(targetABName, ab);
        }

        //返回加载的目标AB包
        return ab;
    }

    /// <summary>
    /// 用于封装异步加载资源的通用方法
    /// </summary>
    /// <typeparam name="T">泛型</typeparam>
    /// <param name="targetABName">目标AB包名称</param>
    /// <param name="targetResName">目标资源名称</param>
    /// <param name="objKey">资源存储的Key</param>
    /// <param name="callBack">回调函数</param>
    /// <param name="loadRequestGetter">加载资源的委托</param>
    /// <returns>对应类型的资源</returns>
    private async Task<T> AwaitAssetBundleRequest<T>(string targetABName, string targetResName, string objKey, UnityAction<UnityEngine.Object> callBack, Func<AssetBundle, string, AssetBundleRequest> loadRequestGetter) where T : UnityEngine.Object
    {
        //如果资源容器中已经有该资源 则执行回调并返回资源
        if (objDic.TryGetValue(objKey, out UnityEngine.Object obj))
        {
            callBack?.Invoke(obj);
            return obj as T;
        }

        //异步加载目标AB包
        AssetBundle ab = await LoadTargetABAsync(targetABName);
        //判断callBack2是否为null
        if (loadRequestGetter == null)
        {
            Debug.LogError("加载函数 callBack2 为空！");
            return null;
        }
        AssetBundleRequest abr = loadRequestGetter.Invoke(ab, targetResName);
        //等待AssetBundleRequest完成
        while (!abr.isDone)
        {           
            await Task.Yield();
        }

        //判断加载的资源是否为null
        if (abr.asset == null)
        {
            Debug.LogError("AssetBundleRequest加载失败！（资源为空）");
            return null;
        }

        obj = abr.asset;

        //如果有回调函数 则执行回调wo1
        callBack?.Invoke(obj);

        //将加载的资源存入字典
        objDic[objKey] = obj;

        return abr.asset as T;
    }

    /// <summary>
    /// 异步加载资源（以名称的形式加载资源）
    /// </summary>
    /// <param name="targetABName">目标AB包</param>
    /// <param name="targetResName">目标资源</param>
    /// <returns>对应类型的资源</returns>
    public async Task<UnityEngine.Object> LoadABResAsync(string targetABName, string targetResName, UnityAction<UnityEngine.Object> callBack)
    {
        //生成资源Key
        string objKey = GenerateObjKey(targetABName, targetResName, "Name");

        UnityEngine.Object obj = await AwaitAssetBundleRequest<UnityEngine.Object>(targetABName, targetResName, objKey, callBack, (ab, targetResName) => ab.LoadAssetAsync(targetResName));

        //返回资源
        return obj;
    }

    /// <summary>
    /// 异步加载资源（以Type的形式加载资源）
    /// </summary>
    /// <param name="targetABName">目标AB包</param>
    /// <param name="targetResName">目标资源</param>
    /// <param name="type">目标资源类型</param>
    /// <returns>对应类型的资源</returns>
    public async Task<UnityEngine.Object> LoadABResAsync(string targetABName, string targetResName, System.Type type, UnityAction<UnityEngine.Object> callBack)
    {
        //生成资源Key
        string objKey = GenerateObjKey(targetABName, targetResName, "Type");

        UnityEngine.Object obj = await AwaitAssetBundleRequest<UnityEngine.Object>(targetABName, targetResName, objKey, callBack, (ab, targetResName) => ab.LoadAssetAsync(targetResName, type));

        //返回资源
        return obj;
    }

    /// <summary>
    /// 异步加载资源（以泛型加载资源）
    /// </summary>
    /// <typeparam name="T">泛型</typeparam>
    /// <param name="targetABName">目标AB包</param>
    /// <param name="targetResName">目标资源</param>
    /// <param name="t">目标类型</param>
    /// <returns>对应类型的资源</returns>
    public async Task<T> LoadABResAsync<T>(string targetABName, string targetResName, UnityAction<UnityEngine.Object> callBack) where T : UnityEngine.Object
    {
        //生成资源Key
        string objKey = GenerateObjKey(targetABName, targetResName, "Generics");

        T obj = await AwaitAssetBundleRequest<T>(targetABName, targetResName, objKey, callBack, (ab, targetResName) => ab.LoadAssetAsync<T>(targetResName));

        //返回资源
        return obj;
    }
    #endregion

    #region 同步卸载AB包
    /// <summary>
    /// 单个包卸载
    /// </summary>
    /// <param name="targetABName">目标包名称</param>
    public void UnLoadAB(string targetABName)
    {
        if(abDic.ContainsKey(targetABName))
        {
            abDic[targetABName].Unload(false);
            abDic.Remove(targetABName);
        }
    }

    /// <summary>
    /// 所有包卸载
    /// </summary>
    public void UnLoadAllAB()
    {
        //卸载所有AB包 
        foreach (var ab in abDic)
        {
            ab.Value.Unload(false);
        }
        abDic.Clear();

        // 卸载主包和主包配置文件
        if (abMain != null)
        {
            abMain.Unload(false);
            abMain = null;
        }
        if (abMainManifest != null)
        {
            abMainManifest = null;
        }
    }
    #endregion

    #region 异步卸载AB包

    /// <summary>
    /// 异步卸载AB包的协程
    /// </summary>
    /// <param name="targetABName">目标AB包名称</param>
    /// <param name="callBack">回调函数</param>
    /// <returns>协程</returns>
    private IEnumerator UnLoadABAsyncCor(string targetABName, UnityAction<bool> callBack)
    {
        //先在字典中查找目标AB包
        if (abDic.ContainsKey(targetABName))
        {
            //等待一帧以确保资源加载完成
            yield return null;
            //卸载目标AB包
            abDic[targetABName].Unload(false);
            //从字典中移除目标AB包
            bool isSuccess = abDic.Remove(targetABName);
            //执行回调函数
            callBack?.Invoke(isSuccess);
        }
    }

    /// <summary>
    /// 异步卸载单个AB包
    /// </summary>
    /// <param name="targetABName">目标包名称</param>
    /// <param name="callBack">回调函数</param>
    /// <returns>是否成功卸载AB包</returns>
    public Task<bool> UnLoadABAsync(string targetABName, UnityAction callBack = null)
    {
        //使用TaskCompletionSource来封装异步操作
        var tcs = new TaskCompletionSource<bool>();
        //开始协程卸载AB包
        StartCoroutine(UnLoadABAsyncCor(targetABName, (b) => { tcs.SetResult(b); }));
        //执行回调函数
        callBack?.Invoke();
        //返回一个Task对象来判断当前协程是否完成
        return tcs.Task;
    }

    /// <summary>
    /// 清空所有AB包的协程
    /// </summary>
    /// <param name="callBack">回调函数</param>
    /// <returns>协程</returns>
    private IEnumerator UnLoadAllABAsyncCor(UnityAction<bool> callBack)
    {
        //卸载内存中所有的AB包
        AssetBundle.UnloadAllAssetBundles(false);
        //清空字典
        abDic.Clear();
        //执行回调函数
        callBack?.Invoke(true);
        //等待一帧以确保资源加载完成
        yield return null;
    }

    /// <summary>
    /// 删除所有AB包的异步方法
    /// </summary>
    /// <param name="callBack">回调函数</param>
    /// <returns>是否成功卸载资源</returns>
    public Task<bool> UnLoadAllABAsync(UnityAction callBack = null)
    {
        //使用TaskCompletionSource来封装异步操作
        var tcs = new TaskCompletionSource<bool>();
        //开始协程卸载所有AB包
        StartCoroutine(UnLoadAllABAsyncCor( (b) => { tcs.SetResult(b); }));
        //执行回调函数
        callBack?.Invoke();
        return tcs.Task;
    }
    #endregion

    #region objDic相关操作
    /// <summary>
    /// 清除已经被销毁的资源的引用
    /// </summary>
    public void ClearUnusedObjects()
    {
        List<string> toRemove = new List<string>();
        foreach (var kvp in objDic)
        {
            if (kvp.Value == null) toRemove.Add(kvp.Key);
        }

        foreach (var key in toRemove)
        {
            objDic.Remove(key);
        }
    }

    /// <summary>
    /// 清空objDic里的缓存资源
    /// </summary>
    public void ClearObjDic()
    {
        if (objDic.Count > 0)
        {
            objDic.Clear();
        }
    }
    #endregion
}