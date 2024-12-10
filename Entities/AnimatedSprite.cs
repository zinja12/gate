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
using gate.Particles;

namespace gate.Entities
{
    public class AnimatedSprite : BillboardSprite
    {
        protected Animation sprite_animation;

        public AnimatedSprite(Texture2D texture, Vector2 base_position, float scale, float animation_duration, int frame_count, int start_x, int start_y, int anim_width, int anim_height, string identifier, int ID, bool background_particle_system = false, Texture2D custom_interaction_tex = null)
            : base(texture, base_position, scale, identifier, ID, background_particle_system, custom_interaction_tex) {
            //init here
            this.sprite_animation = new Animation(animation_duration, frame_count, start_x, start_y, anim_width, anim_height);
            this.draw_position = new Vector2(base_position.X - (anim_width / 2), 
                                            base_position.Y - anim_height);
            this.depth_sort_position = this.draw_position + new Vector2(0, anim_height / 2);
            this.rotation_point = new Vector2(anim_width / 2, anim_height / 2);
            this.rect_size = anim_width;
            this.interaction_display_position = new Vector2(depth_sort_position.X, depth_sort_position.Y);

            this.interaction_box = new RRect(this.draw_position, anim_width*1.5f, anim_height*1.5f);
            this.hitbox = new RRect(this.draw_position, anim_width, anim_height);

            this.ID = ID;
            this.identifier = identifier;

            if (background_particle_system) {
                ps = new ParticleSystem(true, this.draw_position, 1, 800f, 10, 1, 3, Constant.black_particles, new List<Texture2D> { Constant.footprint_tex });
            }

            if (custom_interaction_tex != null) {
                this.interact_texture = custom_interaction_tex;
            }
        }

        public override void update_animation(GameTime gameTime) {
            sprite_animation.Update(gameTime);
        }

        public override void draw_sprite(SpriteBatch spriteBatch) {
            //draw animated texture
            spriteBatch.Draw(texture, draw_position, sprite_animation.source_rect, Color.White, -rotation + rotation_offset, rotation_point, scale, SpriteEffects.None, 0f);
        }
    }
}