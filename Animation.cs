using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace gate
{
    public class Animation
    {
        public Rectangle source_rect;
        public int X;
        public int Y;
        public int width;
        public int height;
        public int frame_count;

        float elapsed;
        float delay;
        int frames;
        bool has_looped;

        public Animation(float delay, int frame_count, int X, int Y, int width, int height){
            this.delay = delay;
            this.frame_count = frame_count - 1;
            this.X = X;
            this.Y = Y;
            this.width = width;
            this.height = height;
            frames = 0;
            has_looped = false;
        }

        public void set_y_offset(int y_offset) {
            this.Y = y_offset;
        }

        public void set_frame_count(int frame_count) {
            this.frame_count = frame_count;
        }

        public void set_elapsed(float elapsed) {
            this.elapsed = elapsed;
        }

        public void set_frame(int frame) {
            frames = frame;
        }

        public void set_looped_once(bool value) {
            this.has_looped = value;
        }

        public bool looped_once() {
            return has_looped;
        }

        public void reset_animation() {
            frames = 0;
            has_looped = false;
            set_elapsed(0);
        }

        public float get_delay() {
            return delay;
        }

        public void Update(GameTime gameTime){
            elapsed += (float)gameTime.ElapsedGameTime.TotalMilliseconds;

            if (elapsed >= delay){
                if (frames >= frame_count){
                    frames = 0;
                    has_looped = true;
                } else {
                    frames++;
                }
                elapsed = 0;
            }

            source_rect = new Rectangle(X + frames * width, Y, width, height);
        }
    }
}