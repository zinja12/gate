using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using gate.Particles;
using gate.Serialize;
using gate.Interface;
using gate.Core;
using gate.Collision;

namespace gate.Entities
{
    public class Ghost : Nightmare, IEntity, ICollisionEntity, IAiEntity
    {
        //world reference to spawn sludge tiles
        private World world;

        //background particle system reference
        private ParticleSystem fx_particle_system;

        private float sludge_elapsed = 0f, sludge_threshold = 1000f;

        public Ghost(Texture2D texture, Vector2 base_position, float scale, Texture2D hit_texture, Player player, int ID, string identifier, World world, bool? static_image_entity = null)
            : base(texture, base_position, scale, hit_texture, player, ID, identifier, static_image_entity) {               
            //set world reference
            this.world = world;
            //set health value for this enemy specifically
            set_health(2);
            //set movement speed
            movement_speed = 0.7f;
            //set new animations for enemy
            this.idle_animation = new Animation(idle_animation_duration, 2, (int)nightmare_size * 1, 0, (int)nightmare_size, (int)nightmare_size);
            this.idle_animation.set_y_offset((int)nightmare_size*6);
            this.walk_animation = new Animation(walk_animation_duration, 2, (int)nightmare_size * 1, 0, (int)nightmare_size, (int)nightmare_size);

            //add background particle system
            fx_particle_system = new ParticleSystem(true, this.draw_position, 1, 800f, 6, 1, 3, Constant.black_particles, new List<Texture2D> { Constant.footprint_tex });
            particle_systems.Add(fx_particle_system);
        }
        
        public override void update_movement(float rotation) {
            //general update housekeeping
            //update particle system position
            fx_particle_system.set_position(Constant.rotate_point(draw_position, rotation, 1f, Constant.direction_up));
            
            //handle custom movement for this enemy
            //this enemy should constantly move towards the player if within engagement distance
            if (Vector2.Distance(get_base_position(), player.get_base_position()) < engagement_distance) {
                //sludge
                add_sludge(gt, rotation);
                //movement
                engagement_distance = Constant.nightmare_aggro_engagement_distance;
                direction_weights = assign_weights(movement_directions.ToArray(), player, false);
                movement_vector_idx = select_best_weight(direction_weights);
                direction = Vector2.Zero + movement_directions[movement_vector_idx];
                //update last direction
                    last_direction.X = direction.X;
                    last_direction.Y = direction.Y;
                    last_movement_vector_idx = movement_vector_idx;
                    moving = true;

                    //alter the position based on direction and movement speed
                    base_position += direction * movement_speed;
                    draw_position += direction * movement_speed;
                    attack_draw_position = draw_position + (nightmare_size/2) * new Vector2(-1 * (float)Math.Cos(-rotation) - (-1) * (float)Math.Sin(-rotation), -1 * (float)Math.Cos(-rotation) + (-1) * (float)Math.Sin(-rotation));
            } else {
                moving = false;
                engagement_distance = Constant.nightmare_engagement_distance;
            }
        }

        private void add_sludge(GameTime gameTime, float rotation) {
            //slude timer
            sludge_elapsed += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
            if (sludge_elapsed >= sludge_threshold) {
                //reset slude timer
                sludge_elapsed = 0f;
                //place sludge
                world.add_temp_tile(
                    new TempTile(draw_position, 1f, rotation, Constant.sludge_tex, Color.White, "sludge_tile", (int)DrawWeight.Light, world.get_editor_object_idx(), true)
                );
                //good practice increment editor idx
                world.increment_editor_idx();
            }
        }
    }
}