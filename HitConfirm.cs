using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace gate
{
    public class HitConfirm
    {
        public Vector2 draw_position;
        public Vector2 depth_sort_position;

        private float rotation;
        private Vector2 rotation_point;
        private float scale;

        private Texture2D texture_spritesheet;
        private int rect_size = 32;
        
        private Animation animation;
        private float frame_count;
        private float animation_duration;

        public HitConfirm(Texture2D texture_spritesheet, Vector2 base_position, float scale, float animation_duration) {
            this.texture_spritesheet = texture_spritesheet;
            this.rect_size = (int)texture_spritesheet.Height;
            this.draw_position = new Vector2(base_position.X - (rect_size / 2), 
                                            base_position.Y - rect_size);
            this.depth_sort_position = this.draw_position + new Vector2(0, rect_size / 2);
            this.scale = scale;
            this.rotation_point = new Vector2(rect_size / 2, rect_size / 2);

            //calculate frame count based on spritesheet
            this.frame_count = texture_spritesheet.Width / rect_size;
            this.animation_duration = animation_duration;
            this.animation = new Animation(animation_duration, (int)this.frame_count, 0, 0, (int)rect_size, (int)rect_size);
        }

        public void Update(GameTime gameTime, float rotation) {
            this.rotation = rotation;
            depth_sort_position = Constant.rotate_point(draw_position, rotation, (rect_size/2), Constant.direction_down);
            
            //update animation
            update_animation(gameTime);
        }

        public void update_animation(GameTime gameTime) {
            animation.Update(gameTime);
        }

        public void Draw(SpriteBatch spriteBatch) {
            spriteBatch.Draw(texture_spritesheet, draw_position, animation.source_rect, Color.White, -rotation, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
    }
}