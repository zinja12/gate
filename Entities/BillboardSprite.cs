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
using gate.Collision;
using gate.Particles;

namespace gate.Entities
{
    public class BillboardSprite : IEntity, ICollisionEntity
    {
        public Vector2 base_position;
        public Vector2 draw_position;
        public Vector2 depth_sort_position;
        public Vector2 interaction_display_position;

        protected Vector2 rotation_point;
        protected float scale;
        protected float rotation = 0.0f, rotation_offset = 0f;
        protected int rect_size;

        protected RRect interaction_box, hitbox;
        private bool display_text = false, display_interaction = false;
        
        protected ParticleSystem ps = null;

        protected Texture2D texture;

        public static bool debug = false;

        private int ID;
        private string identifier;

        public BillboardSprite(Texture2D texture, Vector2 base_position, float scale, string identifier, int ID, bool background_particle_system = false) {
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
            this.hitbox = new RRect(this.draw_position, texture.Width, texture.Height);

            this.ID = ID;
            this.identifier = identifier;
            
            if (background_particle_system) {
                ps = new ParticleSystem(true, this.draw_position, 1, 800f, 10, 1, 3, Constant.black_particles, new List<Texture2D> { Constant.footprint_tex });
            }
        }

        public virtual void Update(GameTime gameTime, float rotation) {
            this.rotation = rotation;
            depth_sort_position = Constant.rotate_point(draw_position, rotation, (texture.Height/2), Constant.direction_down);
            this.interaction_display_position = Constant.rotate_point(draw_position, rotation, (-Constant.read_interact_tex.Height), Constant.direction_down);

            //update interaction box
            interaction_box.update(rotation, draw_position);
            hitbox.update(rotation, draw_position);

            if (ps != null) {
                ps.Update(gameTime, rotation);
                ps.set_position(Constant.rotate_point(draw_position, rotation, 1f, Constant.direction_up));
            }
        }

        public void update_animation(GameTime gameTime) {}

        public void take_hit(IEntity entity, int damage) {}

        public virtual void custom_behavior() {}

        public RRect get_hurtbox() {
            return hitbox;
        }

        public bool is_hurtbox_active() {
            return true;
        }

        public string get_flag() {
            return Constant.ENTITY_ACTIVE;
        }

        public RRect get_interaction_box() {
            return interaction_box;
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
            return identifier;
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
            Vector2 overhead_position = Constant.rotate_point(interaction_display_position, rotation, (-Constant.textbox_height/2), Constant.direction_down);
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
                object_identifier = identifier,
                object_id_num = get_obj_ID_num(),
                x_position = base_position.X,
                y_position = base_position.Y,
                scale = get_scale(),
                rotation = get_rotation_offset()
            };
        }

        public float get_scale() {
            return scale;
        }

        public void set_scale(float scale_value) {
            this.scale = scale_value;
        }

        public virtual void Draw(SpriteBatch spriteBatch) {
            //draw shadow
            spriteBatch.Draw(Constant.shadow_tex, draw_position, null, Color.Black * 0.5f, -rotation, rotation_point, scale, SpriteEffects.None, 0f);
            //particles if present
            if (ps != null) {
                ps.Draw(spriteBatch);
            }
            //draw texture
            spriteBatch.Draw(texture, draw_position, null, Color.White, -rotation + rotation_offset, rotation_point, scale, SpriteEffects.None, 0f);
            //display interaction
            if (display_interaction && !display_text) {
                spriteBatch.Draw(Constant.read_interact_tex, interaction_display_position, null, Color.White, -rotation + rotation_offset, rotation_point, scale, SpriteEffects.None, 0f);
            }
            if (debug) {
                interaction_box.draw(spriteBatch);
                hitbox.draw(spriteBatch);
            }
        }
    }
}