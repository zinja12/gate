using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace gate
{
    public class UIButton
    {
        private Vector2 position;
        private RRect collision_rect;
        private string label;

        public UIButton(Vector2 position, float size, string label) {
            this.position = position;
            this.collision_rect = new RRect(position, size, size);
            this.collision_rect.set_color(Color.Blue);
            this.label = label;
        }

        public void Update(GameTime gameTime, float rotation) {
            collision_rect.update(rotation, position);
        }

        public bool check_clicked(RRect r, bool input_clicked) {
            return collision_rect.collision(r) && input_clicked;
        }

        public bool trigger_action() {
            collision_rect.set_color(Color.Blue);
            return true;
        }

        public RRect get_collision_rect() {
            return collision_rect;
        }

        public void Draw(SpriteBatch spriteBatch) {
            collision_rect.draw(spriteBatch);
            spriteBatch.DrawString(Constant.arial_small, label, position, Color.White);
        }
    }
}