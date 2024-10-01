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
    public class HitSwitch : StackedObject, IEntity, ICollisionEntity
    {
        private Texture2D inactive_texture;
        public bool active;

        protected bool hurtbox_active = true;
        protected float take_hit_elapsed;
        protected float reactivate_hurtbox_threshold = 400f;
        protected float hit_speed = 0.03f;

        protected Texture2D hit_texture;
        protected HitConfirm hit_confirm;
        protected HitConfirm slash_confirm;

        protected bool show_hit_texture = false;
        protected int show_hit_texture_frame_count = 0;
        protected int show_hit_texture_frame_total = 10;
        private Vector2 hit_texture_position;
        private Vector2 hit_noise_position_offset = Vector2.Zero;
        private float noise_radius = 10f;
        private float noise_angle = 1f;

        public HitSwitch(string id, Texture2D active_texture, Texture2D inactive_texture, Vector2 base_position, float scale, float width, float height, int stack_frame_count, int stack_distance, float rotation_degrees, int ID) 
            : base(id, active_texture, base_position, scale, width, height, stack_frame_count, stack_distance, rotation_degrees, ID) {
            //set active to true initially
            active = true;
            //set inactive texture (NOTE: this.texture = active_texture)
            this.inactive_texture = inactive_texture;
        }

        public override void Update(GameTime gameTime, float rotation) {
            this.rotation = -rotation;
            depth_sort_position = draw_position + 1 * new Vector2(direction_down.X * (float)Math.Cos(-rotation) - direction_down.Y * (float)Math.Sin(-rotation), direction_down.Y * (float)Math.Cos(-rotation) + direction_down.X * (float)Math.Sin(-rotation));
            //update collision once to properly instantiate for rotation and then don't update anymore
            if (update_once) {
                hitbox.update(rotation, draw_position);
                update_once = false;
            }

            //set hurtbox active again
            if (!hurtbox_active) { //check if hurtbox is not active
                //add to hurtbox cooldown and color change cooldown
                take_hit_elapsed += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
            }
            if (take_hit_elapsed >= reactivate_hurtbox_threshold) {
                hurtbox_active = true;
                take_hit_elapsed = 0;
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
        }

        public override void take_hit(IEntity entity, int damage) {
            //set active to false
            active = false;
            //turn off hurt box for a bit
            hurtbox_active = false;
            //show hit texture
            show_hit_texture = true;
            //calculate hit texture direction
            hit_texture_position = get_base_position() - entity.get_base_position();
            hit_texture_position = Constant.rotate_point(hit_texture_position, rotation, 0.1f, Constant.direction_up);
            hit_texture_position.Normalize();
            hit_texture_position *= Vector2.Distance(get_base_position(), entity.get_base_position())/2;
            hit_texture_position += entity.get_base_position();
            hit_confirm = new HitConfirm(Constant.hit_confirm_spritesheet, hit_texture_position, 1f, 100f);
            //calcualte noise to add for slash effect
            hit_noise_position_offset = new Vector2((float)(Math.Sin(noise_angle) * noise_radius), (float)(Math.Cos(noise_angle) * noise_radius));
            slash_confirm = new HitConfirm(Constant.slash_confirm_spritesheet, hit_texture_position + hit_noise_position_offset, 1f, 10f);
        }

        public bool is_active() {
            return active;
        }

        public override bool is_hurtbox_active() {
            return hurtbox_active;
        }

        public override void Draw(SpriteBatch spriteBatch) {
            Vector2 offset = Vector2.Zero;
            for (int i = 0; i < sprite_rectangles.Count; i++) {
                Vector2 dir = new Vector2(0, -i*stack_distance);
                Vector2 draw_pos = draw_position + new Vector2(dir.X*(float)Math.Cos(rotation) - dir.Y*(float)Math.Sin(rotation), dir.Y*(float)Math.Cos(rotation) + dir.X*(float)Math.Sin(rotation));
                float rotation_calc = (-rotation + rotation_offset)/1000;
                //draw appropriate texture based on active status
                if (active) {
                    spriteBatch.Draw(texture, draw_pos+offset, sprite_rectangles[i], Color.White, rotation_calc, rotation_point, scale, SpriteEffects.None, 0f);
                } else {
                    spriteBatch.Draw(inactive_texture, draw_pos+offset, sprite_rectangles[i], Color.White, rotation_calc, rotation_point, scale, SpriteEffects.None, 0f);
                }
            }

            //draw hits
            if (show_hit_texture && hit_confirm != null) {
                hit_confirm.Draw(spriteBatch);
            }
            if (slash_confirm != null) {
                slash_confirm.Draw(spriteBatch);
            }
        }
    }
}