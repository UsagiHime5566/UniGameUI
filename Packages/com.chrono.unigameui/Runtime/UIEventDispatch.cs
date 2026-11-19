using System;
using System.Collections;
using System.Reflection;
using System.Runtime.ExceptionServices;
using UnityEngine.Events;

namespace UniGameUI
{
    internal static class UIEventDispatch
    {
        // UnityEvent.Invoke stops at the first exception. Use Unity's prepared calls to
        // preserve Inspector and runtime bindings, including their order and arguments.
        private static readonly MethodInfo PrepareInvoke = typeof(UnityEventBase)
            .GetMethod("PrepareInvoke", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(typeof(UnityEventBase).FullName, "PrepareInvoke");

        private static readonly MethodInfo InvokeListener = PrepareInvoke.ReturnType.GetGenericArguments()[0]
            .GetMethod("Invoke", new[] { typeof(object[]) });

        internal static bool Invoke(UnityEventBase signal, Action<Exception> report, params object[] arguments)
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
