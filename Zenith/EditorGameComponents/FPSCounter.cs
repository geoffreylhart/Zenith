using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zenith.EditorGameComponents
{
    internal class FPSCounter : DrawableGameComponent
    {
        private SpriteBatch spriteBatch;
        List<double> lastUpdateTimes = new List<double>();
        List<double> lastDrawTimes = new List<double>();

        public FPSCounter(Game game) : base(game)
        {
            this.spriteBatch = new SpriteBatch(game.GraphicsDevice);
        }

        public override void Update(GameTime gameTime)
        {
            lastUpdateTimes.Add(gameTime.ElapsedGameTime.TotalSeconds);
            if (lastUpdateTimes.Count > 5) lastUpdateTimes.RemoveAt(0);
        }

        public override void Draw(GameTime gameTime)
        {
            lastDrawTimes.Add(gameTime.ElapsedGameTime.TotalSeconds);
            if (lastDrawTimes.Count > 5) lastDrawTimes.RemoveAt(0);
            string text = $"{1 / lastUpdateTimes.Average():F2} Update FPS";
            text += $"\r\n{1 / lastDrawTimes.Average():F2} Draw FPS";
            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, DepthStencilState.Default, null, null, null);
            spriteBatch.DrawString(GlobalContent.Arial, text, new Vector2(5, 5), Color.White);
            spriteBatch.End();
        }
    }
}
