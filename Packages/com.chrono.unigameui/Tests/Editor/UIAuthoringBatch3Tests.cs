using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using Object = UnityEngine.Object;

namespace UniGameUI.Editor.Tests
{
    public sealed partial class UIAuthoringTests
    {
        [Test]
        public void NewWorkspaceUsesTheInputSystemModuleAndValidationRejectsOtherInputSetups()
        {
            var scene = NewScene("Batch3 Input Setup");
            var root = UIAuthoring.CreateWorkspace(scene);
            var eventSystem = scene.GetRootGameObjects()
                .Select(gameObject => gameObject.GetComponentInChildren<EventSystem>(true)).Single(found => found);
            Assert.That(eventSystem.GetComponent<BaseInputModule>(), Is.TypeOf<InputSystemUIInputModule>());
            Assert.That(InputErrors(root), Is.Empty);

            Object.DestroyImmediate(eventSystem.GetComponent<InputSystemUIInputModule>());
            eventSystem.gameObject.AddComponent<StandaloneInputModule>();
            Assert.That(InputErrors(root).Single(), Does.Contain(eventSystem.name).And.Contain("InputSystemUIInputModule"));

            Object.DestroyImmediate(eventSystem.GetComponent<StandaloneInputModule>());
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            Assert.That(InputErrors(root), Is.Empty);

            var extra = new GameObject("Second EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            try
            {
                Assert.That(InputErrors(root).Single(), Does.Contain("剛好一個").And.Contain("2 個"));
            }
            finally
            {
                Object.DestroyImmediate(extra);
            }

            eventSystem.gameObject.SetActive(false);
            Assert.That(InputErrors(root).Single(), Does.Contain("剛好一個").And.Contain("0 個"));
            eventSystem.gameObject.SetActive(true);

            UIValidation.ActiveInputHandlingOverride = 0;
            try
            {
                Assert.That(InputErrors(root).Single(), Does.Contain("Active Input Handling"));
            }
            finally
            {
                UIValidation.ActiveInputHandlingOverride = null;
            }
            foreach (var supported in new[] { 1, 2 })
            {
                UIValidation.ActiveInputHandlingOverride = supported;
                try
                {
                    Assert.That(InputErrors(root), Is.Empty);
                }
                finally
                {
                    UIValidation.ActiveInputHandlingOverride = null;
                }
            }
        }

        private static IEnumerable<string> InputErrors(UIRoot root)
        {
            return FunctionalErrors(root).Select(issue => issue.Message)
                .Where(message => message.Contains("EventSystem") || message.Contains("Active Input Handling"));
        }
    }
}
