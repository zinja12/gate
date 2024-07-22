using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace gate
{
    public class Grass : IEntity
    {
        public Vector2 base_position;
        public Vector2 draw_position;
        public Vector2 depth_sort_position;

        private Vector2 rotation_point;
        private float scale;
        private float rotation = 0.0f, rotation_offset = 0f;
        private Vector2 direction_down = new Vector2(0, 1);
        private Vector2 direction_up = new Vector2(0, -1);
        private Animation animation;
        private Random random;
        private int rect_size = 32;

        public static bool debug = false;

        private int ID;

        public Grass(Vector2 base_position, float scale, int ID) {
            this.base_position = base_position;
            this.draw_position = new Vector2(base_position.X - (Constant.grass_tex.Height / 2), 
                                            base_position.Y - Constant.grass_tex.Height);
            this.depth_sort_position = this.draw_position + new Vector2(0, Constant.grass_tex.Height / 2);
            this.scale = scale;
            this.rotation_point = new Vector2(Constant.grass_tex.Height / 2, Constant.grass_tex.Height / 2);

            random = new Random();
            int animation_duration = random.Next(1200, 1800);
            this.animation = new Animation((float)animation_duration, 2, 0, 0, rect_size, rect_size);
            
            this.ID = ID;
        }

        public void Update(GameTime gameTime, float rotation) {
            this.rotation = rotation;
            depth_sort_position = Constant.rotate_point(draw_position, rotation, (Constant.grass_tex.Height/2), Constant.direction_down);
        }

        public void update_animation(GameTime gameTime) {
            //update animation
            animation.Update(gameTime);
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
            return Constant.ENTITY_PASSIVE;
        }

        public void set_obj_ID_num(int id_num) {
            this.ID = id_num;
        }

        public int get_obj_ID_num() {
            return this.ID;
        }

        public void set_base_position(Vector2 position) {
            draw_position = position;
        }

        public float get_rotation_offset() {
            return rotation_offset;
        }

        public void set_rotation_offset(float rotation_offset_degrees) {
            float radians_offset = MathHelper.ToRadians(rotation_offset_degrees);
            rotation_offset = radians_offset;
        }

        public string get_id() {
            return "grass";
        }

        public GameWorldObject to_world_level_object() {
            return new GameWorldObject {
                object_identifier = "grass",
                object_id_num = get_obj_ID_num(),
                x_position = draw_position.X,
                y_position = draw_position.Y,
                scale = get_scale(),
                rotation = get_rotation_offset()
            };
        }

        public void Draw(SpriteBatch spriteBatch) {
            spriteBatch.Draw(Constant.grass_tex, draw_position, animation.source_rect, Color.White, -rotation + rotation_offset, rotation_point, scale, SpriteEffects.None, 0f);
            //spriteBatch.Draw(Constant.tree2_tex, draw_position, null, Color.White, -rotation, rotation_point, scale, SpriteEffects.None, 0f);
            if (debug){
                Renderer.DrawALine(spriteBatch, Constant.pixel, 2, Color.Red, 1f, base_position, base_position + new Vector2(0, -10));
                Renderer.DrawALine(spriteBatch, Constant.pixel, 2, Color.Green, 1f, draw_position, draw_position + new Vector2(0, -10));
                Renderer.DrawALine(spriteBatch, Constant.pixel, 2, Color.Yellow, 1f, depth_sort_position, depth_sort_position + new Vector2(0, -10));   
            }
        
            //spriteBatch.DrawString(Constant.arial, "depth: " + depth, draw_position, Color.White);
        }
    }
}