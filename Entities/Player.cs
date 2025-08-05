using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using gate.Particles;
using gate.Serialize;
using gate.Interface;
using gate.Core;
using gate.Collision;

namespace gate.Entities
{
    public class Player : IEntity, ICollisionEntity
    {
        //position
        public Vector2 base_position;
        public Vector2 draw_position;
        public Vector2 attack_draw_position;
        public Vector2 depth_sort_position;
        public Vector2 camera_track_position;
        public Vector2 center_position;
        public Vector2 aim_orbit;
        public Vector2 emotion_display_position;

        //rotation
        private Vector2 rotation_point;
        private float scale;
        private float rotation = 0.0f, rotation_offset = 0f;

        //animation
        private Animation walk_animation,
            idle_animation,
            dash_animation,
            attack_animation,
            heavy_attack_animation;
        private Rectangle charging_animation_rect, aiming_animation_rect;
        private float walk_animation_duration = 125f;
        private float idle_animation_duration = 300f;
        private float dash_animation_duration = 85f;
        private float attack_animation_duration = 120f;

        //foot prints / animation
        private List<Footprints> footprints;
        private List<Footprints> reap_footprints;
        private float footprints_elapsed;
        private float footprints_delay = 200f;
        private int footprint_sound = 0;

        //movement and input
        private float base_movement_speed = 2.0f, movement_speed = 2.0f, fear_movement_speed = 0.8f;
        private float _v, _h, _dash, _attack, _interact, _heavy_attack, _fire, _aim;
        private float _select;//, select_pressed_count = 0;
        private Vector2 direction;
        private Vector2 last_direction;
        private Vector2 dash_direction;
        private Vector2 resultant;
        private Vector2 direction_down = new Vector2(0, 1);
        private bool moving = false;
        private bool movement_disabled = false;
        private bool hitstun_active = false;

        //dash
        private bool dash_active;
        private int dash_charge = Constant.player_dash_charge;
        private int max_dashes = 0;
        private Vector2 dash_direction_unit;
        private Vector2 dash_start;
        private Vector2 dash_queued_direction;
        private float dash_speed = Constant.player_dash_speed;
        private float dash_length = Constant.player_dash_length;
        private float dash_cooldown = Constant.player_dash_cooldown; //cooldown in milliseconds
        private float dash_cooldown_elapsed = Constant.player_dash_cooldown; //set cooldown elapsed to same as cooldown so you can dash on level load
        private float doubledash_cool_down = Constant.player_doubledash_cooldown; //doubledash cooldown in milliseconds
        private bool dash_queued = false;

        //grenades
        private int grenade_charge = Constant.player_grenade_charge;
        private int max_grenades = Constant.player_grenade_charge;
        private float grenade_cooldown_elapsed = Constant.player_grenade_cooldown;

        //player size
        private const float player_size = 32;
        private const float player_attack_sprite_size = player_size*2;

        //larger context variables
        public static bool DEBUG = false;
        public float screen_width;
        public float screen_height;

        //gamepad input
        KeyboardState current_keyboard_state, previous_keyboard_state;
        GamePadState current_gamepad_state, previous_gamepad_state;

        //collision and hitboxes
        private RRect hurtbox;
        private RRect hitbox;
        private RRect future_hurtbox;
        private RRect future_hurtbox_x;
        private RRect future_hurtbox_y;
        private RRect shadowcast_box;
        private Vector2 hitbox_center;
        private Vector2 hitbox_draw_position;
        private float hitbox_center_distance = player_size/2;
        private float hitbox_scale_factor = 1.0f;
        private Vector2 hitbox_dir;
        //hurtbox/hit variables
        private bool hurtbox_active = true;
        private float take_hit_elapsed;
        private float reactivate_hurtbox_threshold = 800f;
        private Vector2 hit_direction;
        private float hit_speed = 0.015f;
        private float take_hit_color_change_elapsed;
        private float color_change_threshold = 200f;
        private int hit_flash_count = 0;
        private Color draw_color = Color.White;
        private Texture2D hit_texture;
        private HitConfirm hit_confirm;
        private HitConfirm slash_confirm;
        private bool show_hit_texture = false;
        private int show_hit_texture_frame_count = 0;
        private int show_hit_texture_frame_total = 10;
        private Vector2 hit_texture_position;
        private Vector2 hit_texture_rotation_point;
        private Vector2 hit_noise_position_offset = Vector2.Zero;
        private float noise_radius = 10f;
        private float noise_angle = 1f;

        Dictionary<IEntity, bool> collision_geometry_map_x, collision_geometry_map_y;
        Dictionary<IEntity, bool> collision_tile_map;

        //attack vars
        private bool attack_active, heavy_attack_active, charging_active, aiming, charging_arrow;
        private int attack_charge = Constant.player_attack_charge;
        private int max_attack_charges = Constant.player_attack_charge;
        private float attack_cooldown = Constant.player_attack_cooldown;
        private float attack_cooldown_elapsed = Constant.player_attack_cooldown;
        private float doubleattack_cooldown = Constant.player_doubleattack_cooldown;
        private float attack_duration = 300f;
        private float hitbox_deactivate_elapsed, hitbox_deactivate_threshold = 200f;
        private float arrow_charge_elapsed, arrow_charge_threshold = 200f;

        private float attack_charged_elapsed = 0f;
        private float sweet_spot_elapsed = 0f;
        private float sweet_spot_start_duration = 500f, sweet_spot_duration = 650f;
        private bool sweet_spot = false;
        private Color sweet_spot_color = Color.Red;

        //arrow vars
        IEntity arrow;
        private int arrow_charge = Constant.player_arrow_charge, max_arrows = Constant.player_max_arrows;
        private float power_shot_elapsed = 0f, power_shot_start_threshold = 500, power_shot_duration = 650f;
        private bool power_shot = false;
        private Color power_shot_color = Color.Red;

        /*PLAYER HEALTH*/
        private int health = 3;
        /*PLAYER MONEY*/
        private int money = 0;

        //idle variables
        private bool idle;
        private float idle_elapsed;
        private float idle_threshold = 60f;

        //camera control vars
        private bool camera_tracking_X = true, camera_tracking_Y = true;

        //particle variables
        List<ParticleSystem> particle_systems;
        List<ParticleSystem> dead_particle_systems;

        //player attributes
        bool sword_attribute = false;
        bool dash_attribute = false;
        bool bow_attribute = false;

        //player emotion
        private Emotion current_emotion;
        private float emotion_elapsed = 0f, emotion_elapsed_threshold = 2500f;

        private int ID;

        //random
        Random random;

        //World variable to trigger world events on player triggers
        World world;

        public Player(Vector2 base_position, float scale, float screen_width, float screen_height, int ID, World world) {
            this.base_position = base_position;
            this.draw_position = new Vector2(base_position.X - (player_size / 2), 
                                            base_position.Y - player_size);
            this.attack_draw_position = new Vector2(draw_position.X - (player_size / 2), draw_position.Y - (player_size / 2));
            this.depth_sort_position = this.draw_position + new Vector2(0, player_size/2);
            this.camera_track_position = base_position;
            this.emotion_display_position = new Vector2(depth_sort_position.X, depth_sort_position.Y);
            this.scale = scale;
            this.rotation_point = new Vector2(player_size / 2, player_size /2);
            //initialize animations with their appropriate starting frames on the spritesheets
            this.walk_animation = new Animation(walk_animation_duration, 6, (int)player_size * 1, 0, (int)player_size, (int)player_size);
            this.idle_animation = new Animation(idle_animation_duration, 6, (int)player_size * 1, 0, (int)player_size, (int)player_size);
            this.dash_animation = new Animation(dash_animation_duration, 4, (int)player_size * 1, 0, (int)player_size, (int)player_size);
            this.attack_animation = new Animation(attack_animation_duration, 5, (int)player_size * 2 * 1, 0, (int)player_size*2, (int)player_size*2);
            this.heavy_attack_animation = new Animation(attack_animation_duration, 5, (int)player_size*2 * 1, 0, (int)player_size*2, (int)player_size*2);
            this.charging_animation_rect = new Rectangle((int)player_size*2*0, (int)player_size*2*0, (int)player_size*2, (int)player_size*2);
            this.aiming_animation_rect = new Rectangle((int)player_size*0, (int)player_size*0, (int)player_size, (int)player_size);

            this.direction = Vector2.Zero;
            
            //footprints init
            this.footprints = new List<Footprints>();
            this.reap_footprints = new List<Footprints>();
            
            //dash init
            this.dash_active = false;
            this.dash_direction_unit = Vector2.Zero;
            this.last_direction = new Vector2(1, 0);

            //attack init
            this.attack_active = false;

            //set screen width and height
            this.screen_width = screen_width;
            this.screen_height = screen_height;

            //set collision and hitboxes
            hurtbox = new RRect(this.draw_position, player_size/2, player_size);
            //make hitbox bigger than the player because it's better for game feel (multiply by 1.5f)
            hitbox = new RRect(this.draw_position, player_size*hitbox_scale_factor, player_size*hitbox_scale_factor);
            hitbox_center = this.draw_position + new Vector2(hitbox_center_distance, 0);
            hitbox_draw_position = hitbox_center + new Vector2(-player_size/2, -player_size/2);
            shadowcast_box = new RRect(this.draw_position, player_size/4, player_size);

            //test particle system code
            this.particle_systems = new List<ParticleSystem>();
            this.dead_particle_systems = new List<ParticleSystem>();

            //hit textures
            this.hit_texture = Constant.hit_confirm_spritesheet;
            this.hit_texture_rotation_point = new Vector2(hit_texture.Width/2, hit_texture.Height/2);

            //set initial emotion
            this.current_emotion = Emotion.Calm;

            this.ID = ID;

            this.random = new Random();

            collision_geometry_map_x = new Dictionary<IEntity, bool>();
            collision_geometry_map_y = new Dictionary<IEntity, bool>();
            collision_tile_map = new Dictionary<IEntity, bool>();
            resultant = Vector2.Zero;

            //world variable reference
            this.world = world;
        }

