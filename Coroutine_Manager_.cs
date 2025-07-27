using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Э��ö������
/// </summary>
public enum CoroutineType
{
    // ����Э������
    Type1,
    Type2,
    // ���Ը�����Ҫ��Ӹ�������
}

/// <summary>
/// Э�̹�����
/// </summary>
public class Coroutine_Manager_ : Singleton_Mono_<Coroutine_Manager_>
{
    /// <summary>
    /// Э�̵ķ�װ����
    /// </summary>
    private class CoroutineWrapper
    {
        public Coroutine Coroutine;// Э�̾��
        public string CoroutineName; // Э������
        public bool IsRunning;// �Ƿ��������еı�־
        public CoroutineType CoroutineType; // Э������
        public string CoroutineID; // ����C#���ɵ�Ψһ��ʶ����ΪЭ��ID

        public CoroutineWrapper()
        {
            IsRunning = false; // Ĭ��Э�̲�������״̬
            CoroutineName = string.Empty; // Ĭ��Э������Ϊ��
            CoroutineID = Guid.NewGuid().ToString(); // ����Ψһ��ʶ��
        }
    }

    private Dictionary<CoroutineType, HashSet<CoroutineWrapper>> coroutineMap = new(); // Э�̻����ֵ�
    private readonly object Locked_ = new object();//Ψһ������
    /// <summary>
    /// ����һ��Э�̣������������ָ���ı�ǩ��
    /// </summary>
    /// <param name="routine">Э�̾��</param>
    /// <param name="cType">Э�̱�ǩ</param>
    /// <param name="callBack">����Э����Ϻ�Ļص�����</param>
    /// <param name="invokeCallbackOnSuccessOnly">�Ƿ���Ҫ��Э�����гɹ���ŵ��ûص�����</param>
    /// <returns>Э�̾��</returns>
    public Coroutine StartManagedCoroutine(Func<IEnumerator> routineFactory, CoroutineType cType, UnityAction callBack = null, bool invokeCallbackOnSuccessOnly = true)
    {
        // ��Э��Ϊ�գ���ֱ���˳�������null
        if (routineFactory == null)
        {
            Debug.LogError("Э�̺�������Ϊ��");
            return null;
        }

        var routine = routineFactory();//�����µ�ʵ��

        //����һ�����ڷ�װЭ�̵Ķ���
        var wrapper = new CoroutineWrapper
        {
            IsRunning = true,// ����Э���������еı�־
            CoroutineType = cType,// ����Э������
            CoroutineName = routine.GetType().Name // ����Э�̵�����
        };

        //ʹ���߳�ͬ���İ�ȫ����
        lock (Locked_)
        {
            //����װ������ӵ��ֵ���
            if (!coroutineMap.TryGetValue(cType, out var set))
            {
                set = new();
                coroutineMap[cType] = set;
            }
            set.Add(wrapper);
        }

        // ����Э�̣�����Э�̾�����ظ���װЭ�̶���
        var cor = StartCoroutine(WrapCoroutine(routine, cType, wrapper, invokeCallbackOnSuccessOnly, callBack));
        wrapper.Coroutine = cor;

        //����Э�̾��
        return cor;
    }

    /// <summary>
    /// ��װЭ�������߼��ĵ�����
    /// </summary>
    /// <param name="routine">��Ӧ��Э�̾��</param>
    /// <param name="cType">Э������</param>
    /// <param name="wrapperCoroutine">��װ</param>
    /// <param name="invokeCallbackOnSuccessOnly">�Ƿ���Ҫ��Э�����гɹ�ʱ��ִ�лص�����</param>
    /// <param name="callBack">Э��������Ϻ�Ļص�����</param>
    /// <returns>Э�̵�����</returns>
    private IEnumerator WrapCoroutine(IEnumerator routine, CoroutineType cType, CoroutineWrapper wrapperCoroutine, bool invokeCallbackOnSuccessOnly, UnityAction callBack = null)
    {
        //�ȴ�һ֡��ȷ��Wrapper.Coroutine����ȷ��ʼ��
        yield return null;

        //��ʾЭ�̵Ļص������Ƿ��ܱ�����ִ�еı�־
        bool errorOccurred = true;

        try
        {
            //�ȴ�Э���������
            yield return routine;
            //Э����������
            errorOccurred = false;
        }
        finally
        {
            //Э��������ɺ󣬽�Э�̱��Ϊ�������е���
            wrapperCoroutine.IsRunning = false;

            //����߳�ͬ��
            lock (Locked_)
            {
                //����ñ�ǩ�µ�Э�̼���Ϊ�գ�����ֵ����Ƴ��ñ�ǩ
                if (coroutineMap.TryGetValue(cType, out var set))
                {
                    //�Ӽ������Ƴ��÷�װ����
                    if (set.Remove(wrapperCoroutine) && set.Count == 0)
                    {
                        coroutineMap.Remove(cType);
                    }
                }
                //���Э�����й����з����˴���
                if (errorOccurred)
                {
                    Debug.Log($"Э��ʱ���ִ���");

                    // �������Ҫ��Э�̳ɹ���ŵ��ûص�����������ûص�����
                    if (!invokeCallbackOnSuccessOnly)
                        callBack?.Invoke();
                }
                //Э��ִ�гɹ�ʱ���ûص�����
                else
                {
                    callBack.Invoke();
                }
            }
        }
    }

