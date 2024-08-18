using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace gate
{
    public class Sign : IEntity
    {
        public Vector2 base_position;
        public Vector2 draw_position;
        public Vector2 depth_sort_position;
        public Vector2 interaction_display_position;

        private Vector2 rotation_point;
        private float scale;
        private float rotation = 0.0f, rotation_offset = 0f;
        private int rect_size;

        private RRect interaction_box;

        private TextBox textbox;
        private bool display_text = false, display_interaction = false;
        List<(string, string)> messages;
        List<string> raw_messages;

        private Texture2D texture;

        public static bool debug = false;

        private int ID;

        public Sign(Texture2D texture, Vector2 base_position, float scale, List<string> messages, int ID) {
            this.texture = texture;
            this.base_position = base_position;
            this.draw_position = new Vector2(base_position.X - (texture.Width / 2), 
                                            base_position.Y - texture.Height);
            this.depth_sort_position = this.draw_position + new Vector2(0, texture.Height / 2);
            this.scale = scale;
            this.rotation_point = new Vector2(texture.Width / 2, texture.Height / 2);
            this.rect_size = texture.Width;
            this.interaction_display_position = new Vector2(depth_sort_position.X, depth_sort_position.Y);

            this.interaction_box = new RRect(this.draw_position, texture.Width*1.5f, texture.Height*1.5f);

            this.raw_messages = messages;
            this.messages = add_sign_speaker_to_messages(messages);

            //initialize textbox
            textbox = new TextBox(Constant.textbox_screen_position, Constant.arial_mid_reg, this.messages, "sign", Constant.textbox2_width, Constant.textbox2_height, Color.White, Color.Black);

            this.ID = ID;
        }

        public List<(string, string)> add_sign_speaker_to_messages(List<string> messages) {
            List<(string, string)> speaker_messages = new List<(string, string)>();
            string speaker = "Sign";
            foreach (string msg in messages) {
                speaker_messages.Add((speaker, msg));
            }
            return speaker_messages;
        }

        public void Update(GameTime gameTime, float rotation) {
            this.rotation = rotation;
            depth_sort_position = Constant.rotate_point(draw_position, rotation, (texture.Height/2), Constant.direction_down);
            this.interaction_display_position = Constant.rotate_point(draw_position, rotation, (-Constant.Y_tex.Height), Constant.direction_down);


            //if displaying text
            if (display_text && !textbox.text_ended()) {
                //update textbox
                textbox.Update(gameTime);
            }

            if (textbox.text_ended()) {
                display_text = false;
            }

            //update interaction box
            interaction_box.update(rotation, draw_position);
        }

        public void update_animation(GameTime gameTime) {}

        public string get_flag() {
            return Constant.ENTITY_ACTIVE;
        }

        public TextBox get_textbox() {
            return textbox;
        }

        public RRect get_interaction_box() {
            return interaction_box;
        }

        public void display_textbox() {
            display_text = true;
        }

        public void set_obj_ID_num(int id_num) {
            this.ID = id_num;
        }

        public int get_obj_ID_num() {
            return this.ID;
        }

        public Vector2 get_base_position() {
            return depth_sort_position;
        }

        public string get_id() {
            return "sign";
        }

        public void set_base_position(Vector2 position) {
            this.draw_position = new Vector2(position.X - (texture.Width / 2), 
                                            position.Y - texture.Height);
        }

        public void set_display_interaction(bool display_interaction) {
            this.display_interaction = display_interaction;
        }

        public bool get_display_interaction() {
            return display_interaction;
        }

        public Vector2 get_overhead_position() {
            Vector2 overhead_position = Constant.rotate_point(interaction_display_position, rotation, (-Constant.textbox2_height/2), Constant.direction_down);
            return overhead_position;
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
                object_identifier = "sign",
                object_id_num = get_obj_ID_num(),
                x_position = get_base_position().X,
                y_position = get_base_position().Y,
                scale = get_scale(),
                rotation = get_rotation_offset(),
                sign_messages = raw_messages
            };
        }

        public float get_scale() {
            return scale;
        }

        public void set_scale(float scale_value) {
            this.scale = scale_value;
        }

        public void Draw(SpriteBatch spriteBatch) {
            //draw shadow
            spriteBatch.Draw(Constant.shadow_tex, draw_position, null, Color.Black * 0.5f, -rotation, rotation_point, scale, SpriteEffects.None, 0f);
            //draw sign
            spriteBatch.Draw(texture, draw_position, null, Color.White, -rotation + rotation_offset, rotation_point, scale, SpriteEffects.None, 0f);
            //display interaction
            if (display_interaction && !display_text) {
                spriteBatch.Draw(Constant.Y_tex, interaction_display_position, null, Color.White, -rotation + rotation_offset, rotation_point, scale, SpriteEffects.None, 0f);
            }
            if (debug) {
                interaction_box.draw(spriteBatch);
            }
        }
    }
}