using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using gate.Serialize;
using gate.Interface;
using gate.Core;

namespace gate.Entities
{
    public class PlaceHolderEntity : IEntity
    {
        public Vector2 base_position;
        public float scale;

        public static bool debug = true;
        private int ID;
        private string id_label;

        public PlaceHolderEntity(Vector2 base_position, string id_label, int ID) {
            this.base_position = base_position;
            this.scale = 1f;

            this.ID = ID;
            this.id_label = id_label;
        }

        public void Update(GameTime gameTime, float rotation) {
        }

        public void update_animation(GameTime gameTime) {
            //update animation
        }

        public Vector2 get_base_position(){
            return base_position;
        }

        public float get_scale(){
            return scale;
        }

        public void set_scale(float scale_value) {
            this.scale = scale_value;
        }

        public string get_flag(){
            return Constant.ENTITY_ACTIVE;
        }

        public void set_obj_ID_num(int id_num) {
            this.ID = id_num;
        }

        public int get_obj_ID_num() {
            return this.ID;
        }

        public void set_base_position(Vector2 position) {
            base_position = position;
        }

        public float get_rotation_offset() {
            return 0f;
        }

        public void set_rotation_offset(float rotation_offset_degrees) {
        }

        public GameWorldObject to_world_level_object() {
            return new GameWorldObject {
                object_identifier = $"placeholder_obj:{id_label}",
                object_id_num = get_obj_ID_num(),
                x_position = base_position.X,
                y_position = base_position.Y,
                scale = get_scale(),
                rotation = MathHelper.ToDegrees(get_rotation_offset())
            };
        }

        public string get_id() {
            return $"placeholder_obj:{id_label}";
        }

        public void Draw(SpriteBatch spriteBatch) {
            spriteBatch.DrawString(Constant.arial, $"PlaceHolder({this.ID})-{id_label}", base_position + new Vector2(0, 20), Color.White, 0f, Vector2.Zero, 0.12f, SpriteEffects.None, 0f);
            if (debug) {
            }
        }
    }
}