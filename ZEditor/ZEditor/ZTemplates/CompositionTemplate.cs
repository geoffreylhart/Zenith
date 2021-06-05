using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ZEditor.ZComponents.Data;
using ZEditor.ZComponents.Drawables;
using ZEditor.ZComponents.UI;
using ZEditor.ZControl;
using ZEditor.ZManage;
using static ZEditor.ZComponents.Data.ReferenceDataComponent;

namespace ZEditor.ZTemplates
{
    // potentially this class will house not just meshes, but basically all node flow between modular things
    public class CompositionTemplate : ZGameObject
    {
        private ReferenceDataComponent references;
        private Dictionary<Reference, BoxOutline> referenceOutlines = new Dictionary<Reference, BoxOutline>();
        private Selector<Reference> selector;
        private ZComponent editingItem = null;
        private bool editMode = false;

        public CompositionTemplate()
        {
            references = new ReferenceDataComponent();
            var nameText = new BasicText();
            nameText.position = new Vector2(5, 5);
            Register(references);
            var tracker = new PointCollectionTracker<Reference>(references, x =>
            {
                return (TemplateManager.LOADED_TEMPLATES[x.name].GetBoundingBox().Min + TemplateManager.LOADED_TEMPLATES[x.name].GetBoundingBox().Max) / 2;
            });
            selector = new Selector<Reference>(new CameraSelectionProvider<Reference>(tracker), x =>
            {
                referenceOutlines[x].boxColor = Color.Orange;
                nameText.text = GetSelectedText(selector.selected);
            }, x =>
            {
                referenceOutlines[x].boxColor = Color.White;
                nameText.text = GetSelectedText(selector.selected);
            });
            Register(selector, nameText);
            RegisterListener(new InputListener(Trigger.E, x =>
            {
                if (selector.selected.Count != 1) return;
                editMode = true;
                editingItem = TemplateManager.LOADED_TEMPLATES[selector.selected.Single().name];
                editingItem.Focus();
                var listener = new InputListener(Trigger.Escape, y =>
                {
                    editingItem.UnregisterListener(y);
                    this.Focus();
                    editingItem = null;
                    editMode = false;
                });
                editingItem.RegisterListener(listener);
            }));
        }

        private string GetSelectedText(HashSet<Reference> selected)
        {
            Dictionary<string, int> counts = new Dictionary<string, int>();
            foreach (var s in selected)
            {
                if (!counts.ContainsKey(s.name)) counts[s.name] = 0;
                counts[s.name]++;
            }
            return string.Join(", ", counts.ToList().OrderBy(x => x.Key).Select(x => (x.Value > 1 ? x.Value + " " + Pluralize(x.Key) : x.Key)));
        }

        private string Pluralize(string name)
        {
            if (name.EndsWith("ey")) return name.Substring(0, name.Length - 1) + "ies";
            return name + "s";
        }

        public override void Draw(GraphicsDevice graphics, Matrix world, Matrix view, Matrix projection)
        {
            base.Draw(graphics, world, view, projection);
            foreach (var reference in references)
            {
                var obj = TemplateManager.LOADED_TEMPLATES[reference.name];
                obj.Draw(graphics, world, view, projection);
            }
        }

        public override void DrawDebug(GraphicsDevice graphics, Matrix world, Matrix view, Matrix projection)
        {
            if (editMode)
            {
                editingItem.DrawDebug(graphics, world, view, projection);
            }
            else
            {
                base.DrawDebug(graphics, world, view, projection);
                foreach (var reference in references)
                {
                    var obj = TemplateManager.LOADED_TEMPLATES[reference.name];
                    obj.Draw(graphics, world, view, projection);
                }
                foreach (var reference in references)
                {
                    if (!referenceOutlines.ContainsKey(reference))
                    {
                        var obj = TemplateManager.LOADED_TEMPLATES[reference.name];
                        referenceOutlines[reference] = new BoxOutline(obj.GetBoundingBox());
                    }
                    referenceOutlines[reference].DrawDebug(graphics, world, view, projection);
                }
            }
        }

        public override void Update(UIContext uiContext)
        {
            base.Update(uiContext);
            if (editMode)
            {
                editingItem.Update(uiContext);
            }
        }
    }
}