    /// <summary>
    /// ֹͣ���Ƴ��ض���Э�̷�װ����
    /// </summary>
    /// <param name="wrapper">Э�̷�װ����</param>
    private void StopSpecificWrapper(CoroutineWrapper wrapper)
    {
        // ���wrapperΪnull,��ֱ�ӷ���
        if (wrapper == null) return;

        // ���Э�̾��
        if (wrapper.Coroutine != null)
        {
            StopCoroutine(wrapper.Coroutine); // ֹͣЭ��
            wrapper.Coroutine = null; // ���Э�̾��
            wrapper.IsRunning = false; // ���Ϊ��������״̬
        }
    }

    /// <summary>
    /// ��ָ����ǩ�����Ƴ�һ��Э�̷�װ����
    /// </summary>
    /// <param name="cType">Э�̵�����</param>
    /// <param name="set">Э�̷�װ������</param>
    /// <param name="wrapper">Э�̷�װ����</param>
    private void RemoveSpecificWrapper(CoroutineType cType, HashSet<CoroutineWrapper> set, CoroutineWrapper wrapper)
    {
        // ���wrapperΪnull,��ֱ�ӷ���
        if (wrapper == null || set == null) return;

        // �Ӽ������Ƴ��÷�װ����
        if (set.Remove(wrapper) && set.Count == 0)
            coroutineMap.Remove(cType);

        // ���������Ϣ
        Debug.Log($"Э��ID: {wrapper.CoroutineID}��Э������: {wrapper.CoroutineName}���ѱ�������ֹͣ���Ƴ�");
    }

    /// <summary>
    /// ֹͣ���Ƴ�ĳ���ض���ǩ�µ�ָ��Э��
    /// </summary>
    /// <param name="cType">Э������</param>
    /// <param name="coroutine">Э�̾��</param>
    public void StopSpecificCoroutine(CoroutineType cType, Coroutine coroutine)
    {
        // ���Э�̾��Ϊ�գ���ֱ�ӷ���
        lock (Locked_)
        {
            // ������coroutineMap�л�ȡָ����ǩ��Э�̼���
            if (!coroutineMap.TryGetValue(cType, out var set)) return;

            // �̱߳�����ȫ
            CoroutineWrapper targetWrapper = null;
            
            // ʹ��LINQ����ָ��Э�̾���ķ�װ����
            targetWrapper = set.FirstOrDefault(w => w.Coroutine == coroutine);
            
            // ����ֹͣ���Ƴ��ض���Э�̷�װ����
            StopSpecificWrapper(targetWrapper);

            // �����Ƴ��÷�װ����
            RemoveSpecificWrapper(cType, set, targetWrapper);
        }
    }

    /// <summary>
    /// ָֹͣ����ǩ�µ�����Э��
    /// </summary>
    /// <param name="cType">Э������</param>
    public void StopAllCoroutineByType(CoroutineType cType)
    {
        //ʹ���߳�ͬ���İ�ȫ����
        lock (Locked_)
        {
            //������coroutineMap�л�ȡָ����ǩ��Э�̼���
            if (!coroutineMap.TryGetValue(cType, out var set))
            {
                Debug.Log($"û���ҵ�ָ����ǩ�µ�Э��: {cType}");
                return;
            }

            //����������Ӧ��Э�̼���
            foreach (var targetWrapper in set.ToList())
            {
                // ����ֹͣ���Ƴ��ض���Э�̷�װ����
                StopSpecificWrapper(targetWrapper);

                // �����Ƴ��÷�װ����
                RemoveSpecificWrapper(cType, set, targetWrapper);
            }
        }
    }

    /// <summary>
    /// ֹͣ�����������е�Э��
    /// </summary>
    public void StopAllManagedCoroutines()
    {
        // ����ֵ�Ϊ�գ���ֱ�ӷ���
        if (coroutineMap.Count == 0)
            return;

        //ʹ���߳�ͬ���İ�ȫ����
        lock (Locked_)
        {
            //�����ֵ��е����б�ǩ
            foreach (var cType in coroutineMap.Keys.ToList())
            {
                StopAllCoroutineByType(cType);
            }

            Debug.Log("ֹͣ�����������е�Э��");
        }
    }

