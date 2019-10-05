using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zenith.EditorGameComponents.UIComponents;
using Zenith.Helpers;
using Zenith.MathHelpers;
using Zenith.ZMath;

namespace Zenith.EditorGameComponents
{
    internal class ShipComponent : DrawableGameComponent
    {
        public Vector3d velocity = new Vector3d(0, 0, 0);
        public SphereVector forward = new SphereVector(0, 0, 1);
        public SphereVector position = new SphereVector(0, -1, 0);
        public double zoom = 1;
        private EditorCamera camera;

        public override void Update(GameTime gameTime)
        {
            if (!Game1.recording) MoveShip();
        }

        private void MoveShip()
        {
            double rotationSpeed = 0;
            double accel = 0;
            Keyboard.GetState().AffectNumber(ref rotationSpeed, Keys.Left, Keys.Right, Keys.A, Keys.D, 0.05);
            Keyboard.GetState().AffectNumber(ref accel, Keys.Down, Keys.Up, Keys.S, Keys.W, 0.0002);
            // apply rotation

            SphereVector right = new SphereVector(forward.Cross(position).Normalized());
            forward = forward.WalkTowards(right, rotationSpeed);

            // move forwards
            velocity += forward * accel * Math.Pow(0.5, zoom) * 20;
            position = new SphereVector((position + velocity).Normalized());
            // flatten our forward/velocity against current position
            forward = new SphereVector(forward.Cross(position).Cross(-position).Normalized());
            if (velocity.Length() > 0.0001)
            {
                velocity = velocity.Cross(position).Cross(-position).Normalized() * velocity.Length();
            }
            else
            {
                velocity = velocity.Cross(position).Cross(-position);
            }
            // now update altitude
            Keyboard.GetState().AffectNumber(ref zoom, Keys.Space, Keys.LeftShift, 0.03);
            // update the camera from the ship info
            SphereVector unitPosition2 = new SphereVector(position.Normalized());
            camera.cameraRotX = unitPosition2.ToLongLat().X;
            camera.cameraRotY = unitPosition2.ToLongLat().Y;
            //camera.cameraZoom = Math.Log((position.Length() - 1) / 9) / Math.Log(0.5);
            camera.cameraZoom = zoom;
            camera.UpdateCamera();
        }

        public ShipComponent(Game game, EditorCamera camera) : base(game)
        {
            this.camera = camera;
        }

        public override void Draw(GameTime gameTime)
        {
            if (Game1.recording) MoveShip();
            float pixelsPerUnit = 32;
            SphereVector unitPosition = new SphereVector(position.Normalized());
            SphereVector up = unitPosition.WalkNorth(Math.PI / 2);
            SphereVector right = new SphereVector(up.Cross(unitPosition).Normalized());
            double rotation = Math.Atan2(forward.Dot(-right), forward.Dot(up)); // we want up to be 0 and a positive rotation to be cw
            Matrix world = Matrix.CreateRotationZ((float)rotation);
            Matrix view = Matrix.CreateLookAt(new Vector3(0, 0, 20), new Vector3(0, 0, 0), -Vector3.UnitY);
            //Matrix projection = Matrix.CreateOrthographicOffCenter(GraphicsDevice.Viewport.Width / pixelsPerUnit / 2, -GraphicsDevice.Viewport.Width / pixelsPerUnit / 2, GraphicsDevice.Viewport.Height / pixelsPerUnit / 2, -GraphicsDevice.Viewport.Height / pixelsPerUnit / 2, 1, 1000); // note: we've flipped this from usual to match blender coordinates?
            Matrix projection = Matrix.CreatePerspectiveFieldOfView(Mathf.PI / 2, this.Game.GraphicsDevice.Viewport.AspectRatio, 0.1f, 100);
            foreach (ModelMesh mesh in GlobalContent.StartingShuttle.Meshes)
            {
                foreach (BasicEffect eff in mesh.Effects)
                {
                    eff.EnableDefaultLighting();
                    eff.World = world;
                    eff.View = view;
                    eff.Projection = projection;
                    eff.VertexColorEnabled = false;
                    eff.Alpha = 1;
                }

                mesh.Draw();
            }
        }
    }
}
