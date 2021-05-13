using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using ZEditor.ZControl;
using ZEditor.ZManage;

namespace ZEditor.ZComponents.UI
{
    // tracks mouse movement from the perspective of a camera
    // TODO: probably take inspiration from the android gestures code
    public class CameraMouseTracker : ZComponent
    {
        public float stepSize = 1;

        public Action<Vector3> OnStepDiff = x => { };
        public Vector3 worldOrigin;
        public Vector2 mouseOrigin;
        public Vector3? oldOffset;

        public override void Update(UIContext uiContext)
        {
            Vector3 offset = uiContext.Camera.GetPerspectiveOffset(uiContext, worldOrigin, uiContext.MouseVector2 - mouseOrigin);
            if (oldOffset.HasValue)
            {
                Vector3 oldOffsetRounded = Round(oldOffset.Value);
                Vector3 currOffsetRounded = Round(offset);
                Vector3 diff = currOffsetRounded - oldOffsetRounded;
                if (diff.X != 0 || diff.Y != 0 || diff.Z != 0)
                {
                    OnStepDiff(diff);
                }
            }
            oldOffset = offset;
        }

        private Vector3 Round(Vector3 v)
        {
            return new Vector3((float)Math.Round(v.X / stepSize) * stepSize, (float)Math.Round(v.Y / stepSize) * stepSize, (float)Math.Round(v.Z / stepSize) * stepSize);
        }
    }
}
