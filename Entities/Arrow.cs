using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using gate.Serialize;
using gate.Interface;
using gate.Core;

namespace gate.Entities
{
    public class Arrow : IEntity
    {
        public Vector2 base_position;
        public Vector2 draw_position;
        public Vector2 depth_sort_position;
        public Vector2 parent_position;
        public Vector2 direction;

        public Texture2D texture;

        private Vector2 rotation_point;
        private float scale;
        private float rotation = 0.0f, rotation_offset = 0f;
        private Vector2 direction_down = new Vector2(0, 1);
        private Vector2 direction_up = new Vector2(0, -1);
        private Random random;
        private Rectangle source_rect = new Rectangle(0, 0, 16, 16);

        private List<Footprints> footprints;
        private List<Footprints> reap_footprints;
        private float footprints_elapsed;
        private float footprints_delay = 100f;
        private Color trail_color = Color.Black;

        private int arrow_size;
        private float angle_offset;
        private Vector2 aim_direction;
        private bool power_shot;

        //particle like variable to determine if stopped
        private bool dead = false;
        
        //attack vars
        private Vector2 movement_direction;
        private float arrow_speed = 10f, speed_multiplier = 1f;
        private bool fired = false;
        private RRect hitbox;

        public static bool debug = false;

        public Arrow(Vector2 base_position, float scale, Texture2D texture, int arrow_size, Vector2 parent_position, Vector2 direction) {
            this.base_position = base_position;
            this.draw_position = new Vector2(base_position.X - (texture.Width / 2), 
                                            base_position.Y - texture.Height);
            this.depth_sort_position = this.draw_position + new Vector2(0, texture.Height / 2);
            this.scale = scale;
            this.rotation_point = new Vector2(texture.Width / 2, texture.Height / 2);

            this.parent_position = parent_position;

            this.texture = texture;
            this.arrow_size = arrow_size;

            //calculate angle offset with direction
            this.angle_offset = Constant.get_angle(base_position, direction);

            this.hitbox = new RRect(this.draw_position, 16, 16);

            //footprints init
            this.footprints = new List<Footprints>();
            this.reap_footprints = new List<Footprints>();

            random = new Random();
        }

        public void Update(GameTime gameTime, float rotation) {
            if (!fired) {
                this.rotation = rotation;
            }
            depth_sort_position = Constant.rotate_point(draw_position, rotation, (texture.Height/2), Constant.direction_down);

            //handle arrow movement once fired
            if (fired) { //arrow has been fired, update movement
                move_arrow(movement_direction);
                add_footprints(gameTime);
                
                //update footprints and reap footprints
                foreach (Footprints f in footprints) {
                    f.Update(gameTime, this.rotation);
                    if (f.reap) {
                        reap_footprints.Add(f);
                    }
                }
                //reap footprints
                foreach (Footprints f in reap_footprints){
                    footprints.Remove(f);
                    Footprints ftprt = f;
                    ftprt = null;

                }
                reap_footprints.Clear();

            } else {
                base_position = parent_position;
                draw_position = base_position;
            }
            
            //update hitbox
            hitbox.update(rotation, draw_position);
        }

        private void add_footprints(GameTime gameTime) {
            footprints_elapsed += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
            //check to add footprints
            if (footprints_elapsed >= footprints_delay) {
                Vector2 footprint_position = Constant.rotate_point(base_position, 16f, -rotation, Constant.direction_up);
                //create new footprint and set elapsed back to 0
                footprints.Add(new Footprints(footprint_position, 1f, Constant.arrow_tex, angle_offset, 0.5f, 0.01f, trail_color, draw_position));
                footprints_elapsed = 0;
            }
        }

        public void update_aim(Vector2 center_position, Vector2 direction) {
            //set the parent position and the direction passed in
            this.parent_position = center_position;
            this.aim_direction = direction;
            //calculate the angle from zero
            //this is because the angle we are passing in needs to be set at the origin
            float direction_angle = Constant.get_angle(Vector2.Zero, direction);
            //rotate the angle by 90 degrees to account for the texture of the arrow being straight up and down (expressed in radians because the angle is already being calculated in radians)
            this.angle_offset = direction_angle + ((float)Math.PI*0.5f);
        }

