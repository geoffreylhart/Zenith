using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace ZEditor.ZControl
{
    public class Trigger
    {
        public static Trigger A = new Trigger(Keys.A, false, false, false, false, false, false);
        public static Trigger E = new Trigger(Keys.E, false, false, false, false, false, false);
        public static Trigger F = new Trigger(Keys.F, false, false, false, false, false, false);
        public static Trigger G = new Trigger(Keys.G, false, false, false, false, false, false);
        public static Trigger H = new Trigger(Keys.H, false, false, false, false, false, false);
        public static Trigger P = new Trigger(Keys.P, false, false, false, false, false, false);
        public static Trigger Delete = new Trigger(Keys.Delete, false, false, false, false, false, false);
        public static Trigger Escape = new Trigger(Keys.Escape, false, false, false, false, false, false);
        public static Trigger CtrlS = new Trigger(Keys.S, false, true, false, false, false, false);
        public static Trigger ShiftA = new Trigger(Keys.A, true, false, false, false, false, false);
        public static Trigger LeftMouseClick = new Trigger(null, false, false, true, false, false, false);
        public static Trigger ReftMouseClick = new Trigger(null, false, false, false, true, false, false);
        public static Trigger MiddleMouseClick = new Trigger(null, false, false, false, false, true, false);
        public static Trigger PlainLeftMouseClick = new Trigger(null, false, false, true, false, false, true);
        public static Trigger ShiftLeftMouseClick = new Trigger(null, true, false, true, false, false, false);

        public Keys? key;
        public bool isShift;
        public bool isCtrl;
        public bool isLeftMouse;
        public bool isRightMouse;
        public bool isMiddleMouse;
        public bool isExclusive; // no shift or ctrl allowed unless indicated

        public Trigger(Keys? key, bool isShift, bool isCtrl, bool isLeftMouse, bool isRightMouse, bool isMiddleMouse, bool isExclusive)
        {
            this.key = key;
            this.isShift = isShift;
            this.isCtrl = isCtrl;
            this.isLeftMouse = isLeftMouse;
            this.isRightMouse = isRightMouse;
            this.isMiddleMouse = isMiddleMouse;
            this.isExclusive = isExclusive;
        }
    }
}
