using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ZEditor.ZComponents.Data;
using ZEditor.ZComponents.Drawables;
using ZEditor.ZComponents.UI;
using ZEditor.ZManage;
using static ZEditor.ZComponents.Data.ReferenceDataComponent;

namespace ZEditor.ZTemplates
{
    // potentially this class will house not just meshes, but basically all node flow between modular things
    public class CompositionTemplate : ZGameObject
    {
        private ReferenceDataComponent references;
        private Dictionary<Reference, BoxOutline> referenceOutlines = new Dictionary<Reference, BoxOutline>();

        public CompositionTemplate()
        {
            references = new ReferenceDataComponent();
            Register(references);
            var tracker = new PointCollectionTracker<Reference>(references, x =>
            {
                return (TemplateManager.LOADED_TEMPLATES[x.name].GetBoundingBox().Min + TemplateManager.LOADED_TEMPLATES[x.name].GetBoundingBox().Max) / 2;
            });
            var selector = new Selector<Reference>(new CameraSelectionProvider<Reference>(tracker),
                x => { referenceOutlines[x].boxColor = Color.Orange; },
                x => { referenceOutlines[x].boxColor = Color.White; });
            Register(selector);
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
            Draw(graphics, world, view, projection);
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
}
