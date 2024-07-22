using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace gate
{
    public interface ForegroundEntity {
        void Update(GameTime gameTime, float rotation);
        void Draw(SpriteBatch spriteBatch);
    }
    
    public class Tree : IEntity, ForegroundEntity
    {
        public Vector2 base_position;
        public Vector2 draw_position;
        public Vector2 depth_sort_position;

        private bool canopy;

        private Vector2 rotation_point;
        private float scale;
        private float rotation = 0.0f, rotation_offset = 0f;
        private Vector2 direction_down = new Vector2(0, 1);

        public static bool debug = false;

        private Texture2D texture;

        private float tree_width = 64;
        private float tree_height = 64;

        private int stack_count = 26;
        private List<Rectangle> sprite_rectangles;
        private Vector2 rotation_factor;

        private int ID;
        private string tree_identifier;

        public Tree(Vector2 base_position, float scale, Texture2D texture, bool canopy, string tree_identifier, int ID) {
            this.base_position = base_position;
            this.draw_position = new Vector2(base_position.X - (tree_width / 2), 
                                            base_position.Y - tree_height);
            this.depth_sort_position = this.draw_position + new Vector2(0, tree_height / 2);
            this.scale = scale;
            this.rotation_point = new Vector2(tree_width / 2, tree_height / 2);

            this.canopy = canopy;

            this.texture = texture;
            
            sprite_rectangles = Constant.generate_rectangles_for_stack(texture, stack_count);
            this.rotation_factor = this.draw_position;

            this.ID = ID;
            this.tree_identifier = tree_identifier;
        }

        public void Update(GameTime gameTime, float rotation) {
            this.rotation = -rotation;
            depth_sort_position = draw_position + (tree_height/2) * new Vector2(direction_down.X * (float)Math.Cos(-rotation) - direction_down.Y * (float)Math.Sin(-rotation), direction_down.Y * (float)Math.Cos(-rotation) + direction_down.X * (float)Math.Sin(-rotation));
        }

        public void update_animation(GameTime gameTime){
            //update animation
        }

        public Vector2 get_base_position(){
            return draw_position;
        }

        public float get_scale(){
            return scale;
        }

        public void set_scale(float scale_value) {
            this.scale = scale_value;
        }

        public string get_flag(){
            return Constant.ENTITY_PASSIVE;
        }

        public void set_obj_ID_num(int id_num) {
            this.ID = id_num;
        }

        public int get_obj_ID_num() {
            return this.ID;
        }

        public void set_base_position(Vector2 position) {
            base_position = position;
            this.draw_position = new Vector2(base_position.X - (tree_width / 2), 
                                            base_position.Y - tree_height);
        }

        public float get_rotation_offset() {
            return rotation_offset;
        }

        public string get_id() {
            return tree_identifier;
        }

        public void set_rotation_offset(float rotation_offset_degrees) {
            float radians_offset = MathHelper.ToRadians(rotation_offset_degrees);
            rotation_offset = radians_offset;
        }

        public GameWorldObject to_world_level_object() {
            if (canopy) {
                return new GameWorldObject {
                    object_identifier = "tree",
                    object_id_num = get_obj_ID_num(),
                    x_position = base_position.X,
                    y_position = base_position.Y,
                    scale = get_scale(),
                    canopy = true,
                    rotation = MathHelper.ToDegrees(get_rotation_offset())
                };
            }

            return new GameWorldObject {
                object_identifier = tree_identifier,
                object_id_num = get_obj_ID_num(),
                x_position = base_position.X,
                y_position = base_position.Y,
                scale = get_scale(),
                rotation = MathHelper.ToDegrees(get_rotation_offset())
            };
        }

        public void Draw(SpriteBatch spriteBatch) {
            //update draw positions so that everything draws in the right direction
            if (canopy) { //draw canopy
                for (int i = sprite_rectangles.Count - 1; i < sprite_rectangles.Count; i++) {
                    Vector2 dir = new Vector2(0, -i*Constant.stack_distance);
                    Vector2 draw_pos = Constant.rotate_point(draw_position, -rotation, 1f, dir);
                    spriteBatch.Draw(texture, draw_pos, sprite_rectangles[i], Color.White, (-rotation + rotation_offset)/1000, rotation_point, scale, SpriteEffects.None, 0f);
                }
            } else { //draw regular tree
                //draw sprite stack for tree
                for (int i = 0; i < sprite_rectangles.Count; i++) {
                    Vector2 dir = new Vector2(0, -i*Constant.stack_distance);
                    Vector2 draw_pos = draw_position + new Vector2(dir.X*(float)Math.Cos(rotation) - dir.Y*(float)Math.Sin(rotation), dir.Y*(float)Math.Cos(rotation) + dir.X*(float)Math.Sin(rotation));
                    float rotation_calc = (-rotation + rotation_offset)/1000;
                    spriteBatch.Draw(texture, draw_pos, sprite_rectangles[i], Color.White, rotation_calc, rotation_point, scale, SpriteEffects.None, 0f);
                }
            }
            if (debug) {
                //Renderer.DrawALine(spriteBatch, Constant.pixel, 2, Color.Red, base_position, base_position + new Vector2(0, -10));
                Renderer.FillRectangle(spriteBatch, base_position, 5, 5, Color.Red);
                //Renderer.DrawALine(spriteBatch, Constant.pixel, 2, Color.Green, draw_position, draw_position + new Vector2(0, -10));
                Renderer.FillRectangle(spriteBatch, draw_position, 7, 7, Color.Blue);
                //Renderer.DrawALine(spriteBatch, Constant.pixel, 2, Color.Yellow, depth_sort_position, depth_sort_position + new Vector2(0, -10));
                Renderer.FillRectangle(spriteBatch, depth_sort_position, 9, 9, Color.Yellow);
                //Renderer.DrawALine(spriteBatch, Constant.pixel, 2, Color.Pink, draw_position + rotation_point, draw_position + rotation_point + new Vector2(0, -10));
                Renderer.FillRectangle(spriteBatch, rotation_point, 11, 11, Color.Pink);
            }
        
            //spriteBatch.DrawString(Constant.arial, "depth: " + depth, draw_position, Color.White);
        }
    }
}