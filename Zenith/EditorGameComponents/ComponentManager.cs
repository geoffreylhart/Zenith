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
            // TODO: why do we have this again?
            foreach (var component in components)
            {
                component.Enabled = true;
            }
        }

        internal void Init(UILayer uiLayer)
        {
            Tabs tabs = new Tabs(3, 20, 350, 500, 200);
            tabs.titles[0] = "Debug";
            tabs.titles[1] = "Actions";
            tabs.titles[2] = "Settings";
            tabs.panels[0].Components.Add(new DebugLabel(this));
            tabs.panels[2] = new ComponentSettingsPanel(this);
            tabs.panels[2].X = tabs.X;
            tabs.panels[2].Y = tabs.Y;
            list = new ComponentList(490, 10, 300, components);
            uiLayer.Add(tabs);
            uiLayer.Add(list);
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

        private class ComponentSettingsPanel : Panel
        {
            private ComponentManager cm;

            public ComponentSettingsPanel(ComponentManager cm) : base()
            {
                this.cm = cm;
            }

            public override List<IUIComponent> Components { get { return cm.components[cm.list.activeIndex].GetSettings(); } }
        }
    }
}
