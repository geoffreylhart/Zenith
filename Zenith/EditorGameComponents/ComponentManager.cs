using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zenith.EditorGameComponents.UIComponents;

namespace Zenith.EditorGameComponents
{
    internal class ComponentManager
    {
        private List<EditorGameComponent> components;
        private ComponentList list;

        internal ComponentManager(params EditorGameComponent[] components)
        {
            this.components = components.ToList();
            foreach(var component in components)
            {
                component.Enabled = true;
            }
        }

        internal void Init(UILayer uiLayer)
        {
            Tabs tabs = new Tabs(1, 20, 300, 500, 200);
            tabs.titles[0] = "Debug";
            tabs.panels[0].Add(new DebugLabel(this));
            list = new ComponentList(430, 10, 200, components);
            uiLayer.Add(tabs);
        }

        private class DebugLabel : Label
        {
            private ComponentManager cm;

            public DebugLabel(ComponentManager cm) : base(0, 0)
            {
                this.cm = cm;
            }

            internal override string GetText()
            {
                return String.Join("\n", cm.components[cm.list.activeIndex].GetDebugInfo());
            }
        }

        private class ComponentList : ListBox<EditorGameComponent>
        {
            public ComponentList(int x, int y, int w, List<EditorGameComponent> components) : base(x, y, w, components)
            {
            }

            internal override string GetItemAsString(EditorGameComponent item)
            {
                return item.GetType().Name;
            }
        }
    }
}
