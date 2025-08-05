using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using gate.Serialize;
using gate.Interface;
using gate.Core;
using gate.Collision;

namespace gate.Entities
{
    //floor tile draw weights
    public enum DrawWeight {
        Light = 1,
        LightMedium = 3,
        Medium = 5,
        MediumHeavy = 8,
        Heavy = 10
    }

    public interface BackgroundEntity {
        Vector2 get_base_position();
        void Draw(SpriteBatch spriteBatch);
    }

    public interface FloorEntity {
        //draw weight indicates the order in which the background entities will be drawn (higher draw weight means that they should be drawn towards the back, lighter means they should be drawn towards the front)
        int get_draw_weight();
        Vector2 get_base_position();
        void Draw(SpriteBatch spriteBatch);
    }

    public class Tile : BackgroundEntity, FloorEntity, IEntity
    {
        public Vector2 draw_position;

        public Texture2D texture;
        public float scale, width, height;

        public string identifier;

        public float rotation = 0f;
        public int draw_weight;

        private int ID;

        public Tile(Vector2 draw_position, float scale, Texture2D texture, string identifier, int draw_weight, int ID) {
            this.draw_position = draw_position;
            this.texture = texture;

            this.scale = scale;

            this.width = texture.Width;
            this.height = texture.Height;

            this.identifier = identifier;

            this.ID = ID;
            this.draw_weight = draw_weight;
        }

        public int get_draw_weight() {
            return draw_weight;
        }

        public virtual void Draw(SpriteBatch spriteBatch) {
            spriteBatch.Draw(texture, draw_position, null, Color.White, rotation/1000, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        public Vector2 get_base_position() { return draw_position; }

        public float get_depth(float rotation) { return -1f; }

        public float get_scale() { return scale; }

        public void set_scale(float scale_value) {
            this.scale = scale_value;
        }

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

        public virtual void Update(GameTime gameTime) {}

        public void update_animation(GameTime gameTime) {}
    }

    public class TempTile : Tile, BackgroundEntity, FloorEntity, ICollisionEntity, IEntity
    {
        private Vector2 center;
        //hurtbox
        private RRect hurtbox;
        private float opacity = 1f;
        private bool fade;

        private bool debug = false;

        private Color color;

        public TempTile(Vector2 draw_position, float scale, float rotation, Texture2D texture, Color color, string identifier, int draw_weight, int ID, bool fade = false)
            : base(draw_position, scale, texture, identifier, draw_weight, ID) {
            //set color
            this.color = color;
            //set rotation offset
            set_rotation_offset(rotation);
            //set up hurtbox
            this.center = this.draw_position + new Vector2(width/2, height/2);
            this.hurtbox = new RRect(center, width, height);
            this.fade = fade;
        }

        public void take_hit(IEntity entity, int damage) {}

        public RRect get_hurtbox() {
            return hurtbox;
        }

        public bool is_hurtbox_active() {
            if (fade) {
                return opacity > 0f;
            }
            return true;
        }

        public bool is_finished() {
            return opacity <= 0f;
        }

        public float get_opacity() {
            return opacity;
        }

        public override void Update(GameTime gameTime) {
            //only update if not finished
            if (!is_finished()) {
                //update hurtbox
                hurtbox.update(0f, center);

                //handle fading
                if (fade) {
                    //decrease opacity
                    opacity -= 0.0025f;
                }
            }
        }

        public override void Draw(SpriteBatch spriteBatch) {
            spriteBatch.Draw(texture, draw_position, null, color * opacity, rotation/1000, Vector2.Zero, scale, SpriteEffects.None, 0f);
            
            //debug draw
            if (debug) {
                hurtbox.draw(spriteBatch);
            }
        }
    }
}