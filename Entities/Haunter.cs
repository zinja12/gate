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
    public class Haunter : Nightmare, IEntity, ICollisionEntity, IAiEntity
    {
        //world reference to spawn projectiles
        private World world;
        //background particle system reference
        private ParticleSystem fx_particle_system;

        private Animation charge_animation, fire_projectile_animation;
        private Animation diamond_expand_animation;

        private bool charging_projectile, fire_projectile;
        private float charging_projectile_elapsed, fire_projectile_elapsed;
        private float projectile_speed = 0.45f;

        public Haunter(Texture2D texture, Vector2 base_position, float scale, Texture2D hit_texture, Player player, Dictionary<(int, int), List<IEntity>> chunked_collision_geometry, int ID, string identifier, World world, bool? static_image_entity = null)
            : base(texture, null, base_position, scale, hit_texture, player, chunked_collision_geometry, ID, identifier, static_image_entity) {
            //set world reference
            this.world = world;
            //set health
            set_health(3);
            //set movement speed
            movement_speed = 0.5f;
            //set new animations
            this.idle_animation_duration = 300f;
            this.idle_animation = new Animation(idle_animation_duration, 4, (int)nightmare_size*1, 0, (int)nightmare_size, (int)nightmare_size);
            this.idle_animation.set_y_offset((int)nightmare_size*6);
            this.walk_animation = new Animation(walk_animation_duration, 4, (int)nightmare_size*1, 0, (int)nightmare_size, (int)nightmare_size);
            this.charge_animation = new Animation(idle_animation_duration, 4, (int)nightmare_size*1, 0, (int)nightmare_size, (int)nightmare_size);
            this.diamond_expand_animation = new Animation(170f, 4, (int)nightmare_size*1, 0, (int)nightmare_size, (int)nightmare_size);
            this.diamond_expand_animation.set_y_offset(0);
            this.fire_projectile_animation = new Animation(idle_animation_duration, 4, (int)nightmare_size*1, 0, (int)nightmare_size, (int)nightmare_size);
            //add background particle system
            fx_particle_system = new ParticleSystem(true, this.draw_position, 1, 800f, 6, 1, 3, Constant.black_particles, new List<Texture2D> { Constant.footprint_tex });
            particle_systems.Add(fx_particle_system);

            //variable config
            time_between_attacks = 4250;
            striking_distance = 160f;
            charging_projectile = false;
            charging_projectile_elapsed = 0f;
            fire_projectile = false;
            fire_projectile_elapsed = 0f;
        }

        public override void update_movement(float rotation) {
            //general update housekeeping
            //update particle system position
            fx_particle_system.set_position(Constant.rotate_point(draw_position, rotation, 1f, Constant.direction_up));

            if (circling && !charging_projectile && !fire_projectile) {
                //start attack timer
                attack_timer_elapsed += (float)gt.ElapsedGameTime.TotalMilliseconds;

                //check for if we have passed the time between attacks
                if (attack_timer_elapsed >= time_between_attacks) {
                    //reset variables
                    attack_timer_elapsed = 0;
                    circling = false;
                    moving = false;
                    //trigger attack charging
                    charging_projectile = true;
                    diamond_expand_animation.reset_animation();
                    this.diamond_expand_animation.set_y_offset(0);
                    fire_projectile_animation.reset_animation();
                    charge_animation.reset_animation();
                }
            }

            if (!charging_projectile && !fire_projectile) {
                if (Vector2.Distance(get_base_position(), player.get_base_position()) < engagement_distance) {
                    engagement_distance = Constant.nightmare_aggro_engagement_distance;
                    direction_weights = assign_weights(movement_directions.ToArray(), player, false);
                    movement_vector_idx = select_best_weight(direction_weights);
                    direction = Vector2.Zero + movement_directions[movement_vector_idx];
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
            } else if (charging_projectile) {
                circling = false;
                moving = false;
                direction = player.get_base_position() - get_base_position();
                direction.Normalize();
                charging_projectile_elapsed += (float)gt.ElapsedGameTime.TotalMilliseconds;
                //finish charging sequence
                if (charging_projectile_elapsed >= idle_animation_duration) {
                    charging_projectile = false;
                    charging_projectile_elapsed = 0f;
                    attack_timer_elapsed = 0f;
                    fire_projectile = true;
                }
            } else if (fire_projectile) {
                circling = false;
                moving = false;
                charging_projectile = false;
                direction = player.get_base_position() - get_base_position();
                direction.Normalize();
                fire_projectile_elapsed += (float)gt.ElapsedGameTime.TotalMilliseconds;
                if (fire_projectile_elapsed >= idle_animation_duration) {
                    //finish charging sequence and fire the projectile
                    //actually fire the projectile after some delay
                    Arrow arrow = new Arrow(get_base_position() + direction*32f, 1f, Constant.hex1_tex, 16, draw_position, direction);
                    arrow.set_trail_color(Color.Black);
                    arrow.set_tex_color(Color.White);
                    //fire arrow
                    arrow.fire_arrow(direction, projectile_speed, false);
                    //add to world
                    world.add_projectile(arrow);

                    //finish firing
                    charging_projectile = false;
                    fire_projectile = false;
                    fire_projectile_elapsed = 0f;
                    circling = true;
                }
            }
        }

        public override void update_animation(GameTime gameTime) {
            //calculate angle
            angle = MathHelper.ToDegrees((float)Math.Atan2(direction.Y, direction.X));
            //update walk animation if moving
            if (moving) {
                //update animation for movement
                int calculated_animation_case = Constant.TranslateAngleToCompassDirection(angle, MathHelper.ToDegrees(rotation));
                //handle animation sticking so that animations don't rubber-band as quickly leading to jank
                if (calculated_animation_case != animation_case && animation_movement_swap_elapsed >= animation_movement_swap_threshold) {
                    animation_case = calculated_animation_case;
                    animation_movement_swap_elapsed = 0f;
                }
                switch (animation_case) {
                    case 0: //up - north
                        walk_animation.set_y_offset((int)nightmare_size*2);
                        break;
                    case 1: //up right - ne
                        walk_animation.set_y_offset((int)nightmare_size*6);
                        break;
                    case 2: //right - east
                        walk_animation.set_y_offset((int)nightmare_size*1);
                        break;
                    case 3: //down right - se
                        walk_animation.set_y_offset((int)nightmare_size*4);
                        break;
                    case 4: //down - south
                        walk_animation.set_y_offset((int)nightmare_size*3);
                        break;
                    case 5: //down left - sw
                        walk_animation.set_y_offset((int)nightmare_size*5);
                        break;
                    case 6: //left - west
                        walk_animation.set_y_offset((int)nightmare_size*0);
                        break;
                    case 7: //up left - nw
                        walk_animation.set_y_offset((int)nightmare_size*7);
                        break;
                    default:
                        Console.WriteLine("Nightmare AI: Default movement vector case.");
                        break;
                }
                // keep walk animation updated when moving
                walk_animation.Update(gameTime);
            } else if (charging_projectile) {
                switch (Constant.TranslateAngleToCompassDirection(angle, MathHelper.ToDegrees(rotation))) {
                    case 0: // up - north
                        charge_animation.set_y_offset((int)nightmare_size*2);
                        break;
                    case 1: //up right - ne
                        charge_animation.set_y_offset((int)nightmare_size*6);
                        break;
                    case 2: //right - east
                        charge_animation.set_y_offset((int)nightmare_size*1);
                        break;
                    case 3: //down right - se
                        charge_animation.set_y_offset((int)nightmare_size*4);
                        break;
                    case 4: //down - south
                        charge_animation.set_y_offset((int)nightmare_size*3);
                        break;
                    case 5: //down left - sw
                        charge_animation.set_y_offset((int)nightmare_size*5);
                        break;
                    case 6: //left - west
                        charge_animation.set_y_offset((int)nightmare_size*0);
                        break;
                    case 7: //up left - nw
                        charge_animation.set_y_offset((int)nightmare_size*7);
                        break;
                    default:
                        Console.WriteLine("Nightmare AI: Default animation case");
                        break;
                }
                charge_animation.Update(gameTime);
            } else if (fire_projectile) {
                switch (Constant.TranslateAngleToCompassDirection(angle, MathHelper.ToDegrees(rotation))) {
                    case 0: // up - north
                        fire_projectile_animation.set_y_offset((int)nightmare_size*2);
                        break;
                    case 1: //up right - ne
                        fire_projectile_animation.set_y_offset((int)nightmare_size*6);
                        break;
                    case 2: //right - east
                        fire_projectile_animation.set_y_offset((int)nightmare_size*1);
                        break;
                    case 3: //down right - se
                        fire_projectile_animation.set_y_offset((int)nightmare_size*4);
                        break;
                    case 4: //down - south
                        fire_projectile_animation.set_y_offset((int)nightmare_size*3);
                        break;
                    case 5: //down left - sw
                        fire_projectile_animation.set_y_offset((int)nightmare_size*5);
                        break;
                    case 6: //left - west
                        fire_projectile_animation.set_y_offset((int)nightmare_size*0);
                        break;
                    case 7: //up left - nw
                        fire_projectile_animation.set_y_offset((int)nightmare_size*7);
                        break;
                    default:
                        Console.WriteLine("Nightmare AI: Default animation case");
                        break;
                }
                fire_projectile_animation.Update(gameTime);
                diamond_expand_animation.Update(gameTime);
            } else {
                //update idle animation based on last direction
                switch (last_movement_vector_idx) {
                    case 0: //up
                        idle_animation.set_y_offset((int)nightmare_size*2);
                        break;
                    case 1: //up right
                        idle_animation.set_y_offset((int)nightmare_size*6);
                        break;
                    case 2: //right
                        idle_animation.set_y_offset((int)nightmare_size*2);
                        break;
                    case 3: //down right
                        idle_animation.set_y_offset((int)nightmare_size*4);
                        break;
                    case 4: //down
                        idle_animation.set_y_offset((int)nightmare_size*3);
                        break;
                    case 5: //down left
                        idle_animation.set_y_offset((int)nightmare_size*5);
                        break;
                    case 6: //left
                        idle_animation.set_y_offset((int)nightmare_size*0);
                        break;
                    case 7: //up left
                        idle_animation.set_y_offset((int)nightmare_size*7);
                        break;
                    default:
                        Console.WriteLine("Nightmare AI: Default movement vector case.");
                        break;
                }
                //keep idle animation updated when not moving
                idle_animation.Update(gameTime);
            }
            //increase animation timer
            animation_movement_swap_elapsed += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
        }

        public override void draw_animation(SpriteBatch spriteBatch) {
            //handle animation drawing (idle vs moving)
            if (get_health() > 0) {
                if (static_image_entity.HasValue && static_image_entity.Value) {
                    spriteBatch.Draw(texture, draw_position, null, draw_color, -rotation + rotation_offset, rotation_point, scale, SpriteEffects.None, 0f);
                } else if (moving && !fire_projectile) {
                    //draw walking animation
                    spriteBatch.Draw(texture, draw_position, walk_animation.source_rect, draw_color, -rotation + rotation_offset, rotation_point, scale, SpriteEffects.None, 0f);
                } else if (charging_projectile) {
                    //draw attack animation
                    spriteBatch.Draw(Constant.haunter_charge_tex, draw_position, charge_animation.source_rect, draw_color, -rotation + rotation_offset, rotation_point, scale, SpriteEffects.None, 0f);
                } else if (fire_projectile) {
                    spriteBatch.Draw(Constant.haunter_charge_hold_tex, draw_position, fire_projectile_animation.source_rect, draw_color, -rotation + rotation_offset, rotation_point, scale, SpriteEffects.None, 0f);
                    spriteBatch.Draw(Constant.diamond_expand_tex, draw_position, diamond_expand_animation.source_rect, draw_color * 0.85f, -rotation + rotation_offset, rotation_point, scale, SpriteEffects.None, 0f);
                } else {
                    //draw idle
                    spriteBatch.Draw(texture, draw_position, idle_animation.source_rect, draw_color, -rotation + rotation_offset, rotation_point, scale, SpriteEffects.None, 0f);
                }
            }
            spriteBatch.Draw(Constant.shadow_tex, draw_position, null, Color.Black * 0.5f, -rotation, rotation_point, scale, SpriteEffects.None, 0f);
        }
    }
}