using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using gate;

namespace gate.Particles
{
    public class Particle
    {
        //particle properties
        public Vector2 draw_position;
        public Vector2 depth_sort_position;
        public Vector2 direction;
        public float speed;

        private float particle_size, scale;
        private bool fade;
        private float life_duration;
        private float elapsed_life;
        private bool dead;

        private float rotation;

        private Texture2D texture;
        private List<Color> colors;
        private int color_idx, color_idx_offset;

        private Random random;

        public Particle(Vector2 position, Vector2 direction, float speed, bool fade, float life_duration, Texture2D texture, float scale, List<Color> colors) {
            this.draw_position = position;
            this.direction = direction;
            this.speed = speed;
            this.fade = fade;
            this.life_duration = life_duration;
            this.dead = false;
            this.texture = texture;
            this.particle_size = texture.Width;
            this.scale = scale;
            this.colors = colors;
            this.color_idx = 0;

            this.random = new Random();

            color_idx_offset = random.Next(0, colors.Count);
        }

        public void update(GameTime gameTime, float rotation) {
            //set current rotation
            this.rotation = rotation;

            //modify position based on direction and speed
            draw_position += direction * speed;

            //check if particle is past it's lifetime
            if (elapsed_life >= life_duration) {
                this.dead = true;
            }

            //calculate life_fraction based off elapsed life so we loop through colors
            float life_fraction = elapsed_life / life_duration;
            //set color idx and factor in offset in order to randomly assign colors that iterate progressively through life of particle
            color_idx = ((int)(life_fraction * (colors.Count))) + color_idx_offset;
            //wrap idx to prevent out of bounds
            if (color_idx > colors.Count - 1) {
                color_idx = colors.Count - 1;
            }

            //add to 
            elapsed_life += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
        }

        public Color get_color() {
            return colors[color_idx];
        }

        public bool is_dead() {
            return dead;
        }

        public void draw(SpriteBatch spriteBatch) {
            //draw postion
            spriteBatch.Draw(this.texture, draw_position, null, colors[color_idx] * (400/elapsed_life), 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
    }
}