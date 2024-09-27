using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using gate.Particles;
using gate.Serialize;
using gate.Interface;
using gate.Core;

namespace gate.Entities
{
    public class Ghastly : IEntity, ICollisionEntity
    {
        public Vector2 base_position;
        public Vector2 draw_position;
        public Vector2 depth_sort_position;
        public Vector2 particle_system_offset;

        private RRect hurtbox;
        private bool hurtbox_active = true;
        private float take_hit_elapsed;
        private float reactivate_hurtbox_threshold = 800f;
        private Vector2 hit_direction;
        private float hit_speed = 0.03f;
        private float take_hit_color_change_elapsed;
        private float color_change_threshold = 200f;
        private int hit_flash_count = 0;

        private Color draw_color = Color.White;

        private Vector2 rotation_point;
        private float scale;
        private float rotation = 0.0f, rotation_offset = 0f;
        //private Animation animation;
        private int rect_size;

        private ParticleSystem particle_system;

        private Texture2D texture;
        private Texture2D hit_texture;
        private HitConfirm hit_confirm;
        private HitConfirm slash_confirm;

        private bool show_hit_texture = false;
        private int show_hit_texture_frame_count = 0;
        private int show_hit_texture_frame_total = 10;
        private Vector2 hit_texture_position;
        private Vector2 hit_texture_rotation_point;
        private Vector2 hit_noise_position_offset = Vector2.Zero;
        private float noise_radius = 10f;
        private float noise_angle = 1f;

        public static bool debug = false;

        private int ID;

        public Ghastly(Texture2D texture, Vector2 base_position, float scale, Texture2D hit_texture, int ID) {
            this.texture = texture;
            this.base_position = base_position;
            this.draw_position = new Vector2(base_position.X - (texture.Width / 2), 
                                            base_position.Y - texture.Height);
            this.depth_sort_position = this.draw_position + new Vector2(0, texture.Height / 2);
            this.scale = scale;
            this.rotation_point = new Vector2(texture.Width / 2, texture.Height / 2);
            this.rect_size = texture.Width;

            this.particle_system = new ParticleSystem(true, this.draw_position, 2, 1000f, 2, 1, 3, Constant.red_particles, new List<Texture2D>() { Constant.footprint_tex });

            this.hurtbox = new RRect(this.draw_position, texture.Width, texture.Height);

            this.hit_texture = hit_texture;
            this.hit_texture_rotation_point = new Vector2(hit_texture.Width/2, hit_texture.Height/2);

            this.ID = ID;
        }

        public void Update(GameTime gameTime, float rotation) {
            this.rotation = rotation;
            particle_system.set_position(Constant.rotate_point(draw_position, rotation, 1f, Constant.direction_up));
            particle_system.Update(gameTime, rotation);
            depth_sort_position = draw_position + (texture.Height/2) * new Vector2(Constant.direction_down.X * (float)Math.Cos(-rotation) - Constant.direction_down.Y * (float)Math.Sin(-rotation), Constant.direction_down.Y * (float)Math.Cos(-rotation) + Constant.direction_down.X * (float)Math.Sin(-rotation));

            //set hurtbox active again
            if (!hurtbox_active) { //check if hurtbox is not active
                //add to hurtbox cooldown and color change cooldown
                take_hit_elapsed += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
                take_hit_color_change_elapsed += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
                //check for secondary and tertiary flashes
                if (take_hit_color_change_elapsed*2 < color_change_threshold) {
                    draw_color = Color.Red;
                }
                //rotate hit_direction based on camera
                hit_direction = Constant.rotate_point(hit_direction, rotation, 1f, Constant.direction_down);
                //move enemy in hit direction after taking the hit
                draw_position += hit_direction * hit_speed;
            }
            if (take_hit_elapsed >= reactivate_hurtbox_threshold) {
                hurtbox_active = true;
                take_hit_elapsed = 0;
            }
            if (take_hit_color_change_elapsed >= color_change_threshold) {
                draw_color = Color.White;
                take_hit_color_change_elapsed = 0;
                hit_flash_count++;
            }

            //show hit texture for certain amount of frames
            if (show_hit_texture_frame_count < show_hit_texture_frame_total && show_hit_texture) {
                show_hit_texture_frame_count++;
                if (hit_confirm != null)
                    hit_confirm.Update(gameTime, rotation);
                if (slash_confirm != null)
                    slash_confirm.Update(gameTime, rotation);
            } else {
                hit_confirm = null;
                slash_confirm = null;
                show_hit_texture = false;
                show_hit_texture_frame_count = 0;
            }

            //update collision
            hurtbox.update(rotation, draw_position);
            //update animation
            update_animation(gameTime);
        }

