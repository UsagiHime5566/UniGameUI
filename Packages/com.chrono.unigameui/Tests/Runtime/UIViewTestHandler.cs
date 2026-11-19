using System;
using UnityEngine;

namespace UniGameUI.Tests
{
    public sealed class UIViewTestHandler : UIViewBehaviour
    {
        internal static Action<UIViewTestHandler, string, object> Observed;
        [SerializeField] internal string label;

        protected override void OnShowing(object data) => Observed?.Invoke(this, "Showing", data);
        protected override void OnShown() => Observed?.Invoke(this, "Shown", null);
        protected override void OnHiding() => Observed?.Invoke(this, "Hiding", null);
        protected override void OnHidden() => Observed?.Invoke(this, "Hidden", null);
        protected override void OnAction(string action) => Observed?.Invoke(this, "Action", action);

        public void ReportFromInspector(string phase) => Observed?.Invoke(this, phase, null);

        public void ThrowFromInspector(string message)
        {
            throw new FormatException(message);
        }
    }
}
