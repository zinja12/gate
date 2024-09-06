using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace gate
{
    public class StackedObject : IEntity, ICollisionEntity
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

        private float object_width = 32;
        private float object_height = 32;

        private int stack_count = 18, stack_distance = 1;
        private List<Rectangle> sprite_rectangles;
        private Vector2 rotation_factor;

        //collision information
        private RRect hitbox;
        private List<Vector2> hitbox_normals;
        
        private bool update_once = true;

        private bool sway = false;
        private float sway_elapsed0, sway_elapsed1, sway_elapsed2, sway_threshold = 3000f;
        private Random random;

        private Texture2D texture;
        private string id;
        private int ID;

        public StackedObject(string id, Texture2D texture, Vector2 base_position, float scale, float width, float height, int stack_frame_count, int stack_distance, float rotation_degrees, int ID) {
            this.object_width = width;
            this.object_height = height;
            this.texture = texture;
            this.stack_count = stack_frame_count;
            this.stack_distance = stack_distance;
            this.id = id;

            this.base_position = base_position;
            this.draw_position = new Vector2(base_position.X - (object_width / 2), 
                                            base_position.Y - object_height);
            this.depth_sort_position = this.draw_position + new Vector2(0, object_height / 2);
            this.scale = scale;
            this.rotation_point = new Vector2(object_width / 2, object_height / 2);

            //generate sprite rectangles for stack
            sprite_rectangles = Constant.generate_rectangles_for_stack(this.texture, stack_count);
            this.rotation_factor = this.draw_position;

            this.hitbox = new RRect(this.draw_position, 20, 5);
            this.hitbox_normals = new List<Vector2>();

            this.ID = ID;

            set_rotation_offset(rotation_degrees);
            
            random = new Random();
            sway_elapsed0 = (float)random.Next(0, (int)sway_threshold*2);
            sway_elapsed1 = (float)random.Next(0, (int)sway_threshold*2);
            sway_elapsed2 = (float)random.Next(0, (int)sway_threshold*2);
        }

        public void Update(GameTime gameTime, float rotation) {
            this.rotation = -rotation;
            depth_sort_position = draw_position + 1 * new Vector2(direction_down.X * (float)Math.Cos(-rotation) - direction_down.Y * (float)Math.Sin(-rotation), direction_down.Y * (float)Math.Cos(-rotation) + direction_down.X * (float)Math.Sin(-rotation));
            //update collision once to properly instantiate for rotation and then don't update anymore
            if (update_once) {
                hitbox.update(rotation, draw_position);
                update_once = false;
            }
            
            //handle sway elapsed
            if (sway) {
                sway_elapsed0 += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
                sway_elapsed1 += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
                sway_elapsed2 += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
                if (sway_elapsed0 >= sway_threshold*2) { sway_elapsed0 = 0f; }
                if (sway_elapsed1 >= sway_threshold*2) { sway_elapsed1 = 0f; }
                if (sway_elapsed2 >= sway_threshold*2) { sway_elapsed2 = 0f; }
            }
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

        public void set_sway(bool value) {
            this.sway = value;
        }

        public bool check_hitbox_collisions(RRect collision_rect) {
            return hitbox.collision(collision_rect);
        }

        public void take_hit(IEntity entity, int damage) {}

        public RRect get_hurtbox() {
            return hitbox;
        }

        public bool is_hurtbox_active() {
            return false;
        }

        public void set_base_position(Vector2 position) {
            base_position = position;
            this.draw_position = new Vector2(base_position.X - (object_width / 2), 
                                            base_position.Y - object_height);
        }

        public float get_rotation_offset() {
            return rotation_offset;
        }

        public void set_rotation_offset(float rotation_offset_degrees) {
            float radians_offset = MathHelper.ToRadians(rotation_offset_degrees);
            this.rotation_offset = radians_offset;
            hitbox.set_rotation_offset(-rotation_offset_degrees/1000);
            update_once = true;
        }

        public GameWorldObject to_world_level_object() {
            return new GameWorldObject {
                object_identifier = id,
                object_id_num = get_obj_ID_num(),
                x_position = base_position.X,
                y_position = base_position.Y,
                scale = get_scale(),
                rotation = MathHelper.ToDegrees(get_rotation_offset())
            };
        }

        public string get_id() {
            return id;
        }

        public void Draw(SpriteBatch spriteBatch) {
            //calculate the number of sprites that will be moving (round down to make sure we don't try to access any sprites that are out of bounds)
            int stack_div3 = (int)Math.Floor((double)stack_count/3);
            //update draw positions so that everything draws in the right direction
            Vector2 offset = Vector2.Zero;
            for (int i = 0; i < sprite_rectangles.Count; i++) {
                if (sway) {
                    if (i < stack_div3) { //first third of the stack
                        if (sway_elapsed0 < sway_threshold) {
                            //set offset left
                            offset.X = 1;
                        } else if (sway_elapsed0 >= sway_threshold) {
                            //set offset right
                            offset.X = -1;
                        }
                    } else if (i >= stack_div3 && i < stack_div3*2) { //second third of the stack
                        if (sway_elapsed1 < sway_threshold) {
                            //set offset right
                            offset.X = 1;
                        } else if (sway_elapsed1 >= sway_threshold) {
                            //set offset left
                            offset.X = -1;
                        }
                    } else { //last third of the stack
                        if (sway_elapsed2 < sway_threshold) {
                            //set offset right
                            offset.X = -2;
                        } else if (sway_elapsed2 >= sway_threshold) {
                            //set offset left
                            offset.X = 2;
                        }
                    }
                }
                Vector2 dir = new Vector2(0, -i*stack_distance);
                Vector2 draw_pos = draw_position + new Vector2(dir.X*(float)Math.Cos(rotation) - dir.Y*(float)Math.Sin(rotation), dir.Y*(float)Math.Cos(rotation) + dir.X*(float)Math.Sin(rotation));
                float rotation_calc = (-rotation + rotation_offset)/1000;
                spriteBatch.Draw(texture, draw_pos+offset, sprite_rectangles[i], Color.White, rotation_calc, rotation_point, scale, SpriteEffects.None, 0f);
            }
            if (debug){
                hitbox.draw(spriteBatch);
                Renderer.DrawALine(spriteBatch, Constant.pixel, 2, Color.Red, 1f, base_position, base_position + new Vector2(0, -10));
                Renderer.DrawALine(spriteBatch, Constant.pixel, 2, Color.Green, 1f, draw_position, draw_position + new Vector2(0, -10));
                Renderer.DrawALine(spriteBatch, Constant.pixel, 2, Color.Yellow, 1f, depth_sort_position, depth_sort_position + new Vector2(0, -10));
                Renderer.DrawALine(spriteBatch, Constant.pixel, 2, Color.Pink, 1f, draw_position + rotation_point, draw_position + rotation_point + new Vector2(0, -10));
                spriteBatch.DrawString(Constant.arial, $"ID:{this.ID}", draw_position + new Vector2(0, 20), Color.White, -rotation, Vector2.Zero, 0.12f, SpriteEffects.None, 0f);
            }
        
            //spriteBatch.DrawString(Constant.arial, "depth: " + depth, draw_position, Color.White);
        }
    }
}