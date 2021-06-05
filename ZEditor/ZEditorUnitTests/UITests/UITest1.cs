using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;
using ZEditor.ZComponents.UI;
using ZEditor.ZControl;
using ZEditor.ZManage;

namespace ZEditorUnitTests.UITests
{
    [TestClass]
    public class UITest1
    {
        [TestMethod]
        public void ChainedStateSwitcherTest()
        {
            // check that you can enter edit modes repeatedly and escape out of each one (like popups within popups)
            var logs = new List<string>();
            StateSwitcher ss1 = new StateSwitcher();
            ss1.Focus();
            ss1.Register(new DummyComponent(logs, "1"));
            StateSwitcher ss2 = new StateSwitcher();
            ss2.Register(new DummyComponent(logs, "2"));
            StateSwitcher ss3 = new StateSwitcher();
            ss3.Register(new DummyComponent(logs, "3"));
            ss1.AddKeyFocus(Trigger.E, ss2, () => { return; }, false);
            ss2.AddKeyFocus(Trigger.E, ss3, () => { return; }, false);
            var mockInputManager = new MockInputManager();
            var uiContext = new UIContext(mockInputManager, null);
            SimulateKeyPress(mockInputManager, uiContext, Keys.P);
            foreach (var key in new[] { Keys.E, Keys.E, Keys.E, Keys.Escape, Keys.Escape, Keys.Escape })
            {
                SimulateKeyPress(mockInputManager, uiContext, key);
                SimulateKeyPress(mockInputManager, uiContext, Keys.P);
            }
            string expectedResult = "1,2,3,3,2,1,1";
            string actualResult = string.Join(",", logs);
            Assert.AreEqual(expectedResult, actualResult);
        }

        private void SimulateKeyPress(MockInputManager mockInputManager, UIContext uiContext, Keys key)
        {
            mockInputManager.SetKeysDown(key);
            ZComponent.NotifyListeners(uiContext);
            mockInputManager.SetKeysDown(null);
            ZComponent.NotifyListeners(uiContext);
        }

        private class DummyComponent : ZComponent
        {
            private List<string> logsRef;
            private string msg;

            public DummyComponent(List<string> logsRef, string msg)
            {
                this.logsRef = logsRef;
                this.msg = msg;
                RegisterListener(new InputListener(Trigger.P, x =>
                {
                    logsRef.Add(msg);
                }));
            }
        }
    }
}
