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

        private float current_scale;

        private List<Effect> postprocessing_effects;
        private RenderTarget2D _intermediateTarget1, _intermediateTarget2;
        private RenderTarget2D current_source, current_destination;
        private RenderTarget2D temp;

        private float downsampleFactor;
        private RenderTarget2D _downsampledTarget;

        public Canvas(GraphicsDevice graphicsDevice, int width, int height, float downsampleFactor = 1.0f) {
            _graphicsDevice = graphicsDevice;
            this.downsampleFactor = downsampleFactor;

            int downsampledWidth = (int)(width / downsampleFactor);
            int downsampledHeight = (int)(height / downsampleFactor);

            _target = new RenderTarget2D(_graphicsDevice, downsampledWidth, downsampledHeight);
            _intermediateTarget1 = new RenderTarget2D(_graphicsDevice, downsampledWidth, downsampledHeight);
            _intermediateTarget2 = new RenderTarget2D(_graphicsDevice, downsampledWidth, downsampledHeight);

            _downsampledTarget = new RenderTarget2D(_graphicsDevice, downsampledWidth, downsampledHeight);

            postprocessing_effects = new List<Effect>();
        }

        public Canvas(GraphicsDevice graphicsDevice, int width, int height, List<Effect> effects, float downsampleFactor = 1.0f) 
            : this(graphicsDevice, width, height, downsampleFactor) {
            postprocessing_effects = effects;
        }

        public void add_postprocessing_effect(Effect effect) {
            postprocessing_effects.Add(effect);
        }
    
        public void clear_postprocessing_effects() {
            postprocessing_effects.Clear();
        }

        public Rectangle get_destination_rectangle() {
            return _destination_rectangle;
        }

        public float get_current_scale() {
            return current_scale;
        }

        public void set_destination_rectangle() {
            var screen_size = _graphicsDevice.PresentationParameters.Bounds;

            float scale_x = (float)screen_size.Width / _target.Width;
            float scale_y = (float)screen_size.Height / _target.Height;
            float scale = Math.Min(scale_x, scale_y);
            current_scale = scale;

            int new_width = (int)(_target.Width * scale);
            int new_height = (int)(_target.Height * scale);

            int pos_x = (screen_size.Width - new_width) / 2;
            int pos_y = (screen_size.Height - new_height) / 2;

            _destination_rectangle = new Rectangle(pos_x, pos_y, new_width, new_height);
        }

        public void Activate() {
            _graphicsDevice.SetRenderTarget(_downsampledTarget);
            _graphicsDevice.Clear(Color.DarkGray);
        }

        public void Draw(SpriteBatch spriteBatch) {
            current_source = _downsampledTarget;
            current_destination = _intermediateTarget1;

            foreach (Effect e in postprocessing_effects) {
                _graphicsDevice.SetRenderTarget(current_destination);
                _graphicsDevice.Clear(Color.Transparent);
                spriteBatch.Begin(effect: e);
                spriteBatch.Draw(current_source, _target.Bounds, Color.White);
                spriteBatch.End();

                // Ping-pong the buffers
                temp = current_source;
                current_source = current_destination;
                current_destination = (current_destination == _intermediateTarget1) ? _intermediateTarget2 : _intermediateTarget1;
            }

            // Set the render target back to null to render to the screen
            _graphicsDevice.SetRenderTarget(null);
            _graphicsDevice.Clear(Color.Black);

            // Draw the upscaled image to the screen
            spriteBatch.Begin();
            spriteBatch.Draw(current_source, _destination_rectangle, Color.White);
            spriteBatch.End();
        }
    }
}