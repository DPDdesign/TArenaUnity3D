using System;
using System.Reflection;
using UnityEngine;

public enum RpcTarget
{
    All,
    Others
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class PunRPCAttribute : Attribute
{
}

public class LocalNetworkBehaviour : MonoBehaviour
{
    private LocalRpcView localPhotonView;

    public LocalRpcView photonView
    {
        get
        {
            if (localPhotonView == null)
            {
                localPhotonView = new LocalRpcView(this);
            }

            return localPhotonView;
        }
    }
}

public sealed class LocalRpcView
{
    private readonly MonoBehaviour owner;

    public LocalRpcView(MonoBehaviour owner)
    {
        this.owner = owner;
    }

    public void RPC(string methodName, RpcTarget target, object[] parameters)
    {
        if (target == RpcTarget.Others)
        {
            return;
        }

        InvokeLocal(methodName, parameters ?? new object[0]);
    }

    public void RPC(string methodName, RpcTarget target)
    {
        RPC(methodName, target, new object[0]);
    }

    private void InvokeLocal(string methodName, object[] parameters)
    {
        MethodInfo method = FindMethod(methodName, parameters.Length);
        if (method == null)
        {
            Debug.LogWarning("Local RPC target not found: " + owner.GetType().Name + "." + methodName);
            return;
        }

        method.Invoke(owner, parameters);
    }

    private MethodInfo FindMethod(string methodName, int parameterCount)
    {
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        foreach (MethodInfo method in owner.GetType().GetMethods(flags))
        {
            if (method.Name == methodName && method.GetParameters().Length == parameterCount)
            {
                return method;
            }
        }

        return null;
    }
}

public static class LocalGameSession
{
    public static bool IsMasterClient
    {
        get { return true; }
    }

    public static int PlayerCount
    {
        get { return 1; }
    }

    public static bool IsConnected
    {
        get { return true; }
    }

    public static bool ShouldRunNetworkGameplay
    {
        get { return false; }
    }

    public static void ForceLocalMode()
    {
        PlayerPrefs.SetInt("Multi", 0);
    }
}
