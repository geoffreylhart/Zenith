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
            StateSwitcher ss1 = new StateSwitcher(new DummyComponent(logs, "1"));
            StateSwitcher ss2 = new StateSwitcher(new DummyComponent(logs, "2"));
            StateSwitcher ss3 = new StateSwitcher(new DummyComponent(logs, "3"));
            ss1.AddKeyState(Keys.E, ss2, () => { return; });
            ss2.AddKeyState(Keys.E, ss3, () => { return; });
            var mockUIContext = new MockUIContext();
            mockUIContext.SetKeyPressed(Keys.P);
            ss1.Update(mockUIContext);
            foreach (var key in new[] { Keys.E, Keys.E, Keys.E, Keys.Escape, Keys.Escape, Keys.Escape })
            {
                mockUIContext.SetKeyPressed(key);
                ss1.Update(mockUIContext);
                mockUIContext.SetKeyPressed(Keys.P);
                ss1.Update(mockUIContext);
            }
            string actualResult = string.Join(",", logs);
            string expectedResult = "1,2,3,3,2,1,1";
            Assert.AreEqual(actualResult, expectedResult);
        }

        private class DummyComponent : ZComponent
        {
            private List<string> logsRef;
            private string msg;

            public DummyComponent(List<string> logsRef, string msg)
            {
                this.logsRef = logsRef;
                this.msg = msg;
            }

            public override void Update(IUIContext uiContext)
            {
                if (uiContext.IsKeyPressed(Keys.P))
                {
                    logsRef.Add(msg);
                }
            }
        }
    }
}
