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
    //NPC behavior enum
    public enum AIBehavior {
        Stationary = 0,
        LoopPath = 1,
        Follow = 2
    }

    public class NPC : Nightmare, IEntity, ICollisionEntity, IAiEntity
    {
        //define custom size for npc
        protected int size = 32;

        private RRect interaction_box;
        
        //Behavior variables
        protected int current_ai_behavior;
        protected List<IEntity> path_points;
        protected IEntity current_path_point;
        protected int current_path_point_idx = 0;
        
        protected GameWorldDialogueFile conversation_file;
        protected string conversation_file_path_id;
        protected string npc_name;
        protected TextBox textbox;
        protected bool display_text = false, display_interaction = false;
        protected List<(string, string, string)> speaker_message_tags;
        protected Vector2 interaction_display_position;
        
        //NPC orient to direction variables
        protected int direction_case = -1;
        protected Vector2 direction_to_target = Vector2.Zero;
        protected float target_angle = 0f;

        private World world;

        public NPC(Texture2D texture, Vector2 base_position, float scale, int size, int initial_ai_behavior, GameWorldDialogueFile conversation_file, string conversation_file_path_id, Texture2D hit_texture, Player player, Dictionary<(int, int), List<IEntity>> chunked_collision_geometry, int ID, string identifier, World world, bool? static_image_entity = null) 
            : base(texture, null, base_position, scale, hit_texture, player, chunked_collision_geometry, ID, identifier, static_image_entity) {
            //NPC constructor
            this.size = size;
            this.interaction_box = new RRect(this.draw_position, (int)size*2f, (int)size*2f);
            this.hitbox_center_distance = size/2;
            this.hitbox_center = this.draw_position + new Vector2(hitbox_center_distance, 0);
            nightmare_size = this.size;
            this.draw_color = Color.Green;
            this.interaction_display_position = new Vector2(depth_sort_position.X, depth_sort_position.Y);

            //set appearance direction to down so they are not drawn facing up on initialization
            last_movement_vector_idx = 3;
            //set current behavior
            this.current_ai_behavior = initial_ai_behavior;
            movement_speed = 1.2f;
            path_points = new List<IEntity>();

            if (conversation_file != null) {
                this.conversation_file_path_id = conversation_file_path_id;
                this.conversation_file = conversation_file;
                this.speaker_message_tags = Constant.parse_dialogue_file(this.conversation_file);
                //initialize textbox
                textbox = new TextBox(Constant.textbox_screen_position, Constant.pxf_font, speaker_message_tags, npc_name, Constant.textbox_width, Constant.textbox_height, Color.Black, Color.White);
            }

            this.world = world;
        }

        public override void Update(GameTime gameTime) {
            gt = gameTime;
            this.rotation = 0f;
            depth_sort_position = draw_position + (size/2) * new Vector2(Constant.direction_down.X * (float)Math.Cos(-rotation) - Constant.direction_down.Y * (float)Math.Sin(-rotation), Constant.direction_down.Y * (float)Math.Cos(-rotation) + Constant.direction_down.X * (float)Math.Sin(-rotation));
            
            //update collision
            hurtbox.update(rotation, draw_position);
            update_hitbox_position(Constant.TranslateAngleToCompassDirection(angle, MathHelper.ToDegrees(rotation)));
            hitbox.update(rotation, hitbox_center);
            interaction_box.update(rotation, hitbox_center);
            //update animation
            update_animation(gameTime);

            //update textboxes
            update_npc_textbox(gameTime, rotation);

            //update hurtbox for scarecrow npc
            update_hurtbox_activity(gameTime, rotation);

            if (ai_behavior_enabled) {
                update_movement(rotation);
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

        public virtual void update_hurtbox_activity(GameTime gameTime, float rotation) {
            //set hurtbox active again
            if (!hurtbox_active) { //check if hurtbox is not active
                //add to hurtbox cooldown and color change cooldown
                take_hit_elapsed += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
                take_hit_color_change_elapsed += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
                //check for secondary and tertiary flashes
                if (take_hit_color_change_elapsed*2 < color_change_threshold) {
                    draw_color = Color.Red;
                }
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
        }

        public override void update_movement(float rotation) {
            switch(current_ai_behavior) {
                case (int)AIBehavior.LoopPath:
                    //TODO: implement some type of pathfinding
                    moving = true;
                    //ai follows a path based on set points
                    if (path_points.Count == 0) {
                        //no path points set, so set ai behavior to idle/stationary
                        current_ai_behavior = (int)AIBehavior.Stationary;
                    } else {
                        //path points exist so move the AI to those path points
                        //need to do some path-finding here at some point lol
                        if (current_path_point == null) {
                            //current path point not set, so we need to set it, move to it and then shift the current point
                            current_path_point_idx = 0;
                            current_path_point = path_points[current_path_point_idx];
                        } else {
                            direction_weights = assign_weights(movement_directions.ToArray(), current_path_point, true);
                            //if all direction weights are 0, we have reached the goal
                            if (direction_weights.All(x => x == 0) ? true : false) {
                                //move the current path point
                                current_path_point_idx++;
                                //check for out of bounds and loop path points
                                if (current_path_point_idx >= path_points.Count) {
                                    current_path_point_idx = 0;
                                }
                                current_path_point = path_points[current_path_point_idx];
                            }
                            //select largest weight
                            movement_vector_idx = select_best_weight(direction_weights);
                            //set direction vector based on chosen movement vector
                            direction = Vector2.Zero + movement_directions[movement_vector_idx];
                            //update last direction
                            last_direction.X = direction.X;
                            last_direction.Y = direction.Y;
                            last_movement_vector_idx = movement_vector_idx;
                            moving = true;

                            //later the position based on direction and movement speed
                            base_position += direction * movement_speed;
                            draw_position += direction * movement_speed;
                            attack_draw_position = draw_position + (size/2) * new Vector2(-1 * (float)Math.Cos(-rotation) - (-1) * (float)Math.Sin(-rotation), -1 * (float)Math.Cos(-rotation) + (-1) * (float)Math.Sin(-rotation));
                        }
                    }
                    break;
                case (int)AIBehavior.Follow:
                    //follow entity set as follow entity
                    //TODO: implement follow behavior
                    moving = true;
                    break;
                case (int)AIBehavior.Stationary:
                    //do nothing
                    moving = false;
                    break;
                default:
                    //do nothing
                    moving = false;
                    break;
            }
        }

        public override float[] assign_weights(Vector2[] movement_directions, IEntity goal_entity, bool ignore_circling) {
            // array list for dot product weights
            List<float> dot_product_weights = new List<float>();
            // loop over vectors and calculate the dot product
            for (int i = 0; i < movement_directions.Length; i++) {
                //make sure the movement direction is normalized
                movement_directions[i].Normalize();
                float weight = 0f;
                //if the player is within acceptable distance to the goal, we can set the weights to 0
                if (Vector2.Distance(get_base_position(), goal_entity.get_base_position()) <= 30f) {
                    weight = 0f;
                } else {
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
                        if (dist <= size) {
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

        public void update_npc_textbox(GameTime gameTime, float rotation) {
            this.interaction_display_position = Constant.rotate_point(draw_position, rotation, (-Constant.read_interact_tex.Height), Constant.direction_down);
            
            if (conversation_file != null) {
                //if displaying text
                if (display_text && !textbox.text_ended()) {
                    //update textbox
                    textbox.Update(gameTime, world);
                }
                
                if (textbox.text_ended()) {
                    display_text = false;
                }
            }

            //update interaction box
            interaction_box.update(rotation, draw_position);
        }

        public string find_first_matching_tag(Dictionary<string, bool> condition_tags) {
            List<string> npc_tags = new List<string>();
            foreach (GameWorldDialogue gw_dialogue in conversation_file.dialogue) {
                //pull tags
                if (!npc_tags.Contains(gw_dialogue.tag)) {
                    //add if it doesn't exist
                    npc_tags.Add(gw_dialogue.tag);
                }
            }

            //filter
            //we are using two lists here instead of a dictionary because dictionaries cannot have null keys
            //and we need to be able to support null string tags
            List<string> filtered_condition_tag_keys = new List<string>();
            List<bool> filtered_condition_tag_values = new List<bool>();
            //Dictionary<string, bool> filtered_condition_tags = condition_tags.Where(i => npc_tags.Contains(i.Key)).ToDictionary(i => i.Key, i => i.Value);
            foreach (KeyValuePair<string, bool> kv in condition_tags) {
                string condition_tag = kv.Key;
                bool condition_status = kv.Value;
                if (npc_tags.Contains(condition_tag)) {
                    filtered_condition_tag_keys.Add(condition_tag);
                    filtered_condition_tag_values.Add(condition_status);
                }
            }
            //NOTE: do not need to insert null tag into this because we return null as our base case at the end of the function
            //find the first valid tag
            for (int i = 0; i < filtered_condition_tag_keys.Count; i++) {
                string tag = filtered_condition_tag_keys[i];
                bool status = filtered_condition_tag_values[i];
                //we want to return the first tag that has not had the condition satisfied
                //the first condition that isn't met should have the matching dialogue displayed
                if (!status) {
                    return tag;
                }
            }
            return null;
        }

        public void orient_to_target(Vector2 target, float rotation) {
            //calculate rotated direction to target
            Vector2 direction_to_target = Constant.rotate_point(target - get_base_position(), rotation, 1, Constant.direction_up);
            //calculate angle from direction
            target_angle = MathHelper.ToDegrees((float)Math.Atan2(direction_to_target.Y, direction_to_target.X));
            //translate angle to direction case for animation
            direction_case = Constant.TranslateAngleToCompassDirection(target_angle, MathHelper.ToDegrees(rotation));
            //set last movement vector idx to direction case and let update animation handle the animation swap (in parent class)
            last_movement_vector_idx = direction_case;
        }

        public override bool is_aggro() {
            //NPCs are usually never aggro so set to false
            //but maybe we want to enable this behavior in a more complex fashion later on
            return false;
        }

        public void set_ai_behavior(AIBehavior ai_behavior) {
            this.current_ai_behavior = (int)ai_behavior;
        }

        public void set_path_points(List<IEntity> path_points) {
            this.path_points = path_points;
        }

        public void add_path_point(IEntity path_point) {
            this.path_points.Add(path_point);
        }

        public override GameWorldObject to_world_level_object() {
            return new GameWorldObject {
                object_identifier = identifier,
                object_id_num = get_obj_ID_num(),
                x_position = base_position.X,
                y_position = base_position.Y,
                scale = get_scale(),
                rotation = get_rotation_offset(),
                npc_path_entity_ids = path_points.ConvertAll(e => e.get_obj_ID_num()),
                npc_conversation_file_id = conversation_file_path_id
            };
        }

        public void set_display_interaction(bool display_interaction) {
            this.display_interaction = display_interaction;
        }

        public bool get_display_interaction() {
            return display_interaction;
        }

        public void display_textbox() {
            display_text = true;
        }
        
        public RRect get_interaction_box() {
            return interaction_box;
        }

        public TextBox get_textbox() {
            return textbox;
        }

        public Vector2 get_overhead_position() {
            Vector2 overhead_position = Constant.rotate_point(interaction_display_position, rotation, (-Constant.textbox_height/2), Constant.direction_down);
            return overhead_position;
        }

        public override Emotion get_emotion_trait() {
            return Emotion.Calm;
        }

        public override void Draw(SpriteBatch spriteBatch) {
            //call base draw function
            base.Draw(spriteBatch);

            //display interaction
            if (display_interaction && !display_text) {
                spriteBatch.Draw(Constant.read_interact_tex, interaction_display_position, null, Color.White, -rotation + rotation_offset, rotation_point, scale, SpriteEffects.None, 0f);
            }

            if (DEBUG) {
                interaction_box.draw(spriteBatch);
                spriteBatch.DrawString(Constant.arial_small, $"orientation_to_target:{direction_case}", get_base_position(), Color.Red);
                Renderer.DrawALine(spriteBatch, Constant.pixel, 1, Color.Red, 1, get_base_position(), get_base_position() + direction_to_target*50);
            }
        }
    }
}