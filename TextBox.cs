using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json.Linq;
using gate.Core;

namespace gate
{
    public class TextBox
    {
        public Vector2 position;

        protected Vector2 text_offset = new Vector2(30, 20);
        protected Vector2 box_title_offset = new Vector2(0, -20);
        protected Vector2 text_draw_position = Vector2.Zero;
        protected Vector2 char_offset = Vector2.Zero;

        protected SpriteFont font;
        protected List<(string, string, string)> msgs;
        protected List<(string, List<(string, string)>)> speaker_msg_screens;
        protected string current_msg;
        protected int current_msg_index, current_msg_screen_idx;
        protected float width, height, original_width, original_height;
        protected Color box_color, text_color;
        protected bool end_of_text = false;
        protected int current_msg_char_idx, previous_msg_char_idx;

        protected string box_title_name;

        protected float advance_message_elapsed;
        protected float advance_message_cooldown = 500f;

        protected float background_opacity = 0.85f;
        protected float max_line_width;

        protected float text_scale = 5f;
        protected float line_spacing = 40f;

        private Random random;

        public TextBox(Vector2 position, SpriteFont font, List<(string, string, string)> msgs, string box_title_name, float width, float height, Color box_color, Color text_color) {
            this.position = position;
            this.font = font;
            this.msgs = msgs;
            this.width = width;
            this.height = height;
            this.original_width = width;
            this.original_height = height;
            this.box_color = box_color;
            this.text_color = text_color;
            this.max_line_width = width - 40;

            this.current_msg_char_idx = 1;
            this.previous_msg_char_idx = 1;

            //Cannot have a text box without text
            if (msgs.Count <= 0) {
                throw new Exception("Messages can not be empty for a text box!");
            }

            //build msg screens
            speaker_msg_screens = msgs_to_msg_screens2(font, msgs, max_line_width);
            //msg_screens = msgs_to_msg_screens(font, msgs, max_line_width);

            //set the current message
            current_msg_index = 0;
            current_msg_screen_idx = 0;
            current_msg = speaker_msg_screens[current_msg_screen_idx].Item2[current_msg_index].Item1;
            
            this.box_title_name = box_title_name;

            this.random = new Random();
        }

        public virtual void filter_messages_on_tag(string tag_id) {
            //the goal is to reset the speaker_msg_screens based on a filtered list which is built based on tags
            //set up filtered msgs
            List<(string, string, string)> filtered_msgs = new List<(string, string, string)>();
            //iterate over textbox msgs
            for (int i = 0; i < msgs.Count; i++) {
                //pull tag out of msg
                string current_tag = msgs[i].Item3;
                //null compare
                if (tag_id == null && current_tag == null) {
                    filtered_msgs.Add(msgs[i]);
                } else if (tag_id != null && current_tag != null && current_tag.Equals(tag_id)) {
                    //null check + string compare
                    filtered_msgs.Add(msgs[i]);
                }
            }
            //rebuild msg screens based on filtered msgs
            //making sure to reset all needed variables
            reset();
            advance_message_cooldown = 500f;
            speaker_msg_screens = msgs_to_msg_screens2(font, filtered_msgs, max_line_width);
            current_msg = speaker_msg_screens[current_msg_screen_idx].Item2[current_msg_index].Item1;
        }

        public virtual void Update(GameTime gameTime, World world) {
            //check for input so that the player can advance to the next message
            int input;
            if (!GamePad.GetState(PlayerIndex.One).IsConnected) { //keyboard input
                input = key_down(Keys.Space);
            } else { //controller input
                input = button_A_pressed();
            }

            //wait for input and advance messages until messages run out
            if (current_msg_index < msgs.Count) {
                //use input to verify advancing a message
                if (advance_message_elapsed >= advance_message_cooldown && input == 1) {
                    advance_message_elapsed = 0;
                    //pull next message to display
                    current_msg = next_message();
                    //reset current_msg_char_idx
                    current_msg_char_idx = 0;
                    //set cooldown relative to length of msg
                    advance_message_cooldown = current_msg.Length * 25f;
                    if (end_of_text) {
                        return;
                    }
                }
                //add to variable to keep track of cooldowns
                advance_message_elapsed += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
                //calculate the index of the current character in the message to display based on how much time has elapsed going up to the cooldown
                current_msg_char_idx = elapsed_to_index(advance_message_elapsed, advance_message_cooldown, current_msg);
                if (current_msg_char_idx != previous_msg_char_idx) {
                    //play sound because we are on a new character
                    world.play_spatial_sfx(Constant.typewriter_sfx, world.get_player().get_base_position(), ((float)random.Next(-1, 2)), world.get_render_distance(), -0.5f);
                }
                previous_msg_char_idx = current_msg_char_idx;
            }
        }

