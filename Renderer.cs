using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace gate
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
    }
}