        public void move_arrow(Vector2 direction) {
            base_position += (direction * arrow_speed) * speed_multiplier;
            draw_position += (direction * arrow_speed) * speed_multiplier;
            depth_sort_position = draw_position + (arrow_size/2) * new Vector2(direction_down.X * (float)Math.Cos(-rotation) - direction_down.Y * (float)Math.Sin(-rotation), direction_down.Y * (float)Math.Cos(-rotation) + direction_down.X * (float)Math.Sin(-rotation));
            //handle arrow slowing down
            if (speed_multiplier <= 0f) {
                speed_multiplier = 0f;
                dead = true;
            } else if (speed_multiplier > 0f) {
                speed_multiplier -= 0.01f;
            }
        }

        public void fire_arrow(Vector2 direction, float speed_multiplier, bool is_power_shot) {
            movement_direction = direction;
            movement_direction.Normalize();
            this.speed_multiplier = speed_multiplier;
            fired = true;
            power_shot = is_power_shot;
            if (power_shot) {
                //do something with trail color
                trail_color = Color.Red;
            }
        }

        public bool hitbox_active() {
            return fired;
        }

        public bool is_fired() {
            return fired;
        }

        public bool is_power_shot() {
            return power_shot;
        }

        public bool is_dead() {
            return dead;
        }

        public RRect get_hitbox() {
            return hitbox;
        }

        public void update_animation(GameTime gameTime) {
            //no animation needed
        }

        public int get_obj_ID_num() {
            return -1;
        }

        public Vector2 get_base_position(){
            return depth_sort_position;
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

        public void set_base_position(Vector2 position) {
            draw_position = position;
        }

        public float get_rotation_offset() {
            return rotation_offset;
        }

        public void set_rotation_offset(float rotation_offset_degrees) {
            float radians_offset = MathHelper.ToRadians(rotation_offset_degrees);
            rotation_offset = radians_offset;
        }

        public string get_id() {
            return "arrow";
        }
        
        //should never really need this (hopefully)
        //this entity is like footsteps, should not be available through the level editor
        public GameWorldObject to_world_level_object() {
            return new GameWorldObject {
                object_identifier = "arrow"
            };
        }

        public void Draw(SpriteBatch spriteBatch) {
            //draw trail connector
            int j = 1;
            for (int i = 0; i < footprints.Count; i++) {
                if (j < footprints.Count - 1) {
                    Renderer.DrawALine(spriteBatch, Constant.pixel, 1, trail_color, footprints[i].get_current_lifecycle() * 0.3f, footprints[i].get_draw_position(), footprints[j].get_draw_position());
                }
                j++;
            }

            //draw footprints
            foreach (Footprints f in footprints) {
                f.Draw(spriteBatch);
            }

            //draw regular arrow
            spriteBatch.Draw(texture, draw_position, source_rect, Color.Black, angle_offset, rotation_point, scale, SpriteEffects.None, 0f);
            //spriteBatch.Draw(Constant.tree2_tex, draw_position, null, Color.White, -rotation, rotation_point, scale, SpriteEffects.None, 0f);
            if (debug){
                hitbox.draw(spriteBatch);
                Renderer.DrawALine(spriteBatch, Constant.pixel, 2, Color.Blue, 1f, base_position, base_position + new Vector2(0, -10));
                Renderer.DrawALine(spriteBatch, Constant.pixel, 2, Color.Green, 1f, draw_position, draw_position + new Vector2(0, -10));
                Renderer.DrawALine(spriteBatch, Constant.pixel, 2, Color.Yellow, 1f, depth_sort_position, depth_sort_position + new Vector2(0, -10));   
                Renderer.DrawALine(spriteBatch, Constant.pixel, 2, Color.Purple, 1f, parent_position, parent_position + new Vector2(0, -5));
            }
        }
    }
}