using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json.Linq;

namespace gate
{
    public class TextBox
    {
        public Vector2 screen_position;

        private Vector2 text_offset = new Vector2(30, 20);
        private Vector2 box_title_offset = new Vector2(0, -20);

        private SpriteFont font;
        private List<string> msgs;
        private List<List<string>> msg_screens;
        private string current_msg;
        private int current_msg_index, current_msg_screen_idx;
        private float width, height;
        private Color color;
        private bool end_of_text = false;

        private string box_title_name;

        private float advance_message_elapsed;
        private float advance_message_cooldown = 500f;

        private float background_opacity = 0.8f;
        private float max_line_width;

        public TextBox(Vector2 screen_position, SpriteFont font, List<string> msgs, string box_title_name, float width, float height, Color color) {
            this.screen_position = screen_position;
            this.font = font;
            this.msgs = msgs;
            this.width = width;
            this.height = height;
            this.color = color;
            this.max_line_width = width - 20;

            //Cannot have a text box without text
            if (msgs.Count <= 0) {
                throw new Exception("Messages can not be zero for a text box!");
            }

            //build msg screens
            msg_screens = msgs_to_msg_screens(font, msgs, max_line_width);

            //set the current message
            current_msg_index = 0;
            current_msg_screen_idx = 0;
            current_msg = msg_screens[current_msg_screen_idx][current_msg_index];
            
            this.box_title_name = box_title_name;
        }

        public void Update(GameTime gameTime) {
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
                    if (end_of_text) {
                        return;
                    }
                }
                //add to variable to keep track of cooldowns
                advance_message_elapsed += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
            }
        }

        public string next_message() {
            //increment index
            ++current_msg_index;
            //check and handle out of bounds
            if (current_msg_index >= msg_screens[current_msg_screen_idx].Count) {   
                ++current_msg_screen_idx;
                current_msg_index = 0;
            }
            if (current_msg_screen_idx >= msg_screens.Count) {
                end_of_text = true;
                return "";
            }
            //pull message based on current index
            return msg_screens[current_msg_screen_idx][current_msg_index];
        }

        private int key_down(Keys key) {
            if (Keyboard.GetState().IsKeyDown(key))
                return 1;
            else
                return 0;
        }

        private int button_A_pressed() {
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
            current_msg = msg_screens[current_msg_screen_idx][current_msg_index];
        }

        //NOTE: going with list of lists here because messages will be formed like thoughts, meaning we can't just concat all the messages together
        //we need to preserve the spacing of the messages so like there should be empty space at the end of a message that runs over onto the next msg screen
        //will flow better that way for story purposes rather than just concatenating all messages together I think
        public List<List<string>> msgs_to_msg_screens(SpriteFont sf, List<string> messages, float max_line_width) {
            List<List<string>> msg_screens_list = new List<List<string>>();
            //loop over messages provided and wrap text + produce msg screens to iterate through in sign
            foreach (string message in messages) {
                msg_screens_list.Add(msg_to_msg_screens(sf, message, max_line_width));
            }
            return msg_screens_list;
        }

        public List<string> msg_to_msg_screens(SpriteFont sf, string message, float max_line_width) {
            //wrap text for message and translate into msg screens
            //break msgs into words
            string[] words = message.Split(" ");
            float space_width = sf.MeasureString(" ").X;
            float current_line_width = 0f;
            StringBuilder sb = new StringBuilder();
            List<string> msg_screens = new List<string>();
            bool wrap_once = false;

            //loop over words in message
            foreach (string word in words) {
                float word_length = sf.MeasureString(word).X;
                //compare current line length with the word and space to the max line width
                if (current_line_width + word_length + space_width <= max_line_width) {
                    current_line_width += (word_length + space_width);
                    sb.Append(word + " ");
                } else {
                    if (wrap_once) {
                        //we have already reached the end of the message box, need to start a new message screen
                        //append current msg to screens
                        msg_screens.Add(sb.ToString());
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
            msg_screens.Add(sb.ToString());
            return msg_screens;
        }

        public void Draw(SpriteBatch spriteBatch) {
            //draw rectangle as backdrop for text
            Renderer.FillRectangle(spriteBatch, screen_position, (int)width, (int)height, Color.Black * background_opacity);
            //draw box title
            if (box_title_name != null) {
                spriteBatch.DrawString(Constant.arial_small, box_title_name, screen_position + box_title_offset, color);
            }
            //draw current message
            spriteBatch.DrawString(font, current_msg, screen_position + text_offset, color);
            //draw continue button for all but the last message screen
            if (current_msg_screen_idx < msg_screens.Count-1) {
                spriteBatch.DrawString(font, ">", screen_position + new Vector2(width - 50, height - 80), Color.White);
            }
        }
    }
}