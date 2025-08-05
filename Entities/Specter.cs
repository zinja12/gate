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
    public class Specter : Nightmare, IEntity, ICollisionEntity, IAiEntity
    {
        private ParticleSystem fx_particle_system;

        Rectangle single_sprite_source_rect;
        
        public Specter(Texture2D texture, Vector2 base_position, float scale, Texture2D hit_texture, Player player, Dictionary<(int, int), List<IEntity>> chunked_collision_geometry, int ID, string identifier, World world, bool? static_image_entity = null)
            : base(texture, null, base_position, scale, hit_texture, player, chunked_collision_geometry, ID, identifier, static_image_entity) {
                
            //health
            set_health(3);
            //speed
            movement_speed = 0.7f;
            
            //background particle system
            fx_particle_system = new ParticleSystem(true, this.draw_position, 1, 600f, 10, 1, 3, Constant.white_particles, new List<Texture2D> { Constant.red_black_particle_tex });
            particle_systems.Add(fx_particle_system);

            single_sprite_source_rect = new Rectangle(0, 0, (int)nightmare_size, (int)nightmare_size);
        }

        public override void update_movement(float rotation) {
            //general update housekeeping
            //update particle system position
            fx_particle_system.set_position(Constant.rotate_point(draw_position, rotation, 1f, Constant.direction_up));
            
            //handle custom movement for this enemy
            //this enemy should constantly move towards the player if within engagement distance
            if (Vector2.Distance(get_base_position(), player.get_base_position()) < engagement_distance) {
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

        public override void update_animation(GameTime gameTime) {
            //calculate angle
            angle = MathHelper.ToDegrees((float)Math.Atan2(direction.Y, direction.X));
            //update animation
            if (moving) {
                int calculated_animation_case = Constant.TranslateAngleToCompassDirection(angle, MathHelper.ToDegrees(0f));
                if (calculated_animation_case != animation_case && animation_movement_swap_elapsed >= animation_movement_swap_threshold) {
                    animation_case = calculated_animation_case;
                    animation_movement_swap_elapsed = 0f;
                }
                switch (animation_case) {
                    case 0: //up - north
                        single_sprite_source_rect.Y = (int)nightmare_size*0;
                        break;
                    case 1: //up right - ne
                        single_sprite_source_rect.Y = (int)nightmare_size*4;
                        break;
                    case 2: //right - east
                        single_sprite_source_rect.Y = (int)nightmare_size*1;
                        break;
                    case 3: //down right - se
                        single_sprite_source_rect.Y = (int)nightmare_size*5;
                        break;
                    case 4: //down - south
                        single_sprite_source_rect.Y = (int)nightmare_size*0;
                        break;
                    case 5: //down left - sw
                        single_sprite_source_rect.Y = (int)nightmare_size*6;
                        break;
                    case 6: //left - west
                        single_sprite_source_rect.Y = (int)nightmare_size*2;
                        break;
                    case 7: //up left - nw
                        single_sprite_source_rect.Y = (int)nightmare_size*3;
                        break;
                    default:
                        Console.WriteLine("Nightmare AI: Default movement vector case.");
                        break;
                }
            } else {
                //update idle animation based on last direction
                switch (last_movement_vector_idx) {
                    case 0: //up
                        single_sprite_source_rect.Y = (int)nightmare_size*0;
                        break;
                    case 1: //up right
                        single_sprite_source_rect.Y = (int)nightmare_size*4;
                        break;
                    case 2: //right
                        single_sprite_source_rect.Y = (int)nightmare_size*1;
                        break;
                    case 3: //down right
                        single_sprite_source_rect.Y = (int)nightmare_size*5;
                        break;
                    case 4: //down
                        single_sprite_source_rect.Y = (int)nightmare_size*0;
                        break;
                    case 5: //down left
                        single_sprite_source_rect.Y = (int)nightmare_size*6;
                        break;
                    case 6: //left
                        single_sprite_source_rect.Y = (int)nightmare_size*2;
                        break;
                    case 7: //up left
                        single_sprite_source_rect.Y = (int)nightmare_size*3;
                        break;
                    default:
                        Console.WriteLine("Nightmare AI: Default movement vector case.");
                        break;
                }
            }
            //increase animation timer
            animation_movement_swap_elapsed += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
        }

        public override void Draw(SpriteBatch spriteBatch) {
            //draw shadow always
            spriteBatch.Draw(Constant.shadow_tex, draw_position, null, Color.Black * 0.5f, -rotation, rotation_point, scale, SpriteEffects.None, 0f);

            //draw background red circle behind particle system
            if (get_health() > 0) {
                spriteBatch.Draw(Constant.red_circle, draw_position, null, Color.White, -rotation, rotation_point, scale, SpriteEffects.None, 0f);
            }

            /*PARTICLE SYSTEMS*/
            foreach (ParticleSystem ps in particle_systems) {
                ps.Draw(spriteBatch);
            }

            if (get_health() > 0) {
                if (moving) {
                    spriteBatch.Draw(texture, draw_position, single_sprite_source_rect, draw_color, -rotation + rotation_offset, rotation_point, scale, SpriteEffects.None, 0f);
                } else {
                    //idle
                    spriteBatch.Draw(texture, draw_position, single_sprite_source_rect, draw_color, -rotation + rotation_offset, rotation_point, scale, SpriteEffects.None, 0f);
                }
            }
        }
    }
}