    /// <summary>
    /// ���ָ����ǩ���Ƿ����κ�Э����������
    /// </summary>
    /// <param name="cType">Э������</param>
    /// <returns>�Ƿ����������е�Э��</returns>
    public bool IsAnyCoroutineRunning(CoroutineType cType)
    {
        //����ֵ���û�иñ�ǩ����ֱ�ӷ���false 
        if (!coroutineMap.TryGetValue(cType, out var wrapperSet))
            return false;

        //ʹ�ø������б���,��HashSet��ֻ��������ʽ�ǰ�ȫ�ģ�
        return wrapperSet.Any(wrapper => wrapper.IsRunning);

    }

    /// <summary>
    /// ͳ��ָ��Э���������������е�Э������
    /// </summary>
    /// <param name="cType">Э������</param>
    /// <returns>ָ���������������е�Э������</returns>
    public int GetRunningCoroutineCount(CoroutineType cType)
    {
        //�ж��ֵ����Ƿ���ڸñ�ǩ��Э�̼���
        if (!coroutineMap.TryGetValue(cType, out var wrapperSet))
        {
            Debug.Log($"û���ҵ�ָ����ǩ�µ�Э��: {cType}");
            return 0;
        }

        //ɸѡ���������е�Э�̣���������������LINQд����
        return wrapperSet.Count(wrapper => wrapper.IsRunning);
    }

    /// <summary>
    /// ��ȡ��Э�̹��������������Э�̵�����
    /// </summary>
    /// <returns></returns>
    public int GetTotalRunningCoroutineCount()
    {
        //ʹ��LINQͳ�����б�ǩ���������е�Э������
        return coroutineMap.Values.Sum(set => set.Count(wrapper => wrapper.IsRunning));
    }

    /// <summary>
    /// ��ӡ�����Э����Ϣ
    /// </summary>
    /// <param name="cType">Э������</param> 
    /// <param name="set">Э�̷�װ����ļ���</param>
    /// <param name="filterRunningOnly">�Ƿ���Ҫɸѡ���������е�Э��</param>
    private void OutputCoroutineDetails(CoroutineType cType, HashSet<CoroutineWrapper> set, bool filterRunningOnly = false)
    {
        Debug.Log($"Э������: {cType}, Э������: {set.Count}");

        //����ÿ����ǩ�µ�Э�̷�װ����
        foreach (var wrapperCoroutine in set)
        {
            //�����Ƿ���Ҫɸѡ���������е�Э�̽������
            if (filterRunningOnly)
            {
                if (wrapperCoroutine.IsRunning)
                {
                    Debug.Log($"Э��ID��{wrapperCoroutine.CoroutineID}��Э������: {wrapperCoroutine.CoroutineName}, ��������: {wrapperCoroutine.IsRunning}");
                }
            }
            else
            {
                Debug.Log($"Э��ID�� {wrapperCoroutine.CoroutineID} ��Э������: : {wrapperCoroutine.CoroutineName}, ��������: {wrapperCoroutine.IsRunning}");
            }
        }
    }

    /// <summary>
    /// ���ݶ�Ӧ��Э������������ڱ������Э����Ϣ
    /// </summary>
    /// <param name="cType">Э������</param>
    /// <param name="filterRunningOnly">�Ƿ���Ҫɸѡ���������е�Э��</param>
    public void LogCoroutineInfoByType(CoroutineType cType, bool filterRunningOnly = false)
    {
        //ʹ���߳�ͬ���İ�ȫ����
        lock (Locked_)
        {   
            coroutineMap.TryGetValue(cType, out var set);

            //���û���ҵ���Ӧ��Э�̼��ϣ��������ʾ��Ϣ
            if (set == null)
            {
                Debug.Log($"û���ҵ�ָ����ǩ�µ�Э��: {cType}");
                return;
            }

            //��������Э����Ϣ
            OutputCoroutineDetails(cType, set, filterRunningOnly);
        }
    }

    /// <summary>
    /// ����������ڱ������Э����Ϣ
    /// <paramref name="filterRunningOnly">�Ƿ���Ҫɸѡ���������е�</param>
    /// </summary>
    public void LogAllCoroutineInfos(bool filterRunningOnly = false)
    {
        // ����ֵ�Ϊ�գ���ֱ�ӷ���
        if (coroutineMap.Count == 0)
        {
            Debug.Log("û���������е�Э��");
            return;
        }

        Debug.Log("�������е�Э���б�:");

        //ʹ���߳�ͬ���İ�ȫ����
        lock (Locked_)
        {   
            foreach (var kvp in coroutineMap)
            {
                //��������Э����Ϣ
                OutputCoroutineDetails(kvp.Key, kvp.Value, filterRunningOnly);
            }
        }
    }
}