        public void Update(GameTime gameTime) {
            Constant.profiler.start("player_update");
            // set rotation
            this.rotation = 0f;
            
            //NOTE: make sure to use math.wrapangle before converting rotation radians to degrees

            //get input
            if (!GamePad.GetState(PlayerIndex.One).IsConnected) { //keyboard input
                current_keyboard_state = Keyboard.GetState();
                _v = key_down(current_keyboard_state, Constant.KEY_DOWN) - key_down(current_keyboard_state, Constant.KEY_UP); //vertical input (y-axis)
                _h = key_down(current_keyboard_state, Constant.KEY_RIGHT) - key_down(current_keyboard_state, Constant.KEY_LEFT); //horizonal input (x-axis)
                _dash = key_down(current_keyboard_state, Constant.KEY_DASH);
                _attack = key_down(current_keyboard_state, Constant.KEY_ATTACK);
                _heavy_attack = key_down(current_keyboard_state, Constant.KEY_HEAVY_ATTACK);
                _interact = key_down(current_keyboard_state, Constant.KEY_INTERACT);
                _aim = key_down(current_keyboard_state, Constant.KEY_AIM);
                _fire = key_down(current_keyboard_state, Constant.KEY_FIRE);
                _select = key_down(current_keyboard_state, Constant.KEY_GRENADE);
            } else { //controller input
                current_gamepad_state = GamePad.GetState(PlayerIndex.One);
                //get left thumbstick current position and set _v and _h
                _v = -current_gamepad_state.ThumbSticks.Left.Y;
                _h = current_gamepad_state.ThumbSticks.Left.X;
                _dash = current_gamepad_state.Buttons.A == ButtonState.Pressed ? 1f : 0f;
                _attack = current_gamepad_state.Buttons.X == ButtonState.Pressed ? 1f : 0f;
                _heavy_attack = current_gamepad_state.Triggers.Right > 0 ? 1f: 0f;
                _interact = current_gamepad_state.Buttons.Y == ButtonState.Pressed ? 1f : 0f;
                //trigger has to be at least halfway held down
                _aim = current_gamepad_state.Triggers.Left > 0 ? 1f : 0f;
                _fire = current_gamepad_state.Buttons.B == ButtonState.Pressed ? 1f : 0f;
                _select = current_gamepad_state.Buttons.Start == ButtonState.Pressed ? 1f : 0f;
            }

            //if the player is providing input, do not need to check how long they are idle for
            if (!is_input()) {
                idle_elapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (idle_elapsed >= idle_threshold) {
                    idle = true;
                }
            } else {
                idle_elapsed = 0;
                idle = false;
            }

            // if attack active and dashing -> queue dash when attack ends
            if ((attack_active || heavy_attack_active) && _dash != 0) {
                if (_h == 0 && _v == 0) { //cannot normalize a vector of 0,0
                    dash_queued_direction = last_direction;
                } else {
                    dash_queued_direction = new Vector2(_h, _v);
                }
                dash_queued_direction.Normalize();
                dash_queued = true;
            } else if (dash_active && _dash != 0) {
                if (_h == 0 && _v == 0) {
                    dash_queued_direction = last_direction;
                } else {
                    dash_queued_direction = new Vector2(_h, _v);
                }
                dash_queued_direction.Normalize();
                dash_queued = true;
            }
            
            #region Arrow Handling
            //player has charged past the threshold and released the trigger
            if (arrow_charge_elapsed >= arrow_charge_threshold && _fire == 0 && arrow_charge > 0) {
                float speed_multiplier = 1f;
                if (power_shot) { speed_multiplier = 2f; }
                //fire the arrow
                charging_arrow = false;
                if (arrow != null) {
                    Arrow a = (Arrow)arrow;
                    a.set_trail_color(Color.Black);
                    a.set_tex_color(Color.Black);
                    a.fire_arrow(aim_orbit - draw_position, speed_multiplier, power_shot);
                    world.add_projectile(arrow);
                    this.arrow = null;
                    arrow_charge--;
                }
            } else if (charging_arrow && arrow == null) {
                arrow = new Arrow(get_base_position(), 1f, Constant.arrow_tex, 16, draw_position, hitbox_dir);
                ((Arrow)arrow).set_trail_color(Color.Black);
                ((Arrow)arrow).set_tex_color(Color.Black);
            }
            if (arrow != null && !aiming) { //if the arrow is out and we're not aiming, remove the arrow
                arrow = null;
            }
            if (arrow != null) {
                Arrow a = (Arrow)arrow;
                a.update_aim(aim_orbit, aim_orbit - draw_position);
                arrow.Update(gameTime);
            }
            if (_aim > 0 && arrow_charge > 0 && bow_attribute) {
                aiming = true;
            } else {
                aiming = false;
            }
            if (aiming && _fire > 0 && arrow_charge > 0 && bow_attribute) {
                charging_arrow = true;
                arrow_charge_elapsed += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
                power_shot_elapsed += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
            } else {
                charging_arrow = false;
                arrow_charge_elapsed = 0;
                power_shot_elapsed = 0;
            }
            if (power_shot_elapsed >= power_shot_start_threshold && power_shot_elapsed <= power_shot_duration) {
                power_shot = true;
                power_shot_color = Color.Red;
            } else {
                power_shot = false;
                power_shot_color = Color.White;
            }
            //play sfx
            if (arrow_charge_elapsed >= 10f && arrow_charge_elapsed <= 20f) {
                //play bow drawn (bow up aim) sfx
                world.play_spatial_sfx(Constant.bow_up_aim_sfx, depth_sort_position, 0.5f, world.get_render_distance());
            }
            #endregion

            if (_select == 1 && grenade_charge > 0 && grenade_cooldown_elapsed >= Constant.player_grenade_cooldown) {
                grenade_cooldown_elapsed = 0f;
                grenade_charge--;
                Grenade g = new Grenade(get_base_position(), 1f, Constant.grenade_tex, 16, draw_position, hitbox_dir);
                g.fire_grenade(aim_orbit - draw_position, 1f);
                world.add_projectile(g);
            }

            if (current_gamepad_state.Triggers.Right == 0f && previous_gamepad_state.Triggers.Right != 0f && attack_charge > 1) {
                //trigger heavy attack
                if (attack_charged_elapsed >= 100f) {
                    if (sweet_spot) {
                        //add particle flair if player hits a sweet spot
                        particle_systems.Add(new ParticleSystem(true, Constant.rotate_point(draw_position, rotation, 1f, Constant.direction_up), 2, 800f, 1, 5, 1, 3, Constant.red_particles, new List<Texture2D>() { Constant.footprint_tex }));
                    }
                    heavy_attack_active = true;
                    attack_cooldown_elapsed = 0;
                    attack_charge -= 2;
                    if (attack_charge <= 0) {
                        attack_cooldown = doubleattack_cooldown;
                    }
                    heavy_attack_animation.set_elapsed(0);
                    heavy_attack_animation.set_frame(0);
                }
            }

            if (heavy_attack_active && attack_cooldown_elapsed >= attack_duration) {
                heavy_attack_active = false;
                hitbox_deactivate_elapsed = 0;
            }

            //handle inputs for heavy attacks -> check if attack is held down for any amount of time and set animation
            if (_heavy_attack != 0 && previous_gamepad_state.Triggers.Right != 0f && attack_charge > 1 && sword_attribute) {
                //player has held down the button for at least 2 frames
                //begin charging attack
                charging_active = true;
                attack_charged_elapsed += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
                sweet_spot_elapsed += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
            } else {
                //otherwise set attack_charged_elapsed = 0 and sweet_spot_elapsed = 0 because the player is not holding down the button
                charging_active = false;
                attack_charged_elapsed = 0;
                sweet_spot_elapsed = 0;
                sweet_spot = false;
            }

            if (sweet_spot_elapsed >= sweet_spot_start_duration && sweet_spot_elapsed <= sweet_spot_duration) {
                sweet_spot = true;
                sweet_spot_color = Color.Red;
            } else {
                sweet_spot = false;
                sweet_spot_color = Color.White;
            }

            //check for dash input for extra long dash
            if (sweet_spot) {
                if (_dash != 0) {
                    //extra long dash
                }
            }

            //set dash to active
            //meaning: if the player has pressed dash, and the dash currently is not active and if the last direction they held is not zero vector and the time since last dash is greater than the cooldown then dash and they have the dash attribute active
            if (_dash != 0 && !dash_active && last_direction != Vector2.Zero && dash_charge > 0 && !attack_active && !heavy_attack_active && dash_attribute) {
                //set dash to active
                dash_active = true;
                //lock dash direction and start position
                if (dash_queued) {
                    if (dash_queued_direction.X == float.NaN || dash_queued_direction.Y == float.NaN) {
                        dash_direction_unit.X = last_direction.X;
                        dash_direction_unit.Y = last_direction.Y;    
                    } else {
                        dash_direction_unit.X = dash_queued_direction.X;
                        dash_direction_unit.Y = dash_queued_direction.Y;
                    }
                } else {
                    dash_direction_unit.X = last_direction.X;
                    dash_direction_unit.Y = last_direction.Y;
                }
                dash_start = base_position;
                //reset dash cooldown timer to 0
                dash_cooldown_elapsed = 0;
                //subtract one from dash charge
                dash_charge--;
                //increase dash cool down on doubledash and replenish the charge
                if (dash_charge <= 0) {
                    //increase dash_cooldown
                    dash_cooldown = doubledash_cool_down;
                }
                //add particle system on dash
                particle_systems.Add(new ParticleSystem(true, Constant.rotate_point(draw_position, rotation, 1f, Constant.direction_up), 2, 800f, 1, 5, 1, 3, Constant.green_particles, new List<Texture2D>() { Constant.footprint_tex }));
                //play dash sound
                world.play_spatial_sfx(Constant.dash_sfx, depth_sort_position, ((float)random.Next(-1, 2))/2f, world.get_render_distance());
            } else if (dash_cooldown_elapsed >= dash_cooldown) { //reset dash_charge if the player has not dashed in the required cooldown
                dash_charge = get_max_dashes();
                dash_cooldown = Constant.player_dash_cooldown;
            }

            //attack code
            if (_attack != 0 && !attack_active && attack_charge > 0 && sword_attribute) {
                //play dash sound dependent on swing
                if (attack_charge > 1) {
                    world.play_spatial_sfx(Constant.sword_slash_sfx, depth_sort_position, 0.75f, world.get_render_distance(), -0.5f);
                } else {
                    world.play_spatial_sfx(Constant.sword_slash_sfx, depth_sort_position, 1f, world.get_render_distance(), -0.5f);
                }
                //set attack to active
                attack_active = true;
                attack_cooldown_elapsed = 0;
                attack_charge--;
                if (attack_charge <= 0) {
                    attack_cooldown = doubleattack_cooldown;
                }
                //should the player hit the attack button again, don't finish the aniamtion -> interrupt and start it again
                //sets the elapsed time and the current frame back to 0 to restart the animation
                attack_animation.set_elapsed(0);
                attack_animation.set_frame(0);
            } else if (attack_cooldown_elapsed >= attack_cooldown) {
                attack_charge = get_max_attack_charges();
                attack_cooldown = Constant.player_attack_cooldown;
            }
            
            if (attack_active && attack_cooldown_elapsed >= attack_duration) {
                attack_active = false;
                hitbox_deactivate_elapsed = 0;
            }
            
            //hitbox timer
            if (heavy_attack_active || attack_active) {
                hitbox_deactivate_elapsed += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
            }
            
            #region Movement
            if (!movement_disabled && !hitstun_active) {
                //check for future collision
                (List<IEntity>, bool) fcdx = future_collision_detected_x();
                (List<IEntity>, bool) fcdy = future_collision_detected_y();

                //normal movement code
                if (!dash_active && !attack_active && !heavy_attack_active && !charging_active && !charging_arrow && !aiming) {
                    /*Update player position*/
                    if (_v == 0 && _h == 0) {
                        //zero out direction
                        direction = Vector2.Zero;
                        moving = false;
                    } else {
                        moving = true;
                        direction = new Vector2(_h, _v);
                        last_direction.X = direction.X;
                        last_direction.Y = direction.Y;
                    }

                    if (direction != Vector2.Zero) { direction.Normalize(); }

                    //separate direction components for applying movement to achieve sliding along walls for collision
                    if (!fcdy.Item2) {
                        if (_v != 0) {
                            //apply vertical
                            apply_movement(direction, false);
                        }
                    }

                    if (!fcdx.Item2) {
                        if (_h != 0) {
                            //apply horizontal
                            apply_movement(direction, true);
                        }
                    }
                    //change depth sort position based on draw position regardless of camera rotation
                    depth_sort_position = draw_position + (player_size/2) * new Vector2(direction_down.X * (float)Math.Cos(-rotation) - direction_down.Y * (float)Math.Sin(-rotation), direction_down.Y * (float)Math.Cos(-rotation) + direction_down.X * (float)Math.Sin(-rotation));
                    //update attack draw_position to always stay at the same spot rotating the player in the Vector2(-1, -1) direction (example:See RRect.cs update function)
                    attack_draw_position = draw_position + (player_size/2) * new Vector2(-1 * (float)Math.Cos(-rotation) - (-1) * (float)Math.Sin(-rotation), -1 * (float)Math.Cos(-rotation) + (-1) * (float)Math.Sin(-rotation));
                } else if (dash_active) { //dash movement code
                    //normalize dash direction vector to ensure no slow dashes
                    dash_direction_unit = Vector2.Normalize(dash_direction_unit);
                    //rotate dash_direction_unit based on rotation
                    float x = dash_direction_unit.X * (float)Math.Cos(-rotation) - dash_direction_unit.Y * (float)Math.Sin(-rotation);
                    float y = dash_direction_unit.Y * (float)Math.Cos(-rotation) + dash_direction_unit.X * (float)Math.Sin(-rotation);

                    dash_direction = new Vector2(x, y);
                    dash_direction.Normalize();

                    //transform player position
                    if (!fcdx.Item2 && !fcdy.Item2) {
                        base_position += dash_direction * dash_speed;
                        draw_position += dash_direction * dash_speed;
                        attack_draw_position += dash_direction * dash_speed;
                    } else {
                        //future collision detected
                        //stop dashing
                        //zero out all dash movement variables added to position variables
                        dash_active = false;
                        dash_direction_unit = Vector2.Zero;
                        dash_direction = Vector2.Zero;
                        dash_queued = false;
                    }

                    if (Vector2.Distance(dash_start, base_position) > dash_length) {
                        dash_active = false;
                    }
                    //empty out queued dash direction vars
                    dash_queued = false;
                    if (_dash != 0) {
                        //ensure that not having input and just holding down the button uses the last direction instead, otherwise it nulls out the player position
                        if (_h == 0 && _v == 0) {
                            dash_queued_direction = last_direction;
                        } else {
                            dash_queued_direction = new Vector2(_h, _v);
                        }
                        dash_queued_direction.Normalize();
                        dash_queued = true;
                    }
                } else if (attack_active || heavy_attack_active) {
                    //normalize direction vector
                    Vector2 dd_unit = new Vector2(last_direction.X, last_direction.Y);
                    dd_unit = Vector2.Normalize(dd_unit);
                    //rotate dash_direction_unit based on rotation
                    float x = dd_unit.X * (float)Math.Cos(-rotation) - dd_unit.Y * (float)Math.Sin(-rotation);
                    float y = dd_unit.Y * (float)Math.Cos(-rotation) + dd_unit.X * (float)Math.Sin(-rotation);
                    dash_direction = new Vector2(x, y);

                    //transform player position
                    float attack_speed_multiplier = Constant.player_attack_movement_speed;
                    if (fcdx.Item2 && fcdy.Item2) {
                        attack_speed_multiplier = 0f;
                    }
                    //transform positions with correct speed based on collision scenario
                    base_position += dash_direction * attack_speed_multiplier;
                    draw_position += dash_direction * attack_speed_multiplier;
                    attack_draw_position += dash_direction * attack_speed_multiplier;
                }
            }
            #endregion

            /*ENEMY HIT COLLISION CODE*/
            receive_damage(gameTime);


            //add to dash, attack, grenade cooldown
            dash_cooldown_elapsed += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
            attack_cooldown_elapsed += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
            grenade_cooldown_elapsed += (float)gameTime.ElapsedGameTime.TotalMilliseconds;

            /*PARTICLE SYSTEMS CODE*/
            update_particle_systems(gameTime, rotation);

            //update animations for the player
            if (!movement_disabled){
                update_animation(gameTime);
            } else {
                //if player has movement disabled, only update idle animation
                idle_animation.Update(gameTime);
            }

            //update collision and hit/hurt boxes
            hurtbox.update(rotation, draw_position);
            //if the attack is active lock the hurtbox from moving
            update_hitbox_position(gameTime, _v, _h);
            hitbox.update(rotation, hitbox_center);
            future_hurtbox_x = get_future_hurtbox_x();
            future_hurtbox_y = get_future_hurtbox_y();
            shadowcast_box.update(rotation, depth_sort_position);

            //update emotion state
            update_emotion_state(gameTime);

            //update temp tile movement speed
            update_temp_tile_movement_speed();

            //set camera track position
            if (camera_tracking_X) {
                camera_track_position.X = base_position.X;
            }
            if (camera_tracking_Y) {
                camera_track_position.Y = base_position.Y;
            }
            
            //set previous state of keyboard and gamepad
            previous_keyboard_state = current_keyboard_state;
            previous_gamepad_state = current_gamepad_state;

            this.center_position = Constant.rotate_point(depth_sort_position, rotation, 16f, Constant.direction_up);
            this.emotion_display_position = Constant.rotate_point(draw_position, rotation, -(player_size/2), Constant.direction_down);

            Constant.profiler.end("player_update");
        }

