using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace gate.Core
{
    public class Renderer
    {
        public static float lineAngle;
        public static float lineLength;

        public static void DrawALine(SpriteBatch batch, Texture2D blank,
              float width, Color color, float opacity, Vector2 point1, Vector2 point2)
        {
            float angle = (float)Math.Atan2(point2.Y - point1.Y, point2.X - point1.X);
            float length = Vector2.Distance(point1, point2);
            lineAngle = angle;
            lineLength = length;

            batch.Draw(blank, point1, null, color * opacity,
                       angle, Vector2.Zero, new Vector2(length, width),
                       SpriteEffects.None, 0);
        }

        public static void FillRectangle(SpriteBatch spriteBatch, Vector2 rect_position, int width, int height, Color color) 
        {
            Texture2D rect = new Texture2D(spriteBatch.GraphicsDevice, width, height);

            Color[] color_data = new Color[width * height];
            for (int i = 0; i < color_data.Length; i++)
                color_data[i] = color;
            rect.SetData(color_data);

            Vector2 position = rect_position;
            spriteBatch.Draw(rect, position, Color.White);
        }

        public static void FillRectangle(SpriteBatch spriteBatch, Vector2 rect_position, int width, int height, Color color, float opacity) 
        {
            Texture2D rect = new Texture2D(spriteBatch.GraphicsDevice, width, height);

            Color[] color_data = new Color[width * height];
            for (int i = 0; i < color_data.Length; i++)
                color_data[i] = color;
            rect.SetData(color_data);

            Vector2 position = rect_position;
            spriteBatch.Draw(rect, position, Color.White * opacity);
        }

        public static void FillRectangle(SpriteBatch spriteBatch, Vector2 rect_position, int width, int height, Color color, float opacity, float rotation) 
        {
            Texture2D rect = new Texture2D(spriteBatch.GraphicsDevice, width, height);

            Color[] color_data = new Color[width * height];
            for (int i = 0; i < color_data.Length; i++)
                color_data[i] = color;
            rect.SetData(color_data);

            Vector2 position = rect_position;
            spriteBatch.Draw(rect, position, null, Color.White * opacity, rotation, new Vector2(width/2, height/2), 1f, SpriteEffects.None, 0f);
        }

        public static Texture2D CreateTexture(GraphicsDevice device, int width,int height, Func<int,Color> paint) {
            //initialize a texture
            Texture2D texture = new Texture2D(device, width, height);

            //the array holds the color for each pixel in the texture
            Color[] data = new Color[width * height];
            for(int pixel=0;pixel<data.Count();pixel++)
            {
                //the function applies the color according to the specified pixel
                data[pixel] = paint(pixel);
            }

            //set the color
            texture.SetData(data);

            return texture;
        }

        public static void DrawCustomString(SpriteBatch spriteBatch, Texture2D font_texture, Dictionary<string, Rectangle> char_map, String text, Vector2 text_position, float scale, Color color) {
            //set up current cursor
            Vector2 cursor = text_position;
            
            //loop over text
            for (int i = 0; i < text.Length; i++) {
                char current = text[i];
                char next = (i < text.Length - 1) ? text[i+1] : '\0';
                
                //try to pull the value
                int spacing = 0;
                Rectangle source_rect = new Rectangle(0, 0, 0, 0);
                if (char_map.ContainsKey(current.ToString())) {
                    source_rect = char_map[current.ToString()];
                    spacing = source_rect.Width;
                }

                if (current == 'e') {
                    spacing -= 1;
                } else if (current == 'm' || current == 'M' || current == 'W' || current == 'w') {
                    spacing += 1;
                }
                
                //apply kerning
                int kerning = 0;
                if (next != '\0' && Constant.kerning_pairs.ContainsKey((current, next))) {
                    kerning = Constant.kerning_pairs[(current, next)];
                }
                //draw character
                //spriteBatch.Draw(font_texture, cursor, source_rect, color);
                spriteBatch.Draw(font_texture, cursor, source_rect, color, 0f /*rotation*/, Vector2.Zero /*rotation origin*/, scale /*scale*/, SpriteEffects.None, 0f);
                //advance cursor position (letter_size*scale + kerning)
                cursor.X += (spacing*scale) + kerning;
            }
        }

        public static float MeasureCustomString(Dictionary<string, Rectangle> char_map, float scale, String text) {
            float width = 0;
            for (int i = 0; i < text.Length; i++) {
                char current = text[i];
                width += char_map.ContainsKey(current.ToString()) ? (float)char_map[current.ToString()].Width*scale: 0f;
            }

            return width;
        }
        
        //NOTE: Monogame has a built in way to draw triangles via primitives on the gpu, but they're difficult to work with
        //plus I cannot for the life of me figure out how to draw the primitive in the view of the camera
        //I can draw it to the screen but only in screen space, not world space
        //this solution basically just connects three vectors as a triangle
        //afterwards it calculates a set number of points between two triangle vertexes and fills in a 2 pixel wide line between
        //the remaining outlying vertex and each of the calculated points
        //this basically only works because it is a pixel art game with crunchy pixels
        public static void DrawTri(SpriteBatch spriteBatch, Vector2 tri1, Vector2 tri2, Vector2 tri3, Color color, float opacity) {
            //draw edges
            DrawALine(spriteBatch, Constant.pixel, 2f, color * opacity, 1f, tri1, tri2);
            DrawALine(spriteBatch, Constant.pixel, 2f, color * opacity, 1f, tri2, tri3);
            DrawALine(spriteBatch, Constant.pixel, 2f, color * opacity, 1f, tri3, tri1);

            //fill
            List<Vector2> points = new List<Vector2>();
            //calculate an estimate of the points needed to fill the triangle
            int n = (int)Vector2.Distance(tri2, tri3) / 4;
            //calculate the vector between vertexes 2 and 3
            Vector2 step = (tri3 - tri2) / (n - 1);
            //calculate each point along the vector from vertex 2 to 3
            for (int i = 0; i < n; i++) {
                Vector2 new_point = tri2 + step * i;
                points.Add(new_point);
            }
            
            //for calculated points draw line between first vertex and calculated point
            foreach (Vector2 p in points) {
                DrawALine(spriteBatch, Constant.pixel, 10f, color * opacity, 1f, tri1, p);
            }
        }
    }
}