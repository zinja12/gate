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
    public class ShadowKnight : Skeleton, IEntity, ICollisionEntity, IAiEntity
    {
        public ShadowKnight(Texture2D texture, Texture2D attack_texture, Texture2D charge_attack_texture, Vector2 base_position, float scale, Texture2D hit_texture, Player player, Dictionary<(int, int), List<IEntity>> chunked_collision_geometry, int ID, string identifier, bool? static_image_entity = null)
            : base(texture, attack_texture, charge_attack_texture, base_position, scale, hit_texture, player, chunked_collision_geometry, ID, identifier, static_image_entity) {
            //custom animation frame count
            this.idle_animation = new Animation(idle_animation_duration, 6, (int)nightmare_size * 1, 0, (int)nightmare_size, (int)nightmare_size);
            this.idle_animation.set_y_offset((int)nightmare_size*6);
            this.walk_animation = new Animation(walk_animation_duration, 6, (int)nightmare_size * 1, 0, (int)nightmare_size, (int)nightmare_size);
            this.attack_animation = new Animation(attack_animation_duration, 5, (int)nightmare_size * 2 * 1, 0, (int)nightmare_size*2, (int)nightmare_size*2);
            this.attack_charge_rectangle = new Rectangle(0, 0, (int)nightmare_size, (int)nightmare_size);
            this.animation_movement_swap_threshold = 200f;

            this.movement_speed = 0.6f;
            this.melee_attack_hitbox_active_threshold = 1000f;
            this.hitbox_deactivate_threshold = 200f;

            this.damage = 2;
            //higher health
            set_health(7);
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
                        walk_animation.set_y_offset((int)nightmare_size*3);
                        break;
                    case 1: //up right - ne
                        walk_animation.set_y_offset((int)nightmare_size*6);
                        break;
                    case 2: //right - east
                        walk_animation.set_y_offset((int)nightmare_size*1);
                        break;
                    case 3: //down right - se
                        walk_animation.set_y_offset((int)nightmare_size*5);
                        break;
                    case 4: //down - south
                        walk_animation.set_y_offset((int)nightmare_size*2);
                        break;
                    case 5: //down left - sw
                        walk_animation.set_y_offset((int)nightmare_size*4);
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
            } else if (melee_attack) {
                switch (Constant.TranslateAngleToCompassDirection(angle, MathHelper.ToDegrees(rotation))) {
                    case 0: // up - north
                        attack_animation.set_y_offset((int)nightmare_size*2*3);
                        attack_charge_rectangle.Y = (int)nightmare_size*1;
                        break;
                    case 1: //up right - ne
                        attack_animation.set_y_offset((int)nightmare_size*2*6);
                        attack_charge_rectangle.Y = (int)nightmare_size*6;
                        break;
                    case 2: //right - east
                        attack_animation.set_y_offset((int)nightmare_size*2*0);
                        attack_charge_rectangle.Y = (int)nightmare_size*3;
                        break;
                    case 3: //down right - se
                        attack_animation.set_y_offset((int)nightmare_size*2*5);
                        attack_charge_rectangle.Y = (int)nightmare_size*5;
                        break;
                    case 4: //down - south
                        attack_animation.set_y_offset((int)nightmare_size*2*2);
                        attack_charge_rectangle.Y = (int)nightmare_size*0;
                        break;
                    case 5: //down left - sw
                        attack_animation.set_y_offset((int)nightmare_size*2*4);
                        attack_charge_rectangle.Y = (int)nightmare_size*4;
                        break;
                    case 6: //left - west
                        attack_animation.set_y_offset((int)nightmare_size*2*1);
                        attack_charge_rectangle.Y = (int)nightmare_size*2;
                        break;
                    case 7: //up left - nw
                        attack_animation.set_y_offset((int)nightmare_size*2*7);
                        attack_charge_rectangle.Y = (int)nightmare_size*7;
                        break;
                    default:
                        Console.WriteLine("Nightmare AI: Default animation case");
                        attack_animation.set_y_offset((int)nightmare_size*2*0);
                        break;
                }
                // keep attack animation updated when melee attack is active
                //NOTE: since we have a charging frame before that to signal to the player an attack is coming, we only want to update the animation when the charging frame is finished
                if (melee_attack_hitbox_active_elapsed >= melee_attack_hitbox_active_threshold) {
                    attack_animation.Update(gameTime);
                }
            } else {
                //update idle animation based on last direction
                switch (last_movement_vector_idx) {
                    case 0: //up
                        idle_animation.set_y_offset((int)nightmare_size*11);
                        break;
                    case 1: //up right
                        idle_animation.set_y_offset((int)nightmare_size*14);
                        break;
                    case 2: //right
                        idle_animation.set_y_offset((int)nightmare_size*9);
                        break;
                    case 3: //down right
                        idle_animation.set_y_offset((int)nightmare_size*13);
                        break;
                    case 4: //down
                        idle_animation.set_y_offset((int)nightmare_size*10);
                        break;
                    case 5: //down left
                        idle_animation.set_y_offset((int)nightmare_size*12);
                        break;
                    case 6: //left
                        idle_animation.set_y_offset((int)nightmare_size*8);
                        break;
                    case 7: //up left
                        idle_animation.set_y_offset((int)nightmare_size*15);
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
    }
}