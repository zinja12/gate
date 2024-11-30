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
using gate.Collision;

namespace gate.Entities
{
    public class StackedObject : IEntity, ICollisionEntity
    {
        public Vector2 base_position;
        public Vector2 draw_position;
        public Vector2 depth_sort_position;
        public Vector2 interaction_display_position;

        protected Vector2 rotation_point;
        protected float scale;
        protected float rotation = 0.0f, rotation_offset = 0f;
        protected Vector2 direction_down = new Vector2(0, 1);
        protected Vector2 direction_up = new Vector2(0, -1);

        public static bool debug = false;

        protected float object_width = 32;
        protected float object_height = 32;

        protected int stack_count = 18, stack_distance = 1;
        protected List<Rectangle> sprite_rectangles;
        protected Vector2 rotation_factor;

        //collision information
        protected RRect hitbox;
        protected List<Vector2> hitbox_normals;
        
        protected bool update_once = true;

        private bool sway = false;
        private float sway_elapsed0, sway_elapsed1, sway_elapsed2, sway_threshold = 3000f;
        private Random random;

        protected RRect interaction_box;
        private bool interaction = false, display_interaction = false;
        private Texture2D interaction_texture;

        protected Texture2D texture;
        protected string id;
        protected int ID;

        public StackedObject(string id, Texture2D texture, Vector2 base_position, float scale, float width, float height, int stack_frame_count, int stack_distance, float rotation_degrees, int ID, bool interaction = false, Texture2D custom_interaction_tex = null) {
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

            if (interaction) {
                Console.WriteLine("setting up interaction box");
                this.interaction = interaction;
                this.interaction_display_position = new Vector2(depth_sort_position.X, depth_sort_position.Y);
                this.interaction_box = new RRect(this.draw_position, width*2.5f, height*2.5f);
                if (custom_interaction_tex != null) {
                    this.interaction_texture = custom_interaction_tex;
                }
            }
        }

        public virtual void Update(GameTime gameTime, float rotation) {
            this.rotation = -rotation;
            depth_sort_position = draw_position + 1 * new Vector2(direction_down.X * (float)Math.Cos(-rotation) - direction_down.Y * (float)Math.Sin(-rotation), direction_down.Y * (float)Math.Cos(-rotation) + direction_down.X * (float)Math.Sin(-rotation));
            //update collision once to properly instantiate for rotation and then don't update anymore
            if (update_once) {
                hitbox.update(rotation, draw_position);
                update_once = false;
            }

            //update interaction box
            if (interaction) {
                this.interaction_display_position = Constant.rotate_point(draw_position, rotation, (-object_height*1.5f), Constant.direction_down);
                interaction_box.update(rotation, draw_position);
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

        public RRect get_interaction_box() {
            //return the interaction box if this stacked object is interactable, otherwise null
            return interaction ? interaction_box : null;
        }

        public Vector2 get_overhead_position() {
            Vector2 overhead_position = Constant.rotate_point(interaction_display_position, rotation, (-object_height/2), Constant.direction_down);
            return overhead_position;
        }

        public void set_display_interaction(bool display_interaction) {
            this.display_interaction = display_interaction;
        }

        public bool get_display_interaction() {
            return display_interaction;
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

        public virtual void take_hit(IEntity entity, int damage) {}

        public RRect get_hurtbox() {
            return hitbox;
        }

        public virtual bool is_hurtbox_active() {
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

        public virtual GameWorldObject to_world_level_object() {
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

        public virtual void Draw(SpriteBatch spriteBatch) {
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

            if (display_interaction) {
                if (interaction_texture != null) {
                    spriteBatch.Draw(interaction_texture, interaction_display_position, null, Color.White, rotation + rotation_offset, new Vector2(8, 8), scale, SpriteEffects.None, 0f);
                } else {
                    spriteBatch.Draw(Constant.read_interact_tex, interaction_display_position, null, Color.White, rotation + rotation_offset, new Vector2(8, 8), scale, SpriteEffects.None, 0f);
                }
            }

            if (debug){
                hitbox.draw(spriteBatch);
                if (interaction) {
                    interaction_box.draw(spriteBatch);
                }
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