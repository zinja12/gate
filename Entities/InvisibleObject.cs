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
    public class InvisibleObject : IEntity, ICollisionEntity
    {
        public Vector2 base_position;
        public Vector2 draw_position;
        public Vector2 depth_sort_position;

        private Vector2 rotation_point;
        private float scale;
        private float rotation = 0.0f, rotation_offset = 0f;
        private Vector2 direction_down = new Vector2(0, 1);
        private Vector2 direction_up = new Vector2(0, -1);

        public bool debug = false;

        private float object_width = 32;
        private float object_height = 32;

        //collision information
        private RRect hitbox;
        private List<Vector2> hitbox_normals;
        
        private bool update_once = true;
        private string id;
        private int ID;

        public InvisibleObject(string id, Vector2 base_position, float scale, float width, float height, float rotation_degrees, int ID) {
            this.object_width = width;
            this.object_height = height;
            this.id = id;

            this.base_position = base_position;
            this.draw_position = new Vector2(base_position.X - (object_width / 2), 
                                            base_position.Y - object_height);
            this.depth_sort_position = this.draw_position + new Vector2(0, object_height / 2);
            this.scale = scale;
            this.rotation_point = new Vector2(object_width / 2, object_height / 2);

            this.hitbox = new RRect(this.draw_position, object_width, object_height);
            this.hitbox_normals = new List<Vector2>();

            set_rotation_offset(rotation_degrees);

            this.ID = ID;
        }

        public void Update(GameTime gameTime) {
            this.rotation = 0f;
            depth_sort_position = draw_position + (object_height/2) * new Vector2(direction_down.X * (float)Math.Cos(-rotation) - direction_down.Y * (float)Math.Sin(-rotation), direction_down.Y * (float)Math.Cos(-rotation) + direction_down.X * (float)Math.Sin(-rotation));
            //update collision once to properly instantiate for rotation and then don't update anymore
            if (update_once) {
                hitbox.update(rotation, draw_position);
                update_once = false;
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

        public void set_debug(bool debug_active) {
            this.debug = debug_active;
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
            if (debug){
                hitbox.draw(spriteBatch);
                Renderer.DrawALine(spriteBatch, Constant.pixel, 2, Color.Red, 1f, base_position, base_position + new Vector2(0, -10));
                Renderer.DrawALine(spriteBatch, Constant.pixel, 2, Color.Green, 1f, draw_position, draw_position + new Vector2(0, -10));
                Renderer.DrawALine(spriteBatch, Constant.pixel, 2, Color.Yellow, 1f, depth_sort_position, depth_sort_position + new Vector2(0, -10));
                Renderer.DrawALine(spriteBatch, Constant.pixel, 2, Color.Pink, 1f, draw_position + rotation_point, draw_position + rotation_point + new Vector2(0, -10));
            }
        
            //spriteBatch.DrawString(Constant.arial, "depth: " + depth, draw_position, Color.White);
        }
    }
}