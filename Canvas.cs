using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace gate
{
    public class Canvas
    {
        private readonly RenderTarget2D _target;
        private readonly GraphicsDevice _graphicsDevice;
        private Rectangle _destination_rectangle;

        private Effect postprocessing_effect;

        public Canvas(GraphicsDevice graphicsDevice, int width, int height) {
            _graphicsDevice = graphicsDevice;
            _target = new RenderTarget2D(_graphicsDevice, width, height);
            postprocessing_effect = null;
        }

        public Canvas(GraphicsDevice graphicsDevice, int width, int height, Effect effect) {
            _graphicsDevice = graphicsDevice;
            _target = new RenderTarget2D(_graphicsDevice, width, height);
            postprocessing_effect = effect;
        }

        public void set_postprocessing_effect(Effect effect) {
            this.postprocessing_effect = effect;
        }

        public void set_destination_rectangle() {
            var screen_size = _graphicsDevice.PresentationParameters.Bounds;

            float scale_x = (float)screen_size.Width / _target.Width;
            float scale_y = (float)screen_size.Height / _target.Height;
            float scale = Math.Min(scale_x, scale_y);

            int new_width = (int)(_target.Width * scale);
            int new_height = (int)(_target.Height * scale);

            int pos_x = (screen_size.Width - new_width) / 2;
            int pos_y = (screen_size.Height - new_height) / 2;

            _destination_rectangle = new Rectangle(pos_x, pos_y, new_width, new_height);
        }

        public void Activate() {
            _graphicsDevice.SetRenderTarget(_target);
            _graphicsDevice.Clear(Color.DarkGray);
        }

        public void Draw(SpriteBatch spriteBatch) {
            _graphicsDevice.SetRenderTarget(null);
            _graphicsDevice.Clear(Color.Black);
            if (postprocessing_effect != null) { spriteBatch.Begin(effect: postprocessing_effect); } 
            else { spriteBatch.Begin(); }
            spriteBatch.Draw(_target, _destination_rectangle, Color.White);
            spriteBatch.End();
        }
    }
}