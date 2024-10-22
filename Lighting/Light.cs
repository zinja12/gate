using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using gate.Interface;
using gate.Core;

namespace gate.Lighting
{
    public class Light
    {
        private Vector2 center_position;
        
        private float radius;
        private float intensity;
        private Color color;

        public Light(Vector2 position, float radius, float intensity, Color color) {
            this.center_position = position;
            this.radius = radius;
            this.intensity = intensity;
            this.color = color;
        }

        public void Update(GameTime gameTime, float rotation, Vector2? parent = null) {
            if (parent.HasValue) {
                center_position = parent.Value;
            }
        }

        public Vector2 get_center_position() {
            return center_position;
        }

        public Vector2 get_draw_position() {
            return center_position - new Vector2(radius, radius);
        }

        public float get_radius() {
            return radius;
        }

        public Color get_color() {
            return color;
        }

        public float get_intensity() {
            return intensity;
        }
    }
}