        private void apply_movement(Vector2 direction, bool x) {
            if (x) {
                //alter the x position based on direction and movement speed
                Vector2 move_dir_x = new Vector2(direction.X, 0);

                base_position += move_dir_x * movement_speed;
                draw_position += move_dir_x * movement_speed;
            } else {
                //alter the y position based on direction and movement speed
                Vector2 move_dir_y = new Vector2(0, direction.Y);

                base_position += move_dir_y * movement_speed;
                draw_position += move_dir_y * movement_speed;
            }
        }

        public void update_particle_systems(GameTime gameTime, float rotation) {
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

        public void update_emotion_state(GameTime gameTime) {
            //handle emotion reset + cooldown
            emotion_elapsed += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
            if (emotion_elapsed >= emotion_elapsed_threshold && current_emotion != Emotion.Calm) {
                set_emotion(Emotion.Calm);
            }
        }

        public void set_emotion(Emotion e) {
            //set current emotion and reset cooldown
            current_emotion = e;
            emotion_elapsed = 0f;

            //handle setting emotion behavior
            switch(current_emotion) {
                case Emotion.Fear:
                    //fear reduces movement speed for all actions (minus attacking)
                    movement_speed = fear_movement_speed;
                    dash_speed = Constant.player_dash_speed/2;
                    dash_length = Constant.player_dash_length/2;
                    break;
                case Emotion.Anxiety:
                    //anxiety reduces charges for player resources temporarily?
                    dash_charge = dash_charge == 0 ? 0 : Constant.player_dash_charge/2;
                    attack_charge = attack_charge == 0 ? 0 : Constant.player_attack_charge/2;
                    arrow_charge = arrow_charge == 0 ? 0 : Constant.player_arrow_charge/2;
                    break;
                case Emotion.Calm:
                    //calm is defaults
                    movement_speed = base_movement_speed;
                    dash_speed = Constant.player_dash_speed;
                    dash_length = Constant.player_dash_length;
                    dash_charge = Constant.player_dash_charge;
                    attack_charge = Constant.player_attack_charge;
                    arrow_charge = Constant.player_arrow_charge;
                    break;
                default:
                    break;
            }
        }

        public void receive_damage(GameTime gameTime) {
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
                //draw_position += hit_direction * hit_speed;
                //attack_draw_position = draw_position + (player_size/2) * new Vector2(-1 * (float)Math.Cos(-rotation) - (-1) * (float)Math.Sin(-rotation), -1 * (float)Math.Cos(-rotation) + (-1) * (float)Math.Sin(-rotation));

                base_position += hit_direction * hit_speed;
                draw_position += hit_direction * hit_speed;
                attack_draw_position += hit_direction * hit_speed;

                //reset all animations
                attack_animation.reset_animation();
                walk_animation.reset_animation();
                idle_animation.reset_animation();
            }
            if (take_hit_elapsed >= reactivate_hurtbox_threshold) {
                //elapsed
                hurtbox_active = true;
                take_hit_elapsed = 0;
                hitstun_active = false;
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

        public void take_hit(IEntity entity, int damage) {
            //set hitstun
            hitstun_active = true;
            //handle emotion transfer
            if (entity is IAiEntity) {
                IAiEntity ai = (IAiEntity)entity;
                Emotion emotion_trait = ai.get_emotion_trait();
                if (emotion_trait != Emotion.Calm) {
                    //set player emtion trait
                    set_emotion(emotion_trait);
                }
            }
            //reduce health on hit
            health -= damage;
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

        public void update_animation(GameTime gameTime) {
            /*NOTE: we check !attack !heavy !charging because we don't want to update the direction once a player has hit the button.*/
            /*Play the animation all the way through without swapping directions even though we already know it has change*/

            if (_v < 0){ //up
                if (_h > 0) { //up right
                    walk_animation.set_y_offset((int)player_size*6);
                    if (!dash_active)
                        dash_animation.set_y_offset((int)player_size*7);
                    //up attack animation
                    if (!attack_active) {
                        if (attack_charge == 1) { attack_animation.set_y_offset((int)player_size*2*12); }
                        else { attack_animation.set_y_offset((int)player_size*2*14); }
                    }
                    //up heavy attack animation
                    if (!heavy_attack_active)
                        heavy_attack_animation.set_y_offset((int)player_size*2*0);
                    //up charging animation rect
                    if (!charging_active)
                        charging_animation_rect.Y = (int)player_size*2*0;
                    aiming_animation_rect.Y = (int)player_size*6;
                } else if (_h < 0) { //up left
                    walk_animation.set_y_offset((int)player_size*7);
                    if (!dash_active)
                        dash_animation.set_y_offset((int)player_size*6);
                    //up attack animation
                    if (!attack_active) {
                        if (attack_charge == 1) { attack_animation.set_y_offset((int)player_size*2*13); }
                        else { attack_animation.set_y_offset((int)player_size*2*15); }
                    }
                    //up heavy attack animation
                    if (!heavy_attack_active)
                        heavy_attack_animation.set_y_offset((int)player_size*2*1);
                    //up charging animation rect
                    if (!charging_active)
                        charging_animation_rect.Y = (int)player_size*2*1;
                    aiming_animation_rect.Y = (int)player_size*7;
                } else { //directly up
                    //set y offset to third row
                    walk_animation.set_y_offset((int)player_size*3);
                    if (!dash_active)
                        dash_animation.set_y_offset((int)player_size*3);
                    //up attack animation
                    if (!attack_active) {
                        if (attack_charge == 1) { attack_animation.set_y_offset((int)player_size*2*5); }
                        else { attack_animation.set_y_offset((int)player_size*2*7); }
                    }
                    //up heavy attack animation
                    if (!heavy_attack_active)
                        heavy_attack_animation.set_y_offset((int)player_size*2*3);
                    //up charging animation rect
                    if (!charging_active)
                        charging_animation_rect.Y = (int)player_size*2*3;
                    aiming_animation_rect.Y = (int)player_size*3;
                }
            } else if (_v > 0) { //down
                if (_h > 0) { //down to the right
                    //set y offset to second row
                    walk_animation.set_y_offset((int)player_size*5);
                    if (!dash_active)
                        dash_animation.set_y_offset((int)player_size*5);
                    //attack animation
                    if (!attack_active) {
                        if (attack_charge == 1) { attack_animation.set_y_offset((int)player_size*2*9); }
                        else { attack_animation.set_y_offset((int)player_size*2*11); }
                    }
                    //heavy attack animation
                    if (!heavy_attack_active)
                        heavy_attack_animation.set_y_offset((int)player_size*2*0);
                    //charging animation rect
                    if (!charging_active)
                        charging_animation_rect.Y = (int)player_size*2*0;
                    aiming_animation_rect.Y = (int)player_size*5;
                } else if (_h < 0){ // down to the left
                    //set y offset to first row
                    walk_animation.set_y_offset((int)player_size*4);
                    if (!dash_active)
                        dash_animation.set_y_offset((int)player_size*4);
                    //attack animation
                    if (!attack_active) {
                        if (attack_charge == 1) { attack_animation.set_y_offset((int)player_size*2*8); }
                        else { attack_animation.set_y_offset((int)player_size*2*10); }
                    }
                    //heavy attack animation
                    if (!heavy_attack_active)
                        heavy_attack_animation.set_y_offset((int)player_size*2*1);
                    //charging animation rect
                    if (!charging_active)
                        charging_animation_rect.Y = (int)player_size*2*1;
                    aiming_animation_rect.Y = (int)player_size*4;
                } else { //directly down
                    //set y offset to first row
                    walk_animation.set_y_offset((int)player_size*2);
                    if (!dash_active)
                        dash_animation.set_y_offset((int)player_size*2);
                    //down attack animation
                    if (!attack_active) {
                        if (attack_charge == 1) { attack_animation.set_y_offset((int)player_size*2*4); }
                        else { attack_animation.set_y_offset((int)player_size*2*6); }
                    }
                    //down heavy attack animation
                    if (!heavy_attack_active)
                        heavy_attack_animation.set_y_offset((int)player_size*2*2);
                    //down charging animation rect
                    if (!charging_active)
                        charging_animation_rect.Y = (int)player_size*2*2;
                    aiming_animation_rect.Y = (int)player_size*2;
                }
            } else if (_h < 0) { //directly left
                //set y offset to first row
                walk_animation.set_y_offset(0);
                if (!dash_active)
                    dash_animation.set_y_offset(0);
                //attack animation
                if (!attack_active) {
                    if (attack_charge == 1) { attack_animation.set_y_offset((int)player_size*2*1); }
                    else { attack_animation.set_y_offset((int)player_size*2*3); }
                }
                //heavy attack animation
                if (!heavy_attack_active)
                    heavy_attack_animation.set_y_offset((int)player_size*2*1);
                //charging animation rect
                if (!charging_active)
                    charging_animation_rect.Y = (int)player_size*2*1;
                aiming_animation_rect.Y = (int)player_size*0;
            } else if (_h > 0){ //directly right
                //set y offset to second row
                walk_animation.set_y_offset((int)player_size*1);
                if (!dash_active)
                    dash_animation.set_y_offset((int)player_size*1);
                //attack animation
                if (!attack_active) {
                    if (attack_charge == 1) { attack_animation.set_y_offset((int)player_size*2*0); }
                    else { attack_animation.set_y_offset((int)player_size*2*2); }
                }
                //heavy attack animation
                if (!heavy_attack_active)
                    heavy_attack_animation.set_y_offset((int)player_size*2*0);
                //charging animation rect
                if (!charging_active)
                    charging_animation_rect.Y = (int)player_size*2*0;
                aiming_animation_rect.Y = (int)player_size*1;
            } else { //stationary
                // player is doing nothing - handle idle animation
                //draw first frame based off last direction
                float lh = last_direction.X;
                float lv = last_direction.Y;

                if (lv < 0){ //up
                    if (lh > 0) { //up right
                        idle_animation.set_y_offset((int)player_size*14);
                        dash_animation.set_y_offset((int)player_size*7);
                        //up attack animation
                        if (attack_charge == 1) { attack_animation.set_y_offset((int)player_size*2*12); }
                        else { attack_animation.set_y_offset((int)player_size*2*14); }
                        //up heavy attack animation
                        heavy_attack_animation.set_y_offset((int)player_size*2*0);
                        //up charging animation rect
                        charging_animation_rect.Y = (int)player_size*2*0;
                        aiming_animation_rect.Y = (int)player_size*6;
                    } else if (lh < 0) { //up left
                        idle_animation.set_y_offset((int)player_size*15);
                        dash_animation.set_y_offset((int)player_size*6);
                        //up attack animation
                        if (attack_charge == 1) { attack_animation.set_y_offset((int)player_size*2*13); }
                        else { attack_animation.set_y_offset((int)player_size*2*15); }
                        //up heavy attack animation
                        heavy_attack_animation.set_y_offset((int)player_size*2*1);
                        //up charging animation rect
                        charging_animation_rect.Y = (int)player_size*2*1;
                        aiming_animation_rect.Y = (int)player_size*7;
                    } else { //directly up
                        idle_animation.set_y_offset((int)player_size*11);
                        dash_animation.set_y_offset((int)player_size*3);
                        //up attack animation
                        if (attack_charge == 1) { attack_animation.set_y_offset((int)player_size*2*5); }
                        else { attack_animation.set_y_offset((int)player_size*2*7); }
                        //up heavy attack animation
                        heavy_attack_animation.set_y_offset((int)player_size*2*3);
                        //up charging animation rect
                        charging_animation_rect.Y = (int)player_size*2*3;
                        aiming_animation_rect.Y = (int)player_size*3;
                    }
                } else if (lv > 0){ //down
                    if (lh > 0){ //down right
                        idle_animation.set_y_offset((int)player_size*13);
                        dash_animation.set_y_offset((int)player_size*5);
                        //up attack animation
                        if (attack_charge == 1) { attack_animation.set_y_offset((int)player_size*2*9); }
                        else { attack_animation.set_y_offset((int)player_size*2*11); }
                        //down heavy attack animation
                        heavy_attack_animation.set_y_offset((int)player_size*2*0);
                        //down charging animation rect
                        charging_animation_rect.Y = (int)player_size*2*0;
                        aiming_animation_rect.Y = (int)player_size*5;
                    } else if (lh < 0){ //down left
                        idle_animation.set_y_offset((int)player_size*12);
                        dash_animation.set_y_offset((int)player_size*4);
                        //down attack animation
                        if (attack_charge == 1) { attack_animation.set_y_offset((int)player_size*2*8); }
                        else { attack_animation.set_y_offset((int)player_size*2*10); }
                        //down heavy attack animation
                        heavy_attack_animation.set_y_offset((int)player_size*2*1);
                        //down charging animation rect
                        charging_animation_rect.Y = (int)player_size*2*1;
                        aiming_animation_rect.Y = (int)player_size*4;
                    } else { //directly down
                        idle_animation.set_y_offset((int)player_size*10);
                        dash_animation.set_y_offset((int)player_size*2);
                        //down attack animation
                        if (attack_charge == 1) { attack_animation.set_y_offset((int)player_size*2*4); }
                        else { attack_animation.set_y_offset((int)player_size*2*6); }
                        //down heavy attack animation
                        heavy_attack_animation.set_y_offset((int)player_size*2*2);
                        //down charging animation rect
                        charging_animation_rect.Y = (int)player_size*2*2;
                        aiming_animation_rect.Y = (int)player_size*2;
                    }
                } else if (lh < 0) { //directly left
                    idle_animation.set_y_offset((int)player_size*8);
                    dash_animation.set_y_offset((int)player_size*0);
                    //attack animation
                    if (attack_charge == 1) { attack_animation.set_y_offset((int)player_size*2*1); }
                    else { attack_animation.set_y_offset((int)player_size*2*3); }
                    //left heavy attack animation
                    heavy_attack_animation.set_y_offset((int)player_size*2*1);
                    //left charging animation rect
                    charging_animation_rect.Y = (int)player_size*2*1;
                    aiming_animation_rect.Y = (int)player_size*0;
                } else if (lh > 0) { //directly right
                    idle_animation.set_y_offset((int)player_size*9);
                    dash_animation.set_y_offset((int)player_size*1);
                    //attack animation
                    if (attack_charge == 1) { attack_animation.set_y_offset((int)player_size*2*0); }
                    else { attack_animation.set_y_offset((int)player_size*2*2); }
                    //left heavy attack animation
                    heavy_attack_animation.set_y_offset((int)player_size*2*0);
                    //left charging animation rect
                    charging_animation_rect.Y = (int)player_size*2*0;
                    aiming_animation_rect.Y = (int)player_size*1;
                } else {
                    //do nothing (maybe panic?)
                    // should not get here
                    idle_animation.set_y_offset((int)player_size*8);
                    dash_animation.set_y_offset((int)player_size*0);
                    //up attack animation
                    if (attack_charge == 1) { attack_animation.set_y_offset((int)player_size*2*0); }
                    else { attack_animation.set_y_offset((int)player_size*2*2); }
                    //right heavy attack animation
                    heavy_attack_animation.set_y_offset((int)player_size*2*0);
                    //right charging animation rect
                    charging_animation_rect.Y = (int)player_size*2*0;
                    aiming_animation_rect.Y = (int)player_size*0;
                }
            }

            //if player is dashing then update dash animation
            if (is_dashing()) {
                dash_animation.Update(gameTime);
            }

            //update attack animation
            if (attack_active) {
                attack_animation.Update(gameTime);
            }

            if (heavy_attack_active) {
                heavy_attack_animation.Update(gameTime);
            }

            if (_v != 0 || _h != 0) { //if player is moving update animation and add footprints
                walk_animation.Update(gameTime);
                if (!aiming) {
                    add_footprints(gameTime);
                }
            } else {
                idle_animation.Update(gameTime);
            }

            //update footprints and reap footprints
            foreach (Footprints f in footprints) {
                f.Update(gameTime);
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
        }

        private void add_footprints(GameTime gameTime){
            footprints_elapsed += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
            //check to add footprints
            if (footprints_elapsed >= footprints_delay) {
                //create new footprint and set elapsed back to 0
                footprints.Add(new Footprints(depth_sort_position, 1f, Constant.footprint_tex, rotation, 0.5f, 0.01f, Color.Black));
                //add particle system for puff
                particle_systems.Add(new ParticleSystem(true, Constant.rotate_point(draw_position, rotation, 2f, Constant.direction_down), 1, 500f, 1, random.Next(1, 4), 1, 3, Constant.white_particles, new List<Texture2D>() { Constant.footprint_tex }));
                footprint_sound++;
                footprints_elapsed = 0;
            }

            if (footprint_sound >= 2 && !is_dashing()) {
                footprint_sound = 0;
                //add sound for footprints
                world.play_spatial_sfx(Constant.footstep_sfx, depth_sort_position, ((float)random.Next(-2, 1))/4f, world.get_render_distance(), -0.45f);
            }
        }

        private void update_hitbox_position(GameTime gameTime, float v, float h) {
            Vector2 hitbox_direction = Vector2.Zero;

            //should not be able to update the hitbox position if the character is already attacking
            if (hitbox_active()) {
                return;
            }

            float lh = last_direction.X, lv = last_direction.Y;

            if (_v < 0){ //up
                if (_h > 0) { //up right
                    hitbox_direction.X = 1;
                    hitbox_direction.Y = -1;
                } else if (_h < 0) { //up left
                    hitbox_direction.X = -1;
                    hitbox_direction.Y = -1;
                } else { //directly up
                    hitbox_direction.X = 0;
                    hitbox_direction.Y = -1;
                }
            } else if (_v > 0) { //down
                if (_h > 0) { //down right
                    hitbox_direction.X = 1;
                    hitbox_direction.Y = 1;
                } else if (_h < 0) { //down left
                    hitbox_direction.X = -1;
                    hitbox_direction.Y = 1;
                } else { //directly down
                    hitbox_direction.X = 0;
                    hitbox_direction.Y = 1;
                } 
            } else if (_h < 0) { // directly left
                hitbox_direction.Y = 0;
                hitbox_direction.X = -1;
            } else if (_h > 0){ //directly right
                hitbox_direction.X = 1;
                hitbox_direction.Y = 0;
            } else { //stationary
                // do nothing
                //draw first frame based off last direction
                if (lv < 0){ //up
                    if (lh > 0) { //up right
                        hitbox_direction.X = 1;
                        hitbox_direction.Y = -1;
                    } else if (lh < 0) { //up left
                        hitbox_direction.X = -1;
                        hitbox_direction.Y = -1;
                    } else { //directly up
                        hitbox_direction.X = 0;
                        hitbox_direction.Y = -1;
                    }
                } else if (lv > 0){ //down
                    if (_h > 0) { //down right
                        hitbox_direction.X = 1;
                        hitbox_direction.Y = 1;
                    } else if (_h < 0) { //down left
                        hitbox_direction.X = -1;
                        hitbox_direction.Y = 1;
                    } else { //directly down
                        hitbox_direction.X = 0;
                        hitbox_direction.Y = 1;
                    }
                } else if (lh < 0) { //directly left
                    hitbox_direction.Y = 0;
                    hitbox_direction.X = -1;
                } else if (lh > 0) { //directly right
                    hitbox_direction.X = 1;
                    hitbox_direction.Y = 0;
                } else {
                    //do nothing
                    // should not get here
                }
            }

            if (dash_active) {
                hitbox_center = draw_position + hitbox_center_distance * new Vector2(dash_direction.X * (float)Math.Cos(-rotation) - dash_direction.Y * (float)Math.Sin(-rotation), dash_direction.Y * (float)Math.Cos(-rotation) + dash_direction.X * (float)Math.Sin(-rotation));
            }

            this.hitbox_dir = hitbox_direction;

            //rotate hitbox center around draw_position
            hitbox_center = draw_position + hitbox_center_distance * new Vector2(hitbox_direction.X * (float)Math.Cos(-rotation) - hitbox_direction.Y * (float)Math.Sin(-rotation), hitbox_direction.Y * (float)Math.Cos(-rotation) + hitbox_direction.X * (float)Math.Sin(-rotation));
            if (_h == 0 && _v == 0) {
                aim_orbit = draw_position + hitbox_center_distance * new Vector2(lh * (float)Math.Cos(-rotation) - lv * (float)Math.Sin(-rotation), lv * (float)Math.Cos(-rotation) + lh * (float)Math.Sin(-rotation));
            } else {
                aim_orbit = draw_position + hitbox_center_distance * new Vector2(_h * (float)Math.Cos(-rotation) - _v * (float)Math.Sin(-rotation), _v * (float)Math.Cos(-rotation) + _h * (float)Math.Sin(-rotation));
            }
            //rotate hitbox_draw_position around hitbox center
            hitbox_draw_position = Constant.rotate_point(hitbox_center, rotation, 1f, new Vector2(-player_size/2, -player_size/2));
        }
        
        public void add_arrow(IEntity arrow, World world) {
            world.add_projectile(arrow);
        }

        public void resolve_collision_geometry_movement(Vector2 current_direction, ICollisionEntity entity) {
            //handle player dashing into collision geometry
            if (is_dashing()) {
                //dash direction is used for dashing (tiny dash / player movement on attacking - like lunging)
                //stop dashing
                //zero out all dash movement variables added to position variables
                dash_active = false;
                dash_direction_unit = Vector2.Zero;
                dash_direction = Vector2.Zero;
                dash_queued = false;
            }
            //resolve collision movement
            //center of collision entity hurtbox
            Vector2 center = entity.get_hurtbox().position;
            //base position for player
            Vector2 base_pos = get_base_position();
            //calculate direction between the two
            Vector2 center_to_player = base_pos - center;
            //normalize
            center_to_player.Normalize();
            //move player back along vector exact amount they're trying to move
            float speed_multiplier = movement_speed;
            if (is_attacking()) { //modify speed depending on whether or not player is attacking
                speed_multiplier = Constant.player_attack_movement_speed;
            }
            base_position += center_to_player * speed_multiplier;
            draw_position += center_to_player * speed_multiplier;
            attack_draw_position += center_to_player * speed_multiplier;
        }

        public RRect get_future_hurtbox() {
            Vector2 draw = draw_position + direction * movement_speed*3f;
            
            future_hurtbox = new RRect(draw, player_size/2, player_size);
            future_hurtbox.update(rotation, draw);
            return future_hurtbox;
        }

        public RRect get_future_hurtbox_x() {
            Vector2 draw = draw_position + new Vector2(direction.X, 0) * movement_speed*3f;

            future_hurtbox_x = new RRect(draw, player_size/2, player_size);
            future_hurtbox_x.update(0f, draw);
            return future_hurtbox_x;
        }

        public RRect get_future_hurtbox_y() {
            Vector2 draw = draw_position + new Vector2(0, direction.Y) * movement_speed*3f;

            future_hurtbox_y = new RRect(draw, player_size/2, player_size);
            future_hurtbox_y.update(0f, draw);
            return future_hurtbox_y;
        }

        public (List<IEntity>, bool) future_collision_detected_x() {
            List<IEntity> collisions = new List<IEntity>();
            bool collision = false;
            foreach (KeyValuePair<IEntity, bool> kv in collision_geometry_map_x) {
                if (kv.Value) {
                    collisions.Add(kv.Key);
                    collision = kv.Value;
                }
            }
            return (collisions, collision);
        }

        public (List<IEntity>, bool) future_collision_detected_y() {
            List<IEntity> collisions = new List<IEntity>();
            bool collision = false;
            foreach (KeyValuePair<IEntity, bool> kv in collision_geometry_map_y) {
                if (kv.Value) {
                    collisions.Add(kv.Key);
                    collision = kv.Value;
                }
            }
            return (collisions, collision);
        }

        public void update_temp_tile_movement_speed() {
            bool slow_movement = false;
            foreach (KeyValuePair<IEntity, bool> kv in collision_tile_map) {
                if (kv.Value) {
                    slow_movement = true;
                    break;
                }
            }
            
            if (slow_movement) {
                movement_speed = fear_movement_speed;
            } else {
                //no collisions mean regular movement speed
                movement_speed = base_movement_speed;
            }
        }

        public Vector2 calculate_resultant_vector(List<IEntity> collision_entities, Vector2 direction) {
            List<Vector2> resultants = new List<Vector2>();
            foreach (IEntity e in collision_entities) {
                if (e is ICollisionEntity) {
                    ICollisionEntity ic = (ICollisionEntity)e;
                    //center of collision entity hurtbox
                    Vector2 center = ic.get_hurtbox().position;
                    //base position for player
                    Vector2 base_pos = get_base_position();
                    //calculate direction between the two (end - start, giving the direction to the player from the collision entity)
                    Vector2 center_to_player = base_pos - center;
                    //normalize
                    center_to_player.Normalize();
                    
                    //find the closest edge from the player
                    int closest_edge = ic.get_hurtbox().closest_edge(get_future_hurtbox().position);
                    //use that edge index pulled from the collision entity hitbox to pull the start and end points of that edge
                    KeyValuePair<Vector2, Vector2> edge = ic.get_hurtbox().get_edges()[closest_edge];
                    //given the start end end of that edge, get the edge normal
                    Vector2 edge_normal = ic.get_hurtbox().get_edge_normal(edge.Key, edge.Value);
                    //calculate the resultant direction by combining the edge_normal and the players current direction
                    Vector2 addition = edge_normal + direction;
                    //normalize and return (if not zero -> normalizing a zero vector yields (NaN, NaN))
                    if (!addition.Equals(Vector2.Zero)) {
                        addition.Normalize();
                    }
                    resultants.Add(addition);
                }
            }
            //calculate average vector from resultants
            Vector2 sum = Vector2.Zero;
            foreach (Vector2 v in resultants) {
                sum += v;
            }
            return sum / resultants.Count;
        }

        public Vector2 calculate_resultant_vector2(List<IEntity> collision_entities, Vector2 direction) {
            List<Vector2> resultants = new List<Vector2>();
            foreach (IEntity e in collision_entities) {
                if (e is ICollisionEntity) {
                    ICollisionEntity ic = (ICollisionEntity)e;
                    resultants.Add(get_hurtbox().calculate_mtv(ic.get_hurtbox()));
                }
            }
            
            //calculate average vector from resultants
            Vector2 sum = Vector2.Zero;
            foreach (Vector2 v in resultants) {
                sum += v;
            }
            return sum / resultants.Count;
        }

        public void set_collision_geometry_map(Dictionary<IEntity, bool> geometry_map_x, Dictionary<IEntity, bool> geometry_map_y, Dictionary<IEntity, bool> tile_map) {
            this.collision_geometry_map_x = geometry_map_x;
            this.collision_geometry_map_y = geometry_map_y;
            this.collision_tile_map = tile_map;
        }

        public bool check_hurtbox_collisions(RRect collision_rect) {
            return hurtbox.collision(collision_rect);
        }

        public bool check_hitbox_collisions(RRect collision_rect) {
            return hitbox.collision(collision_rect);
        }

        public bool hitbox_active() {
            return (attack_active || heavy_attack_active) && hitbox_deactivate_elapsed <= hitbox_deactivate_threshold;
        }

        public Vector2 get_base_position() {
            return depth_sort_position;
        }

        public Vector2 get_camera_track_position() {
            return camera_track_position;
        }

        public Vector2 get_direction() {
            return direction;
        }

        public void set_obj_ID_num(int id_num) {
            this.ID = id_num;
        }

        public int get_obj_ID_num() {
            return this.ID;
        }

        public string get_id() {
            return "player";
        }

        public int get_health() {
            return health;
        }

        public void set_health(int health_value) {
            health = health_value;
        }

        public bool is_moving(){
            return moving;
        }

        public void set_movement_disabled(bool value) {
            this.movement_disabled = value;
        }

        public bool is_input() {
            //check controls for input
            if (_v != 0 || _h != 0 || _dash != 0 || _attack != 0 || _interact != 0) {
                return true;
            }
            return false;
        }

        public bool is_idle() {
            return idle;
        }

        public void reset_screen_size(float screen_width, float screen_height){
            this.screen_width = screen_width;
            this.screen_height = screen_height;
        }

        public float get_scale() {
            return scale;
        }

        public void set_scale(float scale_value) {
            this.scale = scale_value;
        }

        public string get_flag() {
            return Constant.ENTITY_ACTIVE;
        }

        public void set_camera_tracking_X(bool value) {
            this.camera_tracking_X = value;
        }
        public void set_camera_tracking_Y(bool value) {
            this.camera_tracking_Y = value;
        }
        
        public void set_base_position(Vector2 position) {
            base_position = position;
            draw_position = new Vector2(base_position.X - (player_size / 2), 
                                            base_position.Y - player_size);
        }

        public Vector2 get_player_input_direction() {
            return new Vector2(_h, _v);
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
                object_identifier = "player",
                object_id_num = get_obj_ID_num(),
                x_position = get_base_position().X,
                y_position = get_base_position().Y,
                scale = get_scale(),
                rotation = get_rotation_offset()
            };
        }

        public List<GameWorldPlayerAttribute> to_world_player_attributes_object() {
            return new List<GameWorldPlayerAttribute>() {
                new GameWorldPlayerAttribute {
                    identifier = "sword",
                    active = sword_attribute,
                    charges = get_max_attack_charges()
                },
                new GameWorldPlayerAttribute {
                    identifier = "dash",
                    active = dash_attribute,
                    charges = get_max_dashes()
                },
                new GameWorldPlayerAttribute {
                    identifier = "bow",
                    active = bow_attribute,
                    charges = get_max_arrows()
                }
            };
        }

        private int key_down(KeyboardState current_keyboard_state, Keys key) {
            if (current_keyboard_state.IsKeyDown(key))
                return 1;
            else
                return 0;
        }

        public int get_dash_charge() {
            return dash_charge;
        }

        public void set_dash_charges(int charges) {
            this.dash_charge = charges;
            Console.WriteLine($"dash_charges_set:{get_dash_charge()}");
        }

        public void set_max_dashes(int charges) {
            this.max_dashes = charges;
        }

        public int get_max_dashes() {
            return max_dashes;
        }

        public int get_attack_charge() {
            return attack_charge;
        }

        public void set_max_attack_charges(int charges) {
            this.max_attack_charges = charges;
        }

        public int get_max_attack_charges() {
            return max_attack_charges;
        }

        public void set_attack_charges(int charges) {
            this.attack_charge = charges;
            Console.WriteLine($"attack_charges_set:{get_attack_charge()}");
        }

        public int get_grenade_charge() {
            return grenade_charge;
        }

        public void set_grenade_charge(int value) {
            this.grenade_charge = value;
        }

        public bool interacting() {
            return _interact != 0;
        }

        public bool is_dashing() {
            return dash_active;
        }

        public bool is_attacking() {
            return attack_active || heavy_attack_active;
        }

        public void reset_dash_cooldown(float elapsed_value) {
            //reset dash cooldown for use outside Player.cs to avoid clashing inputs
            dash_cooldown_elapsed = elapsed_value;
            dash_charge = 0;
        }

        public RRect get_hurtbox() {
            return hurtbox;
        }

        public RRect get_shadowcast_box() {
            return shadowcast_box;
        }

        public RRect get_hitbox() {
            return hitbox;
        }

        public bool is_hurtbox_active() {
            return hurtbox_active;
        }

        public void add_arrow_charge(int arrow_charges) {
            arrow_charge += arrow_charges;
            if (arrow_charge > max_arrows) {
                arrow_charge = max_arrows;
            }
        }

        public void set_arrow_charges(int charges) {
            arrow_charge = charges;
        }

        public int get_arrow_charges() {
            return arrow_charge;
        }

        public int get_max_arrows() {
            return max_arrows;
        }

        public void set_max_arrow_charges(int charges) {
            max_arrows = charges;
            Console.WriteLine($"arrow_charges_set:{get_max_arrows()}");
        }

        public void set_attribute_charges(string attribute_id, int charges) {
            switch (attribute_id) {
                case "sword":
                    set_attack_charges(charges);
                    set_max_attack_charges(charges);
                    break;
                case "dash":
                    set_dash_charges(charges);
                    set_max_dashes(charges);
                    break;
                case "bow":
                    set_max_arrow_charges(charges);
                    set_arrow_charges(charges);
                    break;
                default:
                    //do nothing
                    break;
            }
        }
        
        public void save_player_attributes() {
            //write the change to the attributes file
            //gather current state of player attributes
            List<GameWorldPlayerAttribute> player_attributes = to_world_player_attributes_object();
            GameWorldPlayerAttributeFile player_attribute_file = new GameWorldPlayerAttributeFile {
                player_attributes = player_attributes
            };
            //serialize to json then write to file
            string file_contents = JsonSerializer.Serialize(player_attribute_file);
            //write to file
            world.write_to_file("player_attributes/", "player_attributes.json", file_contents);
        }

        public void set_attribute(string attribute_id, bool value) {
            switch (attribute_id) {
                case "sword":
                    sword_attribute = value;
                    break;
                case "dash":
                    dash_attribute = value;
                    break;
                case "bow":
                    bow_attribute = value;
                    break;
                default:
                    //do nothing
                    break;
            }
        }

        public bool get_attribute_active(string attribute_id) {
            switch (attribute_id) {
                case "sword":
                    return sword_attribute;
                case "dash":
                    return dash_attribute;
                case "bow":
                    return bow_attribute;
                default:
                    return false;
            }
        }

        public int get_money() {
            return money;
        }

        public void set_money(int value) {
            this.money = value;
        }

        public Emotion get_player_emote() {
            return current_emotion;
        }

        public void set_player_emotion(Emotion emote) {
            this.current_emotion = emote;
        }

        public bool in_hitstun() {
            return hitstun_active;
        }

        public void Draw(SpriteBatch spriteBatch) {
            Constant.profiler.start("player_draw");
            //draw footprints
            foreach (Footprints f in footprints) {
                f.Draw(spriteBatch);
            }

            //draw particle systems for player
            foreach (ParticleSystem ps in particle_systems) {
                ps.Draw(spriteBatch);
            }

            //draw shadow regardless of player state
            spriteBatch.Draw(Constant.shadow_tex, draw_position, null, Color.Black * 0.5f, -rotation, rotation_point, scale, SpriteEffects.None, 0f);

            //draw hits
            if (show_hit_texture && hit_confirm != null) {
                hit_confirm.Draw(spriteBatch);
                //spriteBatch.Draw(hit_texture, hit_texture_position, null, Color.White * 0.8f, hit_texture_rotation, hit_texture_rotation_point, scale, SpriteEffects.None, 0f);
            }
            if (slash_confirm != null) {
                slash_confirm.Draw(spriteBatch);
            }

            //draw character
            if (dash_active) { //draw call for dashing (draw this if the player is dashing)
                //spriteBatch.Draw(Constant.player_tex, draw_position, new Rectangle(0, 0, 32, 32), Color.Black * 0.5f, -rotation, rotation_point, scale, SpriteEffects.None, 0f);
                spriteBatch.Draw(Constant.player_dash_tex, draw_position, dash_animation.source_rect, Color.White, -rotation + rotation_offset, rotation_point, scale, SpriteEffects.None, 0f);
            } else if (aiming || charging_arrow) {
                spriteBatch.Draw(Constant.player_aim_tex, draw_position, aiming_animation_rect, power_shot_color, -rotation + rotation_offset, rotation_point, scale, SpriteEffects.None, 0f);
            } else if (charging_active) { //draw call for charging attack
                spriteBatch.Draw(Constant.player_charging_tex, attack_draw_position, charging_animation_rect, sweet_spot_color, -rotation + rotation_offset, rotation_point, scale, SpriteEffects.None, 0f);
            } else if (heavy_attack_active) { //draw call for heavy attack
                spriteBatch.Draw(Constant.player_heavy_attack_tex, attack_draw_position, heavy_attack_animation.source_rect, Color.White, -rotation + rotation_offset, rotation_point, scale, SpriteEffects.None, 0f);
            } else if (attack_active) { //draw call for attack
                spriteBatch.Draw(Constant.player_attack_tex, attack_draw_position, attack_animation.source_rect, Color.White, -rotation + rotation_offset, rotation_point, scale, SpriteEffects.None, 0f);
            } else if (_v != 0 || _h != 0) { //draw call for moving (draw this if the player is moving)
                //draw player in walk animation
                spriteBatch.Draw(Constant.player_tex, draw_position, walk_animation.source_rect, draw_color, -rotation + rotation_offset, rotation_point, scale, SpriteEffects.None, 0f);
            } else { //draw call for stationary (draw this if the player is not moving) / idle animation if the player is not moving
                //draw player
                spriteBatch.Draw(Constant.player_tex, draw_position, idle_animation.source_rect, draw_color, -rotation + rotation_offset, rotation_point, scale, SpriteEffects.None, 0f);
            }

            if (movement_disabled) {
                //if movement is disabled draw idle
                spriteBatch.Draw(Constant.player_tex, draw_position, idle_animation.source_rect, draw_color, -rotation + rotation_offset, rotation_point, scale, SpriteEffects.None, 0f);
            }

            //draw current emotion
            if (current_emotion != Emotion.Calm) {
                spriteBatch.Draw(Constant.emotion_texture_map[(int)current_emotion].Item1, emotion_display_position, null, Constant.emotion_texture_map[(int)current_emotion].Item2, -rotation + rotation_offset, rotation_point, scale, SpriteEffects.None, 0f);
            }

            //draw arrow (TEMP)
            if (arrow != null) {
                arrow.Draw(spriteBatch);
            }

            //draw debug info
            if (DEBUG){
                //Draw positions
                //Renderer.DrawALine(spriteBatch, Constant.pixel, 2, Color.Blue, 1f, base_position, base_position + new Vector2(0, -20));
                //Renderer.DrawALine(spriteBatch, Constant.pixel, 2, Color.White, 1f, draw_position + rotation_point, draw_position + rotation_point + new Vector2(0, -10));
                Renderer.DrawALine(spriteBatch, Constant.pixel, 2, Color.Yellow, 1f, depth_sort_position, depth_sort_position + new Vector2(0, -10));
                //Renderer.DrawALine(spriteBatch, Constant.pixel, 2, Color.Purple, 1f, attack_draw_position, attack_draw_position + new Vector2(0, -10));
                //Draw collision information
                hurtbox.draw(spriteBatch);
                //hitbox.draw(spriteBatch);
                future_hurtbox_x.set_color(Color.Pink);
                future_hurtbox_x.draw(spriteBatch);
                future_hurtbox_y.set_color(Color.Purple);
                future_hurtbox_y.draw(spriteBatch);
                shadowcast_box.set_color(Color.Orange);
                shadowcast_box.draw(spriteBatch);
                //Renderer.DrawALine(spriteBatch, Constant.pixel, 2, Color.Orange, 1f, draw_position, draw_position + new Vector2(0, -20));
                //Renderer.DrawALine(spriteBatch, Constant.pixel, 2, Color.Red, 1f, hitbox_center, hitbox_center + new Vector2(0, -5));
                //Renderer.DrawALine(spriteBatch, Constant.pixel, 2, Color.Gold, 1f, hitbox_draw_position, hitbox_draw_position + new Vector2(0, -5));
                //Renderer.DrawALine(spriteBatch, Constant.pixel, 2, Color.Magenta, 1f, center_position, center_position + new Vector2(0, -10));
                spriteBatch.DrawString(Constant.arial, "" + attack_charge, attack_draw_position, Color.Black, -rotation, Vector2.Zero, 0.12f, SpriteEffects.None, 0f);
                spriteBatch.DrawString(Constant.arial, $"charging:{charging_active}", attack_draw_position + new Vector2(0, 10), Color.Black, -rotation, Vector2.Zero, 0.12f, SpriteEffects.None, 0f);
                spriteBatch.DrawString(Constant.arial, $"charging_elapsed:{attack_charged_elapsed}", attack_draw_position + new Vector2(0, 20), Color.Black, -rotation, Vector2.Zero, 0.12f, SpriteEffects.None, 0f);

                Renderer.DrawALine(spriteBatch, Constant.pixel, 2, Color.Purple, 1f, get_base_position(), get_base_position()+resultant*50f);
            }
            Constant.profiler.end("player_draw");
        }
    }
}