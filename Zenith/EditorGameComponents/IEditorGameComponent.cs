using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zenith.EditorGameComponents.UIComponents;

namespace Zenith.EditorGameComponents
{
    public interface IEditorGameComponent
    {
        List<String> GetDebugInfo();

        List<IUIComponent> GetSettings();

        List<IEditorGameComponent> GetSubComponents();
    }
}
