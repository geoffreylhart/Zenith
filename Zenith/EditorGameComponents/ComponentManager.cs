using Microsoft.Xna.Framework;
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
        private List<ComponentAndLabel> components;
        private ComponentList list;

        internal ComponentManager(params IEditorGameComponent[] components)
        {
            this.components = new List<ComponentAndLabel>();
            foreach (var component in components)
            {
                RecursiveAddComponent(component, "");
            }
            // TODO: why do we have this again?
            foreach (var component in components)
            {
                if (component is DrawableGameComponent)
                {
                    ((DrawableGameComponent)component).Enabled = true;
                }
            }
        }

        private void RecursiveAddComponent(IEditorGameComponent component, string prefix)
        {
            components.Add(new ComponentAndLabel(component, prefix + component.GetType().Name));
            foreach (var c in component.GetSubComponents())
            {
                RecursiveAddComponent(c, prefix + "     ");
            }
        }

        private class ComponentAndLabel
        {
            public IEditorGameComponent component;
            public string label;

            public ComponentAndLabel(IEditorGameComponent component, string label)
            {
                this.component = component;
                this.label = label;
            }
        }

        internal void Init(UILayer uiLayer)
        {
            Tabs tabs = new Tabs(3, 500, 200);
            tabs.titles[0] = "Debug";
            tabs.titles[1] = "Actions";
            tabs.titles[2] = "Settings";
            tabs.panels[0].Components.Add(new DebugLabel(this));
            tabs.panels[2] = new ComponentSettingsPanel(this);
            list = new ComponentList(300, components);
            uiLayer.Add(tabs, 20, 350);
            uiLayer.Add(list, 490, 10);
        }

        private class DebugLabel : Label
        {
            private ComponentManager cm;

            public DebugLabel(ComponentManager cm) : base()
            {
                this.cm = cm;
            }

            internal override string GetText()
            {
                return String.Join("\n", cm.components[cm.list.activeIndex].component.GetDebugInfo());
            }
        }

        private class ComponentList : ListBox<ComponentAndLabel>
        {
            public ComponentList(int w, List<ComponentAndLabel> components) : base(w, components)
            {
            }

            internal override string GetItemAsString(ComponentAndLabel item)
            {
                return item.label;
            }
        }

        private class ComponentSettingsPanel : Panel
        {
            private ComponentManager cm;
            private List<IUIComponent>[] componentSettings;

            public ComponentSettingsPanel(ComponentManager cm) : base()
            {
                this.cm = cm;
                this.componentSettings = new List<IUIComponent>[cm.components.Count];
                for (int i = 0; i < this.componentSettings.Length; i++)
                {
                    this.componentSettings[i] = cm.components[i].component.GetSettings();
                }
            }

            public override List<IUIComponent> Components { get { return componentSettings[cm.list.activeIndex]; } }
        }
    }
}
