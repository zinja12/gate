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
    public class Nightmare : IEntity, ICollisionEntity, IAiEntity
    {
        protected Vector2 base_position;
        protected Vector2 draw_position;
        protected Vector2 attack_draw_position;
        protected Vector2 depth_sort_position;
        
        private int ID;
        protected string identifier;
        protected int nightmare_size;
        private int health = 4;

        protected RRect hurtbox;
        protected bool hurtbox_active = true;
        protected float take_hit_elapsed;
        protected float reactivate_hurtbox_threshold = 400f;
        protected Vector2 hit_direction;
        protected float hit_speed = 0.03f;
        protected float take_hit_color_change_elapsed;
        protected float color_change_threshold = 200f;
        protected int hit_flash_count = 0;
        protected Color draw_color;

        protected Vector2 rotation_point;
        protected float scale;
        protected float rotation = 0.0f, rotation_offset = 0f;

        //animation vars
        protected Animation idle_animation, 
            walk_animation,
            attack_animation;
        protected float idle_animation_duration = 300f;
        protected float walk_animation_duration = 170f;
        protected float attack_animation_duration = 120f;
        protected float animation_movement_swap_elapsed = 0f, animation_movement_swap_threshold = 150f;
        protected int animation_case = 0;
        protected bool? static_image_entity;

        protected Texture2D texture, attack_texture;
        protected Texture2D hit_texture;
        protected HitConfirm hit_confirm;
        protected HitConfirm slash_confirm;

        protected bool show_hit_texture = false;
        protected int show_hit_texture_frame_count = 0;
        protected int show_hit_texture_frame_total = 10;
        private Vector2 hit_texture_position;
        private Vector2 hit_texture_rotation_point;
        private Vector2 hit_noise_position_offset = Vector2.Zero;
        private float noise_radius = 10f;
        private float noise_angle = 1f;

        /*AI Behavior Variables*/
        protected List<Vector2> movement_directions;
        private Vector2 current_movement_direction = Vector2.Zero;
        protected float[] direction_weights = new float[8];
        protected int movement_vector_idx;
        //direction vectors
        protected Vector2 direction;
        protected Vector2 last_direction;
        Vector2 goal_vector;
        protected int last_movement_vector_idx;
        protected bool moving = false;
        protected float movement_speed = 1.5f;
        //circling variables
        private int circle_direction = 0;
        protected bool circling = false;
        //attack variables
        protected float attack_timer_elapsed, time_between_attacks, melee_attack_hitbox_active_elapsed, time_since_death = 0f;
        private float hitbox_deactivate_elapsed;
        private bool melee_attack = false;
        private float melee_attack_hitbox_active_threshold = 500f, hitbox_deactivate_threshold = 200f;
        protected RRect hitbox;
        protected float hitbox_center_distance;
        protected Vector2 hitbox_center;
        protected bool ai_behavior_enabled = true;
        protected float striking_distance = 35f;

        //Damage variables
        List<IEntity> seen_projectiles;
        
        /*PARTICLE SYSTEM*/
        protected List<ParticleSystem> particle_systems;
        protected List<ParticleSystem> dead_particle_systems;
        
        protected float engagement_distance = Constant.nightmare_engagement_distance;

        protected Player player; // player var for use in AI behavior
        protected Random random;

        protected List<IAiEntity> enemies;
        
        public static bool DEBUG = false;
        
        public List<Color> blues;

        private float previous_rotation = 0f;
        protected float angle;

        protected int placement_source = 0;

        protected GameTime gt;

        public Nightmare(Texture2D texture, Texture2D attack_texture, Vector2 base_position, float scale, Texture2D hit_texture, Player player, int ID, string identifier, bool? static_image_entity = null) {
            this.nightmare_size = 32;
            this.hitbox_center_distance = nightmare_size/2;
            this.draw_color = Color.White;

            this.texture = texture;
            this.attack_texture = (attack_texture == null) ? Constant.nightmare_attack_tex : attack_texture;
            this.base_position = base_position;
            this.draw_position = new Vector2(base_position.X - (nightmare_size / 2), 
                                            base_position.Y - nightmare_size);
            this.depth_sort_position = this.draw_position + new Vector2(0, nightmare_size / 2);
            this.attack_draw_position = new Vector2(draw_position.X - (nightmare_size / 2), draw_position.Y - (nightmare_size / 2));
            this.scale = scale;
            this.rotation_point = new Vector2(nightmare_size / 2, nightmare_size / 2);
            this.ID = ID;
            this.identifier = identifier;
            
            this.hurtbox = new RRect(this.draw_position, nightmare_size, nightmare_size);
            this.hitbox = new RRect(this.draw_position, nightmare_size, nightmare_size);
            hitbox_center = this.draw_position + new Vector2(hitbox_center_distance, 0);

            this.hit_texture = hit_texture;
            this.hit_texture_rotation_point = new Vector2(hit_texture.Width/2, hit_texture.Height/2);

            this.idle_animation = new Animation(idle_animation_duration, 3, (int)nightmare_size * 1, 0, (int)nightmare_size, (int)nightmare_size);
            this.idle_animation.set_y_offset((int)nightmare_size*6);
            this.walk_animation = new Animation(walk_animation_duration, 3, (int)nightmare_size * 1, 0, (int)nightmare_size, (int)nightmare_size);
            this.attack_animation = new Animation(attack_animation_duration, 7, (int)nightmare_size*2*1, 0, (int)nightmare_size*2, (int)nightmare_size*2);

            this.player = player;

            //AI vars init
            movement_directions = new List<Vector2>();
            movement_directions.Add(new Vector2(0, -1)); //up - 0
            movement_directions.Add(new Vector2(1, -1)); //up right - 1
            movement_directions.Add(new Vector2(1, 0)); //right - 2
            movement_directions.Add(new Vector2(1, 1)); //down right - 3
            movement_directions.Add(new Vector2(0, 1)); //down - 4
            movement_directions.Add(new Vector2(-1, 1)); //down left - 5
            movement_directions.Add(new Vector2(-1, 0)); //left - 6
            movement_directions.Add(new Vector2(-1, -1)); //up left - 7
            direction_weights[0] = 0f;
            direction_weights[1] = 0f;
            direction_weights[2] = 0f;
            direction_weights[3] = 0f;
            direction_weights[4] = 0f;
            direction_weights[5] = 0f;
            direction_weights[6] = 0f;
            direction_weights[7] = 0f;
            direction = Vector2.Zero;
            last_direction = direction;

            //damage variables
            seen_projectiles = new List<IEntity>();

            //blues
            blues = new List<Color>();
            blues.Add(Color.AliceBlue);
            blues.Add(Color.Blue);
            blues.Add(Color.BlueViolet);
            blues.Add(Color.CadetBlue);
            blues.Add(Color.CornflowerBlue);
            blues.Add(Color.DarkBlue);
            blues.Add(Color.DarkSlateBlue);
            blues.Add(Color.DeepSkyBlue);

            random = new Random();
            //flip a coin to decide which direction enemy will circle when in range
            if (random.Next(0, 100) > 50) {
                circle_direction = 1;
            }
            //set the timer for attacks for this specific enemy
            //time_between_attacks = random.Next(1000, 5000);
            time_between_attacks = 500;

            //particle system code
            this.particle_systems = new List<ParticleSystem>();
            this.dead_particle_systems = new List<ParticleSystem>();

            //set appearance direction to down so they are not drawn facing up on initialization
            this.last_movement_vector_idx = 1;
            //offset the start of the animation slightly for each entity so they all do not start with the same animation frames (animation syncing)
            idle_animation.set_elapsed((float)random.Next(0, (int)idle_animation_duration-1));
            this.static_image_entity = static_image_entity;
        }

        public virtual void Update(GameTime gameTime, float rotation) {
            if (is_dead()) return;

            gt = gameTime;
            this.rotation = rotation;
            depth_sort_position = draw_position + (nightmare_size/2) * new Vector2(Constant.direction_down.X * (float)Math.Cos(-rotation) - Constant.direction_down.Y * (float)Math.Sin(-rotation), Constant.direction_down.Y * (float)Math.Cos(-rotation) + Constant.direction_down.X * (float)Math.Sin(-rotation));

            //update collision
            hurtbox.update(rotation, draw_position);
            update_hitbox_position(Constant.TranslateAngleToCompassDirection(angle, MathHelper.ToDegrees(rotation)));
            hitbox.update(rotation, hitbox_center);
            //update animation
            update_animation(gameTime);

            //set hurtbox active again
            if (!hurtbox_active) { //check if hurtbox is not active
                //add to hurtbox cooldown and color change cooldown
                take_hit_elapsed += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
                take_hit_color_change_elapsed += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
                //check for secondary and tertiary flashes
                if (take_hit_color_change_elapsed*2 < color_change_threshold) {
                    draw_color = Color.Red;
                }
                //rotate hit_direction based on camera
                hit_direction = Constant.rotate_point(hit_direction, rotation, 1f, Constant.direction_down);
                //move enemy in hit direction after taking the hit
                draw_position += hit_direction * hit_speed;
                attack_draw_position = draw_position + (nightmare_size/2) * new Vector2(-1 * (float)Math.Cos(-rotation) - (-1) * (float)Math.Sin(-rotation), -1 * (float)Math.Cos(-rotation) + (-1) * (float)Math.Sin(-rotation));
                //update state to ensure no conflicts
                //melee_attack = false;
                //reset all animations
                attack_animation.reset_animation();
                walk_animation.reset_animation();
                idle_animation.reset_animation();
            }
            if (take_hit_elapsed >= reactivate_hurtbox_threshold) {
                hurtbox_active = true;
                take_hit_elapsed = 0;
            }
            if (take_hit_color_change_elapsed >= color_change_threshold) {
                draw_color = Color.White;
                take_hit_color_change_elapsed = 0;
                hit_flash_count++;
            }

            //show hit texture for certain amount of frames
            if (show_hit_texture_frame_count < show_hit_texture_frame_total && show_hit_texture) {
                show_hit_texture_frame_count++;
                if (hit_confirm != null)
                    hit_confirm.Update(gameTime, rotation);
                if (slash_confirm != null)
                    slash_confirm.Update(gameTime, rotation);
            } else {
                hit_confirm = null;
                slash_confirm = null;
                show_hit_texture = false;
                show_hit_texture_frame_count = 0;
            }

            if (hurtbox_active && ai_behavior_enabled) {
                //AI movement behavior
                update_movement(rotation);
            }

            //update time_since_death on health == 0
            if (health <= 0) {
                time_since_death += (float)gt.ElapsedGameTime.TotalMilliseconds;
            }

            /*PARTICLE SYSTEMS CODE*/
            //test particle system code
            foreach (ParticleSystem ps in particle_systems) {
                ps.Update(gameTime, rotation);
                //add dead systems to dead list
                if (ps.is_finished()) {
                    dead_particle_systems.Add(ps);
                }
            }
            //clear dead particle systems
            foreach (ParticleSystem ps in dead_particle_systems) {
                particle_systems.Remove(ps);
            }
            dead_particle_systems.Clear();
            /*END PARTICLE SYSTEMS*/

            previous_rotation = rotation;
        }

        public virtual void update_movement(float rotation) {
            if (circling && !melee_attack) {
                //start attack timer
                attack_timer_elapsed += (float)gt.ElapsedGameTime.TotalMilliseconds;
                
                //check for if we have passed the time in between attacks
                if (attack_timer_elapsed >= time_between_attacks) {
                    //reset variables
                    attack_timer_elapsed = 0;
                    circling = false;
                    moving = false;
                    //trigger attack
                    melee_attack = true;
                }
            }
            
            if (!melee_attack) {
                if (Vector2.Distance(get_base_position(), player.get_base_position()) < engagement_distance) {
                    //enemy aggro'd so set new engagement distance
                    engagement_distance = Constant.nightmare_aggro_engagement_distance;
                    //player is within range to start moving and calculating directions
                    direction_weights = assign_weights(movement_directions.ToArray(), player, false);
                    //now that we have the weights/scores for each of the possible directions all we need to do is pick the largest one
                    movement_vector_idx = select_best_weight(direction_weights);
                    //set direction vector based on chosen movement vector
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
                    //can reset aggro if player gets too far away
                    moving = false;
                    engagement_distance = Constant.nightmare_engagement_distance;
                }
            } else if (melee_attack) {
                //melee attack
                circling = false;
                moving = false;
                melee_attack_hitbox_active_elapsed += (float)gt.ElapsedGameTime.TotalMilliseconds;
                if (melee_attack_hitbox_active_elapsed >= melee_attack_hitbox_active_threshold) {
                    hitbox_deactivate_elapsed += (float)gt.ElapsedGameTime.TotalMilliseconds;
                }
                //calculate new direction weights without circling behavior active
                direction_weights = assign_weights(movement_directions.ToArray(), player, true);
                //select new best weight
                movement_vector_idx = select_best_weight(direction_weights);
                //set direction vector based on chosen movement vector
                //this should then update the animation direction
                direction = Vector2.Zero + movement_directions[movement_vector_idx];
                if (attack_animation.looped_once()) {
                    melee_attack = false;
                    melee_attack_hitbox_active_elapsed = 0;
                    attack_animation.set_looped_once(false);
                }
            }
        }

        //loop over potential movement directions and assign weights/scores to which direction will move towards the goal
        public virtual float[] assign_weights(Vector2[] movement_directions, IEntity goal_entity, bool ignore_circling) {
            // array list for dot product weights
            List<float> dot_product_weights = new List<float>();
            // loop over vectors and calculate the dot product
            for (int i = 0; i < movement_directions.Length; i++) {
                //make sure the movement direction is normalized
                movement_directions[i].Normalize();
                float weight = 0f;
                //if the player is within striking distance (55f) we can start to prioritize horizontal movemnet to circle around the player for better strafing hits
                if (Vector2.Distance(get_base_position(), goal_entity.get_base_position()) <= striking_distance && !ignore_circling) {
                    circling = true;
                    //note: need to calculate the dot product with the normal vector as the goal rather than just the player because we want to be moving perpendicular to the goal rather than directly towards it
                    //calculate normal(s)
                    goal_vector = goal_entity.get_base_position() - draw_position;
                    //rotate goal vector
                    goal_vector = Constant.rotate_point(goal_vector, rotation, 1f, Constant.direction_up);
                    //calculate two normals
                    Vector2 normal1 = new Vector2(-goal_vector.Y, goal_vector.X);
                    Vector2 normal2 = new Vector2(goal_vector.Y, -goal_vector.X);
                    //calculate dot product weights with normal vector instead for circling after flipping a coin about which one to use
                    if (circle_direction == 0) {
                        //dot_product_weights.Add(Vector2.Dot(movement_directions[i], normal1));
                        weight += Vector2.Dot(movement_directions[i], normal1);
                    } else {
                        //dot_product_weights.Add(Vector2.Dot(movement_directions[i], normal2));
                        weight += Vector2.Dot(movement_directions[i], normal2);
                    }
                } else {
                    circling = false;
                    //weight/score is calculated by the dot product of the potential movement direction and the direction in which the goal is set
                    //then add each of those scores to the array in order to preserve index ordering
                    //dot_product_weights.Add(Vector2.Dot(movement_directions[i], (goal_entity.get_base_position() - draw_position)));
                    weight += Vector2.Dot(movement_directions[i], (goal_entity.get_base_position() - draw_position));
                }
                //loop over enemies
                float weight_modifier = 0f;
                foreach (IAiEntity e in enemies) {
                    //ensure we are not checking against this object (ourselves) because it will break everything and throw off all calculations
                    if (get_ID_num() !=  e.get_ID_num()) {
                        IEntity entity = (IEntity)e;
                        float dist = Vector2.Distance(draw_position, entity.get_base_position());
                        if (dist <= nightmare_size) {
                            weight_modifier = Vector2.Dot(movement_directions[i], -(entity.get_base_position() - draw_position));
                        } else {
                            weight_modifier = 0f;
                        }
                    }
                }
                //add to weights list
                dot_product_weights.Add(weight+weight_modifier);
            }

            // return weights
            return dot_product_weights.ToArray();
        }

        //basically just pick the best/largest weight/score and that is the direction that we should be going
        public virtual int select_best_weight(float[] weights) {
            // set the max weight to the first value, so we always have a best weight
            float max_weight = weights[0];
            int max_weight_idx = 0;
            // loop over weights and check against max to set new max
            for (int i = 0; i < weights.Length; i++) {
                if (weights[i] > max_weight) {
                    max_weight = weights[i];
                    max_weight_idx = i;
                }
            }

            //return index
            return max_weight_idx;
        }

        public virtual void update_animation(GameTime gameTime) {
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
                        walk_animation.set_y_offset((int)nightmare_size*1);
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
                        walk_animation.set_y_offset((int)nightmare_size*0);
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
                        break;
                    case 1: //up right - ne
                        attack_animation.set_y_offset((int)nightmare_size*2*1);
                        break;
                    case 2: //right - east
                        attack_animation.set_y_offset((int)nightmare_size*2*1);
                        break;
                    case 3: //down right - se
                        attack_animation.set_y_offset((int)nightmare_size*2*1);
                        break;
                    case 4: //down - south
                        attack_animation.set_y_offset((int)nightmare_size*2*2);
                        break;
                    case 5: //down left - sw
                        attack_animation.set_y_offset((int)nightmare_size*2*0);
                        break;
                    case 6: //left - west
                        attack_animation.set_y_offset((int)nightmare_size*2*0);
                        break;
                    case 7: //up left - nw
                        attack_animation.set_y_offset((int)nightmare_size*2*0);
                        break;
                    default:
                        Console.WriteLine("Nightmare AI: Default animation case");
                        attack_animation.set_y_offset((int)nightmare_size*2*0);
                        break;
                }
                // keep attack animation updated when melee attack is active
                attack_animation.Update(gameTime);
            } else {
                //update idle animation based on last direction
                switch (last_movement_vector_idx) {
                    case 0: //up
                        idle_animation.set_y_offset((int)nightmare_size*9);
                        break;
                    case 1: //up right
                        idle_animation.set_y_offset((int)nightmare_size*7);
                        break;
                    case 2: //right
                        idle_animation.set_y_offset((int)nightmare_size*7);
                        break;
                    case 3: //down right
                        idle_animation.set_y_offset((int)nightmare_size*11);
                        break;
                    case 4: //down
                        idle_animation.set_y_offset((int)nightmare_size*8);
                        break;
                    case 5: //down left
                        idle_animation.set_y_offset((int)nightmare_size*10);
                        break;
                    case 6: //left
                        idle_animation.set_y_offset((int)nightmare_size*6);
                        break;
                    case 7: //up left
                        idle_animation.set_y_offset((int)nightmare_size*6);
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

        public void update_hitbox_position(int direction_idx) {
            Vector2 hitbox_direction = movement_directions[direction_idx];
            //rotate hitbox center around draw_position
            hitbox_center = draw_position + hitbox_center_distance * new Vector2(hitbox_direction.X * (float)Math.Cos(-rotation) - hitbox_direction.Y * (float)Math.Sin(-rotation), hitbox_direction.Y * (float)Math.Cos(-rotation) + hitbox_direction.X * (float)Math.Sin(-rotation));
        }

        public bool is_hurtbox_active() {
            return hurtbox_active;
        }

        public bool hitbox_active() {
            return melee_attack && 
            melee_attack_hitbox_active_elapsed >= melee_attack_hitbox_active_threshold &&
            melee_attack_hitbox_active_elapsed <= hitbox_deactivate_threshold + melee_attack_hitbox_active_threshold;
        }

        public bool is_moving() {
            return moving;
        }

        public virtual bool is_aggro() {
            //enemy is only aggro when it is moving which is basically only when it's engagement distance is set to the aggro engagement distance
            return engagement_distance == Constant.nightmare_aggro_engagement_distance;
        }

        public RRect get_hurtbox() {
            return hurtbox;
        }

        public RRect get_hitbox() {
            return hitbox;
        }

        public void set_obj_ID_num(int id_num) {
            this.ID = id_num;
        }

        public int get_obj_ID_num() {
            return this.ID;
        }

        public void set_base_position(Vector2 position) {
            base_position = position;
            this.draw_position = new Vector2(base_position.X - (nightmare_size / 2), 
                                            base_position.Y - nightmare_size);
        }

        public float get_rotation_offset() {
            return rotation_offset;
        }

        public void set_rotation_offset(float rotation_offset_degrees) {
            float radians_offset = MathHelper.ToRadians(rotation_offset_degrees);
            rotation_offset = radians_offset;
        }

        public virtual GameWorldObject to_world_level_object() {
            return new GameWorldObject {
                object_identifier = identifier,
                object_id_num = get_obj_ID_num(),
                x_position = base_position.X,
                y_position = base_position.Y,
                scale = get_scale(),
                rotation = get_rotation_offset()
            };
        }

        public void set_ai_entities(List<IAiEntity> enemies) {
            this.enemies = enemies;
        }

        public string get_id() {
            return identifier;
        }

        public int get_ID_num() {
            return ID;
        }

        public bool is_dead() {
            //only stop updating when the health is 0 and the time since death is greater than the threshold
            return health <= 0 && time_since_death >= 500f;
        }

        public void set_placement_source(int placement_source_value) {
            this.placement_source = placement_source_value;
        }

        public int get_placement_source() {
            return this.placement_source;
        }

        public void take_hit(IEntity entity, int damage) {
            int health_decrease_value = damage;
            if (entity is Arrow) {
                Arrow a = (Arrow)entity;
                if (a.is_power_shot()) {
                    //upgrade the health decrease value if the arrow is a power shot
                    health_decrease_value += 3;
                }
                //don't take damage from an arrow that has already struck (supposed to pass through)
                if (seen_projectiles.Contains(entity)) return;
                //add to seen projectiles if we get this far
                seen_projectiles.Add(entity);
            }
            //reduce health on hit
            health -= health_decrease_value;
            if (health <= 0) {
                particle_systems.Add(new ParticleSystem(true, Constant.rotate_point(draw_position, rotation, 1f, Constant.direction_up), 2, 800f, 1, 5, 1, 3, Constant.red_particles, new List<Texture2D>() { Constant.footprint_tex }));
            }
            //turn off hurt box for a bit
            hurtbox_active = false;
            //calculate hit direction
            hit_direction = get_base_position() - entity.get_base_position();
            //set color to red to indicate hit taken
            draw_color = Color.Red;
            //show hit texture
            show_hit_texture = true;
            //calculate hit texture direction
            hit_texture_position = get_base_position() - entity.get_base_position();
            hit_texture_position = Constant.rotate_point(hit_texture_position, rotation, 0.1f, Constant.direction_up);
            hit_texture_position.Normalize();
            hit_texture_position *= Vector2.Distance(get_base_position(), entity.get_base_position())/2;
            hit_texture_position += entity.get_base_position();
            hit_confirm = new HitConfirm(Constant.hit_confirm_spritesheet, hit_texture_position, 1f, 100f);
            //calcualte noise to add for slash effect
            hit_noise_position_offset = new Vector2((float)(Math.Sin(noise_angle) * noise_radius), (float)(Math.Cos(noise_angle) * noise_radius));
            slash_confirm = new HitConfirm(Constant.slash_confirm_spritesheet, hit_texture_position + hit_noise_position_offset, 1f, 10f);
        }

        public Vector2 get_base_position() {
            return depth_sort_position;
        }

        public float get_scale() {
            return scale;
        }

        public void set_scale(float scale_value) {
            this.scale = scale_value;
        }

        public string get_flag(){
            return Constant.ENTITY_ACTIVE;
        }

        public void set_behavior_enabled(bool value) {
            ai_behavior_enabled = value;
        }

        public int get_health() {
            return health;
        }

        public void set_health(int health_value) {
            health = health_value;
        }

        public virtual Emotion get_emotion_trait() {
            return Emotion.Fear;
        }

        public int get_damage() {
            return 1;
        }

        public virtual void draw_animation(SpriteBatch spriteBatch) {
            //handle animation drawing (idle vs moving)
            if (health > 0) {
                if (static_image_entity.HasValue && static_image_entity.Value) {
                    spriteBatch.Draw(texture, draw_position, null, draw_color, -rotation + rotation_offset, rotation_point, scale, SpriteEffects.None, 0f);
                } else if (moving && !melee_attack) {
                    //draw walking animation
                    spriteBatch.Draw(texture, draw_position, walk_animation.source_rect, draw_color, -rotation + rotation_offset, rotation_point, scale, SpriteEffects.None, 0f);
                } else if (melee_attack) {
                    //draw attack animation
                    spriteBatch.Draw(attack_texture, attack_draw_position, attack_animation.source_rect, draw_color, -rotation + rotation_offset, rotation_point, scale, SpriteEffects.None, 0f);
                } else {
                    //draw idle
                    spriteBatch.Draw(texture, draw_position, idle_animation.source_rect, draw_color, -rotation + rotation_offset, rotation_point, scale, SpriteEffects.None, 0f);
                }
            }
        }

        public virtual void Draw(SpriteBatch spriteBatch) {
            //if dead return
            if (is_dead()) return;

            //draw shadow always
            spriteBatch.Draw(Constant.shadow_tex, draw_position, null, Color.Black * 0.5f, -rotation, rotation_point, scale, SpriteEffects.None, 0f);

            /*PARTICLE SYSTEMS*/
            foreach (ParticleSystem ps in particle_systems) {
                ps.Draw(spriteBatch);
            }

            draw_animation(spriteBatch);
            
            //draw hits
            if (show_hit_texture && hit_confirm != null) {
                hit_confirm.Draw(spriteBatch);
                //spriteBatch.Draw(hit_texture, hit_texture_position, null, Color.White * 0.8f, hit_texture_rotation, hit_texture_rotation_point, scale, SpriteEffects.None, 0f);
            }
            if (slash_confirm != null) {
                slash_confirm.Draw(spriteBatch);
            }
            if (DEBUG) {
                hurtbox.draw(spriteBatch);
                hitbox.draw(spriteBatch);
                // Renderer.DrawALine(spriteBatch, Constant.pixel, 2, Color.Pink, draw_position, draw_position + new Vector2(0, -5));
                // Renderer.DrawALine(spriteBatch, Constant.pixel, 2, Color.Yellow, depth_sort_position, depth_sort_position + new Vector2(0, -5));

                // for (int i = 0; i < movement_directions.Count; i++) {
                //     Vector2 mv = movement_directions[i];
                //     Vector2 draw_to = draw_position + mv*(1/direction_weights[i]);
                //     Vector2 draw_weight_position = draw_position + mv*27;
                //     Renderer.DrawALine(spriteBatch, Constant.pixel, 2, Color.Blue, draw_position, draw_to);
                //     spriteBatch.DrawString(Constant.arial, "" + Constant.truncate_to_sig_digits((double)direction_weights[i], 2), draw_weight_position, Color.Black, -rotation, Vector2.Zero, 0.12f, SpriteEffects.None, 0f);
                // }

                //draw movement directions
                int blue_offset = 0;
                foreach (Vector2 v in movement_directions) {
                    if (blue_offset > 7) {
                        blue_offset = 0;
                    }
                    Vector2 draw_to = draw_position + v*2*direction_weights[blue_offset];
                    Vector2 draw_weight_position = draw_position + v*27;
                    Renderer.DrawALine(spriteBatch, Constant.pixel, 2, blues[blue_offset], 1f, draw_position, draw_to);
                    spriteBatch.DrawString(Constant.arial, "" + Constant.truncate_to_sig_digits((double)direction_weights[blue_offset], 2), draw_weight_position, Color.Black, -rotation, Vector2.Zero, 0.12f, SpriteEffects.None, 0f);
                    blue_offset++;
                }
                Vector2 goal = new Vector2(direction.X, direction.Y);
                goal.Normalize();
                Renderer.DrawALine(spriteBatch, Constant.pixel, 2, Color.Red, 1f, draw_position, draw_position + goal*12);
                //draw angle string
                spriteBatch.DrawString(Constant.arial, "angle:" + Constant.truncate_to_sig_digits((double)angle, 2), draw_position - new Vector2(0, 50), Color.Black, -rotation, Vector2.Zero, 0.12f, SpriteEffects.None, 0f);
            }
        }
    }
}