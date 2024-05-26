using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace gate
{
    public class EditorTextBox
    {
        public string current_text { get; set; }
        public Vector2 current_text_position;
        public Vector2 cursor_position;
        public int animation_time;
        public bool visible;
        public float layer_depth;
        public Vector2 position;
        public bool selected;
        public int cell_width;
        public int cell_height;
        private int _cursor_width;
        private int _cursor_height;
        private int _length;
        private bool _numeric_only;
        private Texture2D _texture;
        private Texture2D _cursor_texture;
        private Point _cursor_dimensions;
        private SpriteFont _font;

        public EditorTextBox(Texture2D texture,
                            Texture2D _cursor_texture,
                            Point dimensions,
                            Point cursor_dimensions,
                            Vector2 position,
                            int length,
                            bool numeric_only,
                            bool visible,
                            SpriteFont font,
                            string text,
                            float layer_depth) {
            _texture = texture;
            cell_width = dimensions.X;
            cell_height = dimensions.Y;
            _cursor_width = cursor_dimensions.X;
            _cursor_height = cursor_dimensions.Y;
            _length = length;
            _numeric_only = numeric_only;
            animation_time = 0;
            this.visible = visible;
            this.layer_depth = layer_depth;
            cursor_position = new Vector2(position.X+7, position.Y+6);
            current_text_position = new Vector2(position.X+7, position.Y+3);
            current_text = "";
            this._cursor_texture = _cursor_texture;
            _cursor_dimensions = cursor_dimensions;
            selected = false;
            _font = font;
            current_text = text;
        }

        public void Update() {
            animation_time++;
        }

        public bool is_flashing_cursor_visible() {
            int time = animation_time % 60;

            if (time >= 0 && time < 31) {
                return true;
            }
            return false;
        }

        public void add_more_text(char text) {
            Vector2 spacing = new Vector2();
            KeyboardState keyboard_state = Keyboard.GetState();
            bool lower_character = true;
            
            if (keyboard_state.CapsLock || keyboard_state.IsKeyDown(Keys.LeftShift) || keyboard_state.IsKeyDown(Keys.RightShift)) {
                lower_character = false;
            }

            if (_numeric_only && (int)Char.GetNumericValue(text) < 0 || (int)Char.GetNumericValue(text) > 9) { // do not allow non numeric chars
                if (text != '\b') { //backspace char
                    return;
                }
            }

            if (text != '\b') { //backspace char
                if (current_text.Length > _length) {
                    if (lower_character)
                        text = Char.ToLower(text);

                    current_text += text;
                    spacing = _font.MeasureString(text.ToString());
                    cursor_position = new Vector2(cursor_position.X + spacing.X, cursor_position.Y);
                }
            } else { //backspace or delete char
                if (current_text.Length > 0) {
                    spacing = _font.MeasureString(current_text.Substring(current_text.Length - 1));

                    current_text = current_text.Remove(current_text.Length - 1, 1);
                    cursor_position = new Vector2(cursor_position.X - spacing.X, cursor_position.Y);
                }
            }
        }

        public void Render(SpriteBatch spriteBatch) {
            if (visible) {
                spriteBatch.Draw(_texture, position, Color.White); //draw background image
                spriteBatch.DrawString(_font, current_text, current_text_position, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, layer_depth);

                //draw cursor
                if (selected && is_flashing_cursor_visible()) {
                    Rectangle source_rect = new Rectangle(0, 0, _cursor_width, _cursor_height);
                    Rectangle destination_rect = new Rectangle((int)cursor_position.X, (int)cursor_position.Y, _cursor_width, _cursor_height);

                    spriteBatch.Draw(_cursor_texture, destination_rect, source_rect, Color.White, 0f, Vector2.Zero, SpriteEffects.None, layer_depth);
                }
            }
        }
    }
}