        //function to calculate the index of what character we should be on in a string given elapsed and threshold
        private int elapsed_to_index(float elapsed, float threshold, string msg) {
            //calculate percentage
            float percentage = MathHelper.Clamp(elapsed / threshold, 0f, 1f);
            //calculate and return index based on percentage of message length
            return (int)(percentage * (msg.Length - 1));
        }

        public string next_message() {
            //increment index
            ++current_msg_index;
            //check and handle out of bounds
            if (current_msg_index >= speaker_msg_screens[current_msg_screen_idx].Item2.Count) {   
                ++current_msg_screen_idx;
                current_msg_index = 0;
            }
            if (current_msg_screen_idx >= speaker_msg_screens.Count) {
                end_of_text = true;
                return "";
            }
            //pull message based on current index
            return speaker_msg_screens[current_msg_screen_idx].Item2[current_msg_index].Item1;
        }

        protected int key_down(Keys key) {
            if (Keyboard.GetState().IsKeyDown(key))
                return 1;
            else
                return 0;
        }

        protected int button_A_pressed() {
            //ternary operator to check if A is pressed
            return GamePad.GetState(PlayerIndex.One).Buttons.A == ButtonState.Pressed ? 1 : 0;
        }

        public bool text_ended() {
            return end_of_text;
        }

        public void reset() {
            end_of_text = false;
            current_msg_index = 0;
            current_msg_screen_idx = 0;
            current_msg_char_idx = 1;
            previous_msg_char_idx = 1;
            advance_message_elapsed = 0;
            current_msg = speaker_msg_screens[current_msg_screen_idx].Item2[current_msg_index].Item1;
        }

        public List<(string, string, string)> get_msgs() {
            return this.msgs;
        }

        //NOTE: going with list of lists here because messages will be formed like thoughts, meaning we can't just concat all the messages together
        //we need to preserve the spacing of the messages so like there should be empty space at the end of a message that runs over onto the next msg screen
        //will flow better that way for story purposes rather than just concatenating all messages together I think
        /*public List<List<string>> msgs_to_msg_screens(SpriteFont sf, List<string> messages, float max_line_width) {
            List<List<string>> msg_screens_list = new List<List<string>>();
            //loop over messages provided and wrap text + produce msg screens to iterate through in sign
            foreach (string message in messages) {
                msg_screens_list.Add(msg_to_msg_screens(sf, message, max_line_width));
            }
            return msg_screens_list;
        }*/

        public List<(string, List<(string, string)>)> msgs_to_msg_screens2(SpriteFont sf, List<(string, string, string)> messages, float max_line_width) {
            List<(string, List<(string, string)>)> msg_screens_list = new List<(string, List<(string, string)>)>();
            foreach (var tuple in messages) {
                string speaker = tuple.Item1;
                string message = tuple.Item2;
                string tag = tuple.Item3;
                msg_screens_list.Add((speaker, msg_to_msg_screens(sf, message, tag, max_line_width)));
            }
            return msg_screens_list;
        }

        public List<(string, string)> msg_to_msg_screens(SpriteFont sf, string message, string tag, float max_line_width) {
            //wrap text for message and translate into msg screens
            //break msgs into words
            string[] words = message.Split(" ");
            //float space_width = sf.MeasureString(" ").X;
            float space_width = Renderer.MeasureCustomString(Constant.pixelfont_char_map, text_scale, " ");
            float current_line_width = 0f;
            StringBuilder sb = new StringBuilder();
            List<(string, string)> msg_screens = new List<(string, string)>();
            bool wrap_once = false;

            //loop over words in message
            foreach (string word in words) {
                //float word_length = sf.MeasureString(word).X;
                float word_length = Renderer.MeasureCustomString(Constant.pixelfont_char_map, text_scale, word);
                //compare current line length with the word and space to the max line width
                if (current_line_width + word_length + space_width <= max_line_width) {
                    current_line_width += (word_length + space_width);
                    sb.Append(word + " ");
                } else {
                    if (wrap_once) {
                        //we have already reached the end of the message box, need to start a new message screen
                        //append current msg to screens
                        msg_screens.Add((sb.ToString(), tag));
                        //empty out string builder
                        sb.Clear();
                        //set current line_length
                        current_line_width = word_length + space_width;
                        sb.Append(word + " ");
                        wrap_once = false;
                        continue;
                    }
                    current_line_width = word_length + space_width;
                    sb.Append("\n" + word + " ");
                    wrap_once = true;
                }
            }
            msg_screens.Add((sb.ToString(), tag));
            return msg_screens;
        }

