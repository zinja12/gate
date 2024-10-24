using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using gate.Serialize;
using gate.Interface;
using gate.Core;

namespace gate.Entities
{
    public class Footprints : IEntity
    {
        public Vector2 base_position;
        public Vector2 draw_position;
        public Vector2 depth_sort_position;

        private Vector2 rotation_point;
        private float scale;
        private float tex_rotation;
        private float rotation = 0.0f, rotation_offset = 0f;
        private Vector2 direction_down = new Vector2(0, 1);
        private Vector2 direction_up = new Vector2(0, -1);

        private Texture2D texture;
        private Color color;

        public static bool debug = false;

        private float lifecycle;
        public bool reap;
        private float decay_rate;

        public Footprints(Vector2 base_position, float scale, Texture2D texture, float tex_rotation, float lifecycle, float decay_rate, Color color) {
            //set texture
            this.texture = texture;

            this.base_position = base_position;
            this.draw_position = new Vector2(base_position.X - (this.texture.Width / 2), 
                                            base_position.Y - this.texture.Height);
            this.depth_sort_position = this.draw_position + new Vector2(0, this.texture.Height / 2);
            this.scale = scale;
            this.rotation_point = new Vector2(this.texture.Width / 2, this.texture.Height / 2);
            this.lifecycle = lifecycle;
            this.decay_rate = decay_rate;
            this.reap = false;
            
            this.tex_rotation = tex_rotation;
            this.color = color;
        }

        public Footprints(Vector2 base_position, float scale, Texture2D texture, float tex_rotation, float lifecycle, float decay_rate, Color color, Vector2 draw_position) {
            //set texture
            this.texture = texture;

            this.draw_position = draw_position;
            this.base_position = new Vector2(draw_position.X + (this.texture.Width / 2), 
                                            draw_position.Y + this.texture.Height);
            this.depth_sort_position = this.draw_position + new Vector2(0, this.texture.Height / 2);
            this.scale = scale;
            this.rotation_point = new Vector2(this.texture.Width / 2, this.texture.Height / 2);
            this.lifecycle = lifecycle;
            this.decay_rate = decay_rate;
            this.reap = false;
            
            this.tex_rotation = tex_rotation;
            this.color = color;
        }

        public void Update(GameTime gameTime, float rotation) {
            //subtract from lifecycle and check for time to reap
            lifecycle -= decay_rate;
            if (lifecycle <= 0) {
                reap = true;
            }

            //update rotation and depth
            this.rotation = rotation;
            this.rotation += rotation_offset;
            depth_sort_position = draw_position + (this.texture.Height/2) * new Vector2(direction_down.X * (float)Math.Cos(-rotation) - direction_down.Y * (float)Math.Sin(-rotation), direction_down.Y * (float)Math.Cos(-rotation) + direction_down.X * (float)Math.Sin(-rotation));
        }

        public void update_animation(GameTime gameTime) {

        }

        public Vector2 get_base_position() {
            return depth_sort_position;
        }

        public float get_current_lifecycle() {
            return lifecycle;
        }

        public Vector2 get_draw_position() {
            return draw_position;
        }

        public float get_scale() {
            return scale;
        }

        public void set_scale(float scale_value) {
            this.scale = scale_value;
        }

        public string get_id() {
            return "footprints";
        }

        public int get_obj_ID_num() {
            return -1;
        }

        public void set_obj_ID_num(int value) {
            //do nothing, not needed
        }

        public string get_flag() {
            return Constant.ENTITY_PASSIVE;
        }

        public void set_base_position(Vector2 position) {
            base_position = position;
            this.draw_position = new Vector2(base_position.X - (this.texture.Width / 2), 
                                            base_position.Y - this.texture.Height);
        }

        public float get_rotation_offset() {
            return rotation_offset;
        }

        public void set_rotation_offset(float rotation_offset_degrees) {
            float radians_offset = MathHelper.ToRadians(rotation_offset_degrees);
            rotation_offset = radians_offset;
        }

        public GameWorldObject to_world_level_object() {
            return new GameWorldObject {
                object_identifier = "footprint"
            };
        }

        public void Draw(SpriteBatch spriteBatch) {
            spriteBatch.Draw(this.texture, draw_position, null, color * lifecycle, tex_rotation, rotation_point, scale, SpriteEffects.None, 0f);
            //spriteBatch.Draw(Constant.tree2_tex, draw_position, null, Color.White, -rotation, rotation_point, scale, SpriteEffects.None, 0f);
            if (debug) {
                Renderer.DrawALine(spriteBatch, Constant.pixel, 2, Color.Red, 1f, base_position, base_position + new Vector2(0, -20));
                Renderer.DrawALine(spriteBatch, Constant.pixel, 2, Color.Green, 1f, draw_position, draw_position + new Vector2(0, -30));
                Renderer.DrawALine(spriteBatch, Constant.pixel, 2, Color.Yellow, 1f, depth_sort_position, depth_sort_position + new Vector2(0, -10));   
            }
        
            //spriteBatch.DrawString(Constant.arial, "depth: " + depth, draw_position, Color.White);
        }
    }
}