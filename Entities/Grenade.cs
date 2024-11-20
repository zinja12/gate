using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using gate.Serialize;
using gate.Interface;
using gate.Core;
using gate.Collision;
using gate.Particles;

namespace gate.Entities
{
    public class Grenade : IEntity
    {
        public Vector2 base_position;
        public Vector2 draw_position;
        public Vector2 depth_sort_position;
        public Vector2 parent_position;
        public Vector2 direction;

        public Texture2D texture;

        private Vector2 rotation_point;
        private float scale;
        private float rotation = 0.0f, rotation_offset = 0f;
        private Vector2 direction_down = new Vector2(0, 1);
        private Vector2 direction_up = new Vector2(0, -1);
        private Random random;
        private Rectangle source_rect = new Rectangle(0, 0, 16, 16);

        private int size;
        private float angle_offset;
        private Vector2 aim_direction;

        //particle like variable to determine if stopped
        private bool dead = false;

        //attack vars
        private Vector2 movement_direction;
        private float speed = 6f, speed_multiplier = 1f;
        private bool fired = false;
        private RRect hitbox;

        public static bool debug = false;

        ParticleSystem trail_particle_system;

        public Grenade(Vector2 base_position, float scale, Texture2D texture, int size, Vector2 parent_position, Vector2 direction) {
            this.base_position = base_position;
            this.draw_position = new Vector2(base_position.X - (texture.Width / 2), 
                                            base_position.Y - texture.Height);
            this.depth_sort_position = this.draw_position + new Vector2(0, texture.Height / 2);
            this.scale = scale;
            this.rotation_point = new Vector2(texture.Width / 2, texture.Height / 2);

            this.parent_position = parent_position;

            this.texture = texture;
            this.size = size;

            //calculate angle offset with direction
            this.angle_offset = Constant.get_angle(base_position, direction);

            this.hitbox = new RRect(this.draw_position, 16, 16);

            random = new Random();

            trail_particle_system = new ParticleSystem(true, draw_position, 2, 250f, 2, 1, 3, Constant.white_particles, new List<Texture2D>() { Constant.footprint_tex });
        }

        public void Update(GameTime gameTime, float rotation) {
            this.rotation = rotation;
            depth_sort_position = Constant.rotate_point(draw_position, rotation, (texture.Height/2), Constant.direction_down);

            //handle grenade movement once fired
            if (fired) {
                move_grenade(movement_direction, gameTime);
            }

            hitbox.update(rotation, draw_position);

            trail_particle_system.Update(gameTime, rotation);
            trail_particle_system.set_position(draw_position);
        }

        public void update_aim(Vector2 center_position, Vector2 direction) {
            //set the parent position and the direction passed in
            this.parent_position = center_position;
            this.aim_direction = direction;
            //calculate the angle from zero
            //this is because the angle we are passing in needs to be set at the origin
            float direction_angle = Constant.get_angle(Vector2.Zero, direction);
            //rotate the angle by 90 degrees to account for the texture of the arrow being straight up and down (expressed in radians because the angle is already being calculated in radians)
            this.angle_offset = direction_angle + ((float)Math.PI*0.5f);
        }

        public void move_grenade(Vector2 direction, GameTime gameTime) {
            //calculate which direction is up
            Vector2 vertical_component = Constant.rotate_point(Constant.direction_up, -rotation, (size/2), Constant.direction_up);
            Vector2 up = new Vector2(-(float)Math.Sin(rotation), -(float)Math.Cos(rotation));
            up.Normalize();
            Vector2 gravity_component = Constant.rotate_point(Constant.direction_down, rotation, (size/2), Constant.direction_down);
            vertical_component.Normalize();
            gravity_component.Normalize();
            //Console.WriteLine($"vertical_component:{vertical_component}");
            //Console.WriteLine($"gravity_component:{gravity_component}");

            Vector2 final = up*2 + gravity_component*4 * speed_multiplier;
            //clamp
            if (final.X < -1) {
                final.X = -1;
            }
            if (final.X > 1) {
                final.X = 1;
            }
            if (final.Y < -1) {
                final.Y = -1;
            }
            if (final.Y > 1) {
                final.Y = 1;
            }
            
            //Console.WriteLine($"final:{final}");
            
            Vector2 g_direction = direction + (-final);

            base_position += (g_direction * speed);
            draw_position += (g_direction * speed);
            depth_sort_position = Constant.rotate_point(draw_position, rotation, (size/2), Constant.direction_down);

            //handle slowing down
            if (speed_multiplier <= 0f) {
                speed_multiplier = 0f;
                dead = true;
            } else if (speed_multiplier > 0f) {
                speed_multiplier -= 0.04f;
            }
        }

        public void fire_grenade(Vector2 direction, float speed_multiplier) {
            movement_direction = direction;
            movement_direction.Normalize();
            this.speed_multiplier = speed_multiplier;
            fired = true;
        }

        public bool hitbox_active() {
            return fired;
        }

        public bool is_fired() {
            return fired;
        }

        public bool is_dead() {
            return dead;
        }

        public RRect get_hitbox() {
            return hitbox;
        }

        public void update_animation(GameTime gameTime) {
            //no animation needed
        }

        public int get_obj_ID_num() {
            return -1;
        }

        public void set_obj_ID_num(int value) {
            //not needed
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
            return "grenade";
        }

        public GameWorldObject to_world_level_object() {
            return new GameWorldObject {
                object_identifier = "grenade"
            };
        }

        public void Draw(SpriteBatch spriteBatch) {
            //draw particle system
            trail_particle_system.Draw(spriteBatch);
            //draw texture
            spriteBatch.Draw(texture, draw_position, source_rect, Color.White, angle_offset, rotation_point, scale, SpriteEffects.None, 0f);
        }
    }
}