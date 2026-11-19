using System;
using System.Collections;
using System.Reflection;
using System.Runtime.ExceptionServices;
using UnityEngine;
using UnityEngine.Events;

namespace UniGameUI
{
    internal static class UIEventDispatch
    {
        // UnityEvent.Invoke stops at the first exception. Use Unity's prepared calls to
        // preserve Inspector and runtime bindings, including their order and arguments.
        private static readonly MethodInfo PrepareInvoke = typeof(UnityEventBase)
            .GetMethod("PrepareInvoke", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, Type.EmptyTypes, null);

        private static readonly MethodInfo InvokeListener = FindListenerInvoke();
        private static bool warnedFallback;

        private static MethodInfo FindListenerInvoke()
        {
            var list = PrepareInvoke?.ReturnType;
            if (list == null || !typeof(IList).IsAssignableFrom(list) || !list.IsGenericType)
                return null;

            var listener = list.GetGenericArguments()[0];
            return listener.GetMethod("Invoke", new[] { typeof(object[]) });
        }

        internal static bool Invoke(UnityEvent signal, Action<Exception> report)
        {
            return InvokeListener == null
                ? InvokeFallback(signal.Invoke, report)
                : InvokeIsolated(signal, report, Array.Empty<object>());
        }

        internal static bool Invoke<TFirst, TSecond>(
            UnityEvent<TFirst, TSecond> signal, Action<Exception> report, TFirst first, TSecond second)
        {
            return InvokeListener == null
                ? InvokeFallback(() => signal.Invoke(first, second), report)
                : InvokeIsolated(signal, report, new object[] { first, second });
        }

        internal static bool InvokeFallback(Action invoke, Action<Exception> report)
        {
            if (!warnedFallback)
            {
                warnedFallback = true;
                Debug.LogWarning("UniGameUI: 找不到逐一派送 UnityEvent 所需的方法，改用 UnityEvent.Invoke()。" +
                    "同一個 UnityEvent 會在第一個監聽丟出例外時停止。");
            }

            try
            {
                invoke();
                return true;
            }
            catch (Exception exception) when (report != null)
            {
                report(exception);
                return false;
            }
        }

        private static bool InvokeIsolated(UnityEventBase signal, Action<Exception> report, object[] arguments)
        {
            var calls = (IList)PrepareInvoke.Invoke(signal, null);
            if (calls.Count == 0)
                return true;

            var parameters = new object[] { arguments };
            var succeeded = true;
            for (var index = 0; index < calls.Count; index++)
            {
                try
                {
                    InvokeListener.Invoke(calls[index], parameters);
                }
                catch (TargetInvocationException exception) when (exception.InnerException != null)
                {
                    if (report == null)
                        ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                    report(exception.InnerException);
                    succeeded = false;
                }
            }
            return succeeded;
        }

        internal static void Invoke<T>(T signal, Action<T> invoke, Action<Exception> report) where T : Delegate
        {
            if (signal == null)
                return;
            foreach (T listener in signal.GetInvocationList())
            {
                try
                {
                    invoke(listener);
                }
                catch (Exception exception)
                {
                    report(exception);
                }
            }
        }
    }
}