        public void set_position(Vector2 position) {
            this.position = position;
        }

        public float get_width() {
            return width;
        }

        public float get_height() {
            return height;
        }

        public void set_width(float value) {
            width = value;
            max_line_width = width - 20;
        }

        public void set_height(float value) {
            height = value;
        }

        public void set_font_scale(float value) {
            this.text_scale = value;
            this.line_spacing *= text_scale;
        }

        public float get_original_height() {
            return original_height;
        }

        public float get_original_width() {
            return original_width;
        }

        public void recompute_msg_screens(float new_width, float new_height) {
            //store current message index and character index to maintain progress
            string current_msg_backup = current_msg;
            int char_idx_backup = current_msg_char_idx;

            //update textbox size
            this.width = new_width;
            this.height = new_height;
             // adjust text wrapping width
            this.max_line_width = width - 40;

            //recompute message screens with new dimensions
            speaker_msg_screens = msgs_to_msg_screens2(font, msgs, max_line_width);

            // try to restore the closest matching position in the new text layout
            current_msg_index = 0;
            current_msg_screen_idx = 0;

            //iterate over speaker msg screens
            foreach (var screen in speaker_msg_screens) {
                //iterate over messages
                for (int i = 0; i < screen.Item2.Count; i++) {
                    if (screen.Item2[i].Item1.StartsWith(current_msg_backup.Substring(0, Math.Min(10, current_msg_backup.Length)))) {
                        current_msg_screen_idx = speaker_msg_screens.IndexOf(screen);
                        current_msg_index = i;
                        break;
                    }
                }
            }

            //restore message text and character index
            current_msg = speaker_msg_screens[current_msg_screen_idx].Item2[current_msg_index].Item1;
            current_msg_char_idx = Math.Min(char_idx_backup, current_msg.Length - 1);
        }

        public virtual void Draw(SpriteBatch spriteBatch) {
            //draw rectangle as backdrop for text
            Renderer.FillRectangle(spriteBatch, position, (int)width, (int)height, box_color * background_opacity);
            //start position
            text_draw_position = position + text_offset;
            //track offset
            char_offset = Vector2.Zero;
            //draw current message based on characters (current_msg_char_idx will increase with elapsed time up until the threshold so we can display character by character over time)
            for (int i = 0; i <= current_msg_char_idx; i++) {
                //pull current char from msg
                char current_char = current_msg[i];
                //handle line break
                if (current_char == '\n') {
                    //set offset back to zero for x axis
                    char_offset.X = 0;
                    //set offset to line spacing for y axis
                    char_offset.Y += line_spacing;
                    //skip and don't draw this character obviously
                    continue;
                }
                //calculate size of current character
                Vector2 char_size = new Vector2(Constant.pixelfont_char_size*text_scale, Constant.pixelfont_char_size*text_scale);
                //draw current character at appropriate positioning
                Renderer.DrawCustomString(spriteBatch, Constant.pixel_font1, Constant.pixelfont_char_map, current_char.ToString(), text_draw_position + char_offset, this.text_scale, text_color);
                //increase offset on x axis
                char_offset.X += char_size.X;
            }
            //draw box title
            Renderer.DrawCustomString(spriteBatch, Constant.pixel_font1, Constant.pixelfont_char_map, speaker_msg_screens[current_msg_screen_idx].Item1.ToUpper(), position + box_title_offset, this.text_scale, text_color);
            //draw continue button for all but the last message screen
            if (current_msg_screen_idx < speaker_msg_screens.Count-1) {
                Renderer.DrawCustomString(spriteBatch, Constant.pixel_font1, Constant.pixelfont_char_map, "...", position + new Vector2(width - 40, height - 20), this.text_scale, Color.White);
            }
        }
    }
}