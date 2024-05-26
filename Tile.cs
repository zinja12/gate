using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace gate
{
    public interface BackgroundEntity {
        void Draw(SpriteBatch spriteBatch);
    }

    public class Tile : BackgroundEntity, IEntity
    {
        public Vector2 draw_position;

        public Texture2D texture;
        public float scale, width, height;

        public string identifier;

        public float rotation = 0f;

        private int ID;

        public Tile(Vector2 draw_position, float scale, Texture2D texture, string identifier, int ID) {
            this.draw_position = draw_position;
            this.texture = texture;

            this.scale = scale;

            this.width = texture.Width;
            this.height = texture.Height;

            this.identifier = identifier;

            this.ID = ID;
        }

        public void Draw(SpriteBatch spriteBatch) {
            spriteBatch.Draw(texture, draw_position, null, Color.White, rotation, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        public Vector2 get_base_position() { return draw_position; }

        public float get_depth(float rotation) { return -1f; }

        public float get_scale() { return scale; }

        public string get_flag() { return Constant.ENTITY_PASSIVE; }

        public void set_base_position(Vector2 position) {
            draw_position = position;
        }

        public float get_rotation_offset() {
            return rotation;
        }

        public void set_rotation_offset(float rotation_offset_degrees) {
            float radians_offset = MathHelper.ToRadians(rotation_offset_degrees);
            rotation = radians_offset;
        }

        public string get_id() {
            return identifier;
        }

        public void set_obj_ID_num(int id_num) {
            this.ID = id_num;
        }

        public int get_obj_ID_num() {
            return this.ID;
        }

        public GameWorldObject to_world_level_object() {
            return new GameWorldObject {
                object_identifier = identifier,
                object_id_num = get_obj_ID_num(),
                //it is fine to get the base position here as it is just the draw position for tiles
                x_position = get_base_position().X,
                y_position = get_base_position().Y,
                scale = get_scale(),
                rotation = get_rotation_offset()
            };
        }

        public void Update(GameTime gameTime, float rotation) {}

        public void update_animation(GameTime gameTime) {}
    }
}