        public void update_animation(GameTime gameTime) {

        }

        public bool is_hurtbox_active() {
            return hurtbox_active;
        }

        public RRect get_hurtbox() {
            return hurtbox;
        }

        public string get_id() {
            return "ghastly";
        }

        public void set_obj_ID_num(int id_num) {
            this.ID = id_num;
        }

        public int get_obj_ID_num() {
            return this.ID;
        }

        public void set_base_position(Vector2 position) {
            this.draw_position = new Vector2(position.X - (texture.Width / 2), 
                                            position.Y - texture.Height);
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
                object_identifier = "ghastly",
                object_id_num = get_obj_ID_num(),
                x_position = base_position.X,
                y_position = base_position.Y,
                scale = get_scale(),
                rotation = get_rotation_offset()
            };
        }

        public void take_hit(IEntity entity, int damage) {
            //turn off hurt box for a bit
            hurtbox_active = false;
            //calculate hit direction
            hit_direction = get_base_position() - entity.get_base_position();
            //set color to red to indicate hit taken
            draw_color = Color.Red;
            //show hit texture
            show_hit_texture = true;
            //calculate hit texture direction
            hit_texture_position = hit_direction;
            hit_texture_position.Normalize(); //normalize (unit vector)
            hit_texture_position *= (-texture.Width/4); //set length with direction from this object to entity
            hit_texture_position += draw_position; //offset from draw_position
            hit_confirm = new HitConfirm(Constant.hit_confirm_spritesheet, hit_texture_position, 1f, 100f);
            //calcualte noise to add for slash effect
            hit_noise_position_offset = new Vector2((float)(Math.Sin(noise_angle) * noise_radius), (float)(Math.Cos(noise_angle) * noise_radius));
            slash_confirm = new HitConfirm(Constant.slash_confirm_spritesheet, hit_texture_position + hit_noise_position_offset, 1f, 10f);
        }

        public Vector2 get_base_position() {
            return depth_sort_position;
        }

        public float get_scale() {
            return scale;
        }

        public void set_scale(float scale_value) {
            this.scale = scale_value;
        }

        public string get_flag(){
            return Constant.ENTITY_ACTIVE;
        }

        public void Draw(SpriteBatch spriteBatch) {
            //draw shadow
            spriteBatch.Draw(Constant.shadow_tex, depth_sort_position, null, Color.Black * 0.5f, -rotation, rotation_point, scale, SpriteEffects.None, 0f);
            //draw particle system
            particle_system.Draw(spriteBatch);
            //draw image
            spriteBatch.Draw(texture, draw_position, null/*animation.source_rect*/, draw_color, -rotation + rotation_offset, rotation_point, scale, SpriteEffects.None, 0f);
            //draw hits
            if (show_hit_texture && hit_confirm != null) {
                hit_confirm.Draw(spriteBatch);
                //spriteBatch.Draw(hit_texture, hit_texture_position, null, Color.White * 0.8f, hit_texture_rotation, hit_texture_rotation_point, scale, SpriteEffects.None, 0f);
            }
            if (slash_confirm != null) {
                slash_confirm.Draw(spriteBatch);
            }
            if (debug){
                hurtbox.draw(spriteBatch);
                Renderer.DrawALine(spriteBatch, Constant.pixel, 2, Color.Pink, 1f, draw_position, draw_position + new Vector2(0, -5));
                Renderer.DrawALine(spriteBatch, Constant.pixel, 2, Color.Yellow, 1f, depth_sort_position, depth_sort_position + new Vector2(0, -5));
            }
        }
    }
}