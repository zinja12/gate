using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using gate.Serialize;
using gate.Interface;
using gate.Core;

namespace gate.Entities
{
    public class Lamppost : IEntity
    {
        public Vector2 base_position;
        public Vector2 draw_position;
        public Vector2 depth_sort_position;

        private Vector2 rotation_point;
        private float scale;
        private float rotation = 0.0f, rotation_offset = 0f;
        private Vector2 direction_down = new Vector2(0, 1);
        private Vector2 direction_up = new Vector2(0, -1);

        public static bool debug = false;

        private float lamppost_width = 32;
        private float lamppost_height = 32;

        private int stack_count = 26;
        private List<Rectangle> sprite_rectangles;
        private Vector2 rotation_factor;

        private int ID;

        public Lamppost(Vector2 base_position, float scale, int ID) {
            this.base_position = base_position;
            this.draw_position = new Vector2(base_position.X - (lamppost_width / 2), 
                                            base_position.Y - lamppost_height);
            this.depth_sort_position = this.draw_position + new Vector2(0, lamppost_height / 2);
            this.scale = scale;
            this.rotation_point = new Vector2(lamppost_width / 2, lamppost_height / 2);

            //generate sprite rectangles for stack
            sprite_rectangles = Constant.generate_rectangles_for_stack(Constant.lamppost_spritesheet, stack_count);
            this.rotation_factor = this.draw_position;

            this.ID = ID;
        }

        public void Update(GameTime gameTime) {
            this.rotation = 0f;
            depth_sort_position = draw_position + (lamppost_height/2) * new Vector2(direction_down.X * (float)Math.Cos(-rotation) - direction_down.Y * (float)Math.Sin(-rotation), direction_down.Y * (float)Math.Cos(-rotation) + direction_down.X * (float)Math.Sin(-rotation));
        }

        public void update_animation(GameTime gameTime) {
            //update animation
        }

        public Vector2 get_base_position(){
            return depth_sort_position;
        }

        public float get_scale(){
            return scale;
        }

        public void set_scale(float scale_value) {
            this.scale = scale_value;
        }

        public string get_flag(){
            return Constant.ENTITY_ACTIVE;
        }

        public void set_obj_ID_num(int id_num) {
            this.ID = id_num;
        }

        public int get_obj_ID_num() {
            return this.ID;
        }

        public void set_base_position(Vector2 position) {
            base_position = position;
            this.draw_position = new Vector2(base_position.X - (lamppost_width / 2), 
                                            base_position.Y - lamppost_height);
        }

        public string get_id() {
            return "lamppost";
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
                object_identifier = "lamp",
                object_id_num = get_obj_ID_num(),
                x_position = base_position.X,
                y_position = base_position.Y,
                scale = get_scale(),
                rotation = MathHelper.ToDegrees(get_rotation_offset())
            };
        }

        public void Draw(SpriteBatch spriteBatch) {
            //update draw positions so that everything draws in the right direction
            for (int i = 0; i < sprite_rectangles.Count; i++){
                Vector2 dir = new Vector2(0, -i*Constant.stack_distance);
                Vector2 draw_pos = draw_position + new Vector2(dir.X*(float)Math.Cos(rotation) - dir.Y*(float)Math.Sin(rotation), dir.Y*(float)Math.Cos(rotation) + dir.X*(float)Math.Sin(rotation));
                spriteBatch.Draw(Constant.lamppost_spritesheet, draw_pos, sprite_rectangles[i], Color.White, (-rotation + rotation_offset)/1000, rotation_point, scale, SpriteEffects.None, 0f);
            }
            if (debug){
                Renderer.DrawALine(spriteBatch, Constant.pixel, 2, Color.Red, 1f, base_position, base_position + new Vector2(0, -100));
                Renderer.DrawALine(spriteBatch, Constant.pixel, 2, Color.Green, 1f, draw_position, draw_position + new Vector2(0, -100));
                Renderer.DrawALine(spriteBatch, Constant.pixel, 2, Color.Yellow, 1f, depth_sort_position, depth_sort_position + new Vector2(0, -10));
                Renderer.DrawALine(spriteBatch, Constant.pixel, 2, Color.Pink, 1f, draw_position + rotation_point, draw_position + rotation_point + new Vector2(0, -10));
            }
        
            //spriteBatch.DrawString(Constant.arial, "depth: " + depth, draw_position, Color.White);
        }
    }
}