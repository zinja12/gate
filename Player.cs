using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace gate
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
        private float walk_animation_duration = 170f;
        private float idle_animation_duration = 300f;
        private float dash_animation_duration = 85f;
        private float attack_animation_duration = 120f;

        //foot prints / animation
        private List<Footprints> footprints;
        private List<Footprints> reap_footprints;
        private float footprints_elapsed;
        private float footprints_delay = 300f;

        //movement and input
        private float movement_speed = 2.0f;
        private float _v, _h, _dash, _attack, _interact, _heavy_attack, _fire, _aim;
        private Vector2 direction;
        private Vector2 last_direction;
        private Vector2 dash_direction;
        private Vector2 direction_down = new Vector2(0, 1);
        private bool moving = false;

        //dash
        private bool dash_active;
        private int dash_charge = 2;
        private Vector2 dash_direction_unit;
        private Vector2 dash_start;
        private Vector2 dash_queued_direction;
        private float dash_speed = Constant.player_dash_speed;
        private float dash_length = Constant.player_dash_length;
        private float dash_cooldown = Constant.player_dash_cooldown; //cooldown in milliseconds
        private float dash_cooldown_elapsed = Constant.player_dash_cooldown; //set cooldown elapsed to same as cooldown so you can dash on level load
        private float doubledash_cool_down = Constant.player_doubledash_cooldown; //doubledash cooldown in milliseconds
        private bool dash_queued = false;

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
        private float hit_speed = 0.03f;
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

        //attack vars
        private bool attack_active, heavy_attack_active, charging_active, aiming, charging_arrow;
        private int attack_charge = 2;
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
        private int arrow_charge = 2, max_arrows = 2;
        private float power_shot_elapsed = 0f, power_shot_start_threshold = 500, power_shot_duration = 650f;
        private bool power_shot = false;
        private Color power_shot_color = Color.Red;

        /*PLAYER HEALTH*/
        private int health = 2;

        //idle variables
        private bool idle;
        private float idle_elapsed;
        private float idle_threshold = 60f;

        //camera control vars
        private bool camera_tracking_X = true, camera_tracking_Y = true;

        //particle variables
        List<ParticleSystem> particle_systems;
        List<ParticleSystem> dead_particle_systems;

        private int ID;

        //World variable to trigger world events on player triggers
        World world;

        public Player(Vector2 base_position, float scale, float screen_width, float screen_height, int ID, World world) {
            this.base_position = base_position;
            this.draw_position = new Vector2(base_position.X - (player_size / 2), 
                                            base_position.Y - player_size);
            this.attack_draw_position = new Vector2(draw_position.X - (player_size / 2), draw_position.Y - (player_size / 2));
            this.depth_sort_position = this.draw_position + new Vector2(0, player_size/2);
            this.camera_track_position = base_position;
            this.scale = scale;
            this.rotation_point = new Vector2(player_size / 2, player_size /2);
            //initialize animations with their appropriate starting frames on the spritesheets
            this.walk_animation = new Animation(walk_animation_duration, 3, (int)player_size * 1, 0, (int)player_size, (int)player_size);
            this.idle_animation = new Animation(idle_animation_duration, 3, (int)player_size * 1, 0, (int)player_size, (int)player_size);
            this.dash_animation = new Animation(dash_animation_duration, 4, (int)player_size * 1, 0, (int)player_size, (int)player_size);
            this.attack_animation = new Animation(attack_animation_duration, 5, (int)player_size * 2 * 1, 0, (int)player_size*2, (int)player_size*2);
            this.heavy_attack_animation = new Animation(attack_animation_duration, 5, (int)player_size*2 * 1, 0, (int)player_size*2, (int)player_size*2);
            this.charging_animation_rect = new Rectangle((int)player_size*2*0, (int)player_size*2*0, (int)player_size*2, (int)player_size*2);
            this.aiming_animation_rect = new Rectangle((int)player_size*0, (int)player_size*0, (int)player_size, (int)player_size);
            
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

            //test particle system code
            this.particle_systems = new List<ParticleSystem>();
            this.dead_particle_systems = new List<ParticleSystem>();

            //hit textures
            this.hit_texture = Constant.hit_confirm_spritesheet;
            this.hit_texture_rotation_point = new Vector2(hit_texture.Width/2, hit_texture.Height/2);

            this.ID = ID;

            //world variable reference
            this.world = world;
        }

        public void Update(GameTime gameTime, float rotation) {
            // set rotation
            this.rotation = rotation;
            
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
            } else { //controller input
                current_gamepad_state = GamePad.GetState(PlayerIndex.One);
                //get left thumbstick current position and set _v and _h
                _v = -current_gamepad_state.ThumbSticks.Left.Y;
                _h = current_gamepad_state.ThumbSticks.Left.X;
                _dash = current_gamepad_state.Buttons.A == ButtonState.Pressed ? 1f : 0f;
                _attack = current_gamepad_state.Buttons.X == ButtonState.Pressed ? 1f : 0f;
                _heavy_attack = current_gamepad_state.Buttons.B == ButtonState.Pressed ? 1f : 0f;
                _interact = current_gamepad_state.Buttons.Y == ButtonState.Pressed ? 1f : 0f;
                //trigger has to be at least halfway held down
                _aim = current_gamepad_state.Triggers.Left > 0 ? 1f : 0f;
                _fire = current_gamepad_state.Triggers.Right > 0 ? 1f: 0f;
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
                    a.fire_arrow(aim_orbit - draw_position, speed_multiplier, power_shot);
                    world.add_projectile(arrow);
                    this.arrow = null;
                    arrow_charge--;
                }
            } else if (charging_arrow && arrow == null) {
                arrow = new Arrow(get_base_position(), 1f, Constant.arrow_tex, 16, draw_position, hitbox_dir);
            }
            if (arrow != null && !aiming) { //if the arrow is out and we're not aiming, remove the arrow
                arrow = null;
            }
            if (arrow != null) {
                Arrow a = (Arrow)arrow;
                a.update_aim(aim_orbit, aim_orbit - draw_position);
                arrow.Update(gameTime, rotation);
            }
            if (_aim > 0 && arrow_charge > 0) {
                aiming = true;
            } else {
                aiming = false;
            }
            if (aiming && _fire > 0 && arrow_charge > 0) {
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
            #endregion

            if (current_gamepad_state.Buttons.B == ButtonState.Released && previous_gamepad_state.Buttons.B == ButtonState.Pressed && attack_charge > 1) {
                //trigger heavy attack
                if (attack_charged_elapsed >= 100f) {
                    if (sweet_spot) {
                        //add particle flair if player hits a sweet spot
                        particle_systems.Add(new ParticleSystem(Constant.rotate_point(draw_position, rotation, 1f, Constant.direction_up), 2, 800f, 5, Constant.red_particles));
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
            if (_heavy_attack != 0 && previous_gamepad_state.Buttons.B == ButtonState.Pressed && attack_charge > 1) {
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
            //meaning: if the player has pressed dash, and the dash currently is not active and if the last direction they held is not zero vector and the time since last dash is greater than the cooldown then dash
            if (_dash != 0 && !dash_active && last_direction != Vector2.Zero && dash_charge > 0 && !attack_active && !heavy_attack_active) {
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
                //test particle system code
                particle_systems.Add(new ParticleSystem(Constant.rotate_point(draw_position, rotation, 1f, Constant.direction_up), 2, 800f, 5, Constant.green_particles));
            } else if (dash_cooldown_elapsed >= dash_cooldown) { //reset dash_charge if the player has not dashed in the required cooldown
                dash_charge = 2;
                dash_cooldown = Constant.player_dash_cooldown;
            }

            //attack code
            if (_attack != 0 && !attack_active && attack_charge > 0) {
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
                attack_charge = 2;
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
            //normal movement code
            if (!dash_active && !attack_active && !heavy_attack_active && !charging_active && !charging_arrow && !aiming) {
                /*Update player position*/
                if (_v != 0 || _h != 0){
                    //Set direction to unit vector of (horizontal input, vertical input)
                    direction = Vector2.Zero + new Vector2(_h, _v);
                    //update last direction
                    last_direction.X = direction.X;
                    last_direction.Y = direction.Y;
                    moving = true;
                } else {
                    //zero out direction
                    direction = Vector2.Zero;
                    moving = false;
                }

                // rotate direction vector around the origin based on the current camera rotation
                float x = direction.X * (float)Math.Cos(-rotation) - direction.Y * (float)Math.Sin(-rotation);
                float y = direction.Y * (float)Math.Cos(-rotation) + direction.X * (float)Math.Sin(-rotation);

                direction = new Vector2(x, y);

                //alter the position based on direction and movement speed
                base_position += direction * movement_speed;
                draw_position += direction * movement_speed;
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
                base_position += dash_direction * dash_speed;
                draw_position += dash_direction * dash_speed;
                attack_draw_position += dash_direction * dash_speed;

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
                base_position += dash_direction * Constant.player_attack_movement_speed;
                draw_position += dash_direction * Constant.player_attack_movement_speed;
                attack_draw_position += dash_direction * Constant.player_attack_movement_speed;
            }
            #endregion

            /*ENEMY HIT COLLISION CODE*/
            receive_damage(gameTime);


            //add to dash and attack cooldown
            dash_cooldown_elapsed += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
            attack_cooldown_elapsed += (float)gameTime.ElapsedGameTime.TotalMilliseconds;

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

            //update animations for the player
            update_animation(gameTime);

            //update collision and hit/hurt boxes
            hurtbox.update(rotation, draw_position);
            //if the attack is active lock the hurtbox from moving
            update_hitbox_position(gameTime, _v, _h);
            hitbox.update(rotation, hitbox_center);

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

        public void take_hit(IEntity entity) {
            //reduce health on hit
            health--;
            //turn off hurt box for a bit
            hurtbox_active = false;
            //calculate hit direction
            hit_direction = get_base_position() - entity.get_base_position();
            //set color to red to indicate hit taken
            draw_color = Color.Red;
            //show hit texture
            show_hit_texture = true;
            //calculate hit texture direction
            hit_texture_position = hit_direction;
            hit_texture_position.Normalize(); //normalize (unit vector)
            hit_texture_position *= (-player_size/4); //set length with direction from this object to entity
            hit_texture_position += draw_position; //offset from draw_position
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
                    walk_animation.set_y_offset((int)player_size*1);
                    if (!dash_active)
                        dash_animation.set_y_offset((int)player_size*7);
                    //up attack animation
                    if (!attack_active) {
                        if (attack_charge == 1) { attack_animation.set_y_offset((int)player_size*2*0); }
                        else { attack_animation.set_y_offset((int)player_size*2*2); }
                    }
                    //up heavy attack animation
                    if (!heavy_attack_active)
                        heavy_attack_animation.set_y_offset((int)player_size*2*0);
                    //up charging animation rect
                    if (!charging_active)
                        charging_animation_rect.Y = (int)player_size*2*0;
                    aiming_animation_rect.Y = (int)player_size*6;
                } else if (_h < 0) { //up left
                    walk_animation.set_y_offset((int)player_size*0);
                    if (!dash_active)
                        dash_animation.set_y_offset((int)player_size*6);
                    //up attack animation
                    if (!attack_active) {
                        if (attack_charge == 1) { attack_animation.set_y_offset((int)player_size*2*1); }
                        else { attack_animation.set_y_offset((int)player_size*2*3); }
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
                        if (attack_charge == 1) { attack_animation.set_y_offset((int)player_size*2*0); }
                        else { attack_animation.set_y_offset((int)player_size*2*2); }
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
                        if (attack_charge == 1) { attack_animation.set_y_offset((int)player_size*2*1); }
                        else { attack_animation.set_y_offset((int)player_size*2*3); }
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
                        idle_animation.set_y_offset((int)player_size*7);
                        dash_animation.set_y_offset((int)player_size*7);
                        //up attack animation
                        if (attack_charge == 1) { attack_animation.set_y_offset((int)player_size*2*0); }
                        else { attack_animation.set_y_offset((int)player_size*2*2); }
                        //up heavy attack animation
                        heavy_attack_animation.set_y_offset((int)player_size*2*0);
                        //up charging animation rect
                        charging_animation_rect.Y = (int)player_size*2*0;
                        aiming_animation_rect.Y = (int)player_size*6;
                    } else if (lh < 0) { //up left
                        idle_animation.set_y_offset((int)player_size*6);
                        dash_animation.set_y_offset((int)player_size*6);
                        //up attack animation
                        if (attack_charge == 1) { attack_animation.set_y_offset((int)player_size*2*1); }
                        else { attack_animation.set_y_offset((int)player_size*2*3); }
                        //up heavy attack animation
                        heavy_attack_animation.set_y_offset((int)player_size*2*1);
                        //up charging animation rect
                        charging_animation_rect.Y = (int)player_size*2*1;
                        aiming_animation_rect.Y = (int)player_size*7;
                    } else { //directly up
                        idle_animation.set_y_offset((int)player_size*9);
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
                        idle_animation.set_y_offset((int)player_size*11);
                        dash_animation.set_y_offset((int)player_size*5);
                        //up attack animation
                        if (attack_charge == 1) { attack_animation.set_y_offset((int)player_size*2*0); }
                        else { attack_animation.set_y_offset((int)player_size*2*2); }
                        //down heavy attack animation
                        heavy_attack_animation.set_y_offset((int)player_size*2*0);
                        //down charging animation rect
                        charging_animation_rect.Y = (int)player_size*2*0;
                        aiming_animation_rect.Y = (int)player_size*5;
                    } else if (lh < 0){ //down left
                        idle_animation.set_y_offset((int)player_size*10);
                        dash_animation.set_y_offset((int)player_size*4);
                        //down attack animation
                        if (attack_charge == 1) { attack_animation.set_y_offset((int)player_size*2*1); }
                        else { attack_animation.set_y_offset((int)player_size*2*3); }
                        //down heavy attack animation
                        heavy_attack_animation.set_y_offset((int)player_size*2*1);
                        //down charging animation rect
                        charging_animation_rect.Y = (int)player_size*2*1;
                        aiming_animation_rect.Y = (int)player_size*4;
                    } else { //directly down
                        idle_animation.set_y_offset((int)player_size*8);
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
                    idle_animation.set_y_offset((int)player_size*6);
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
                    idle_animation.set_y_offset((int)player_size*7);
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
                    idle_animation.set_y_offset((int)player_size*6);
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
            if (is_dashing() || dash_active) {
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
                add_footprints(gameTime);
            } else {
                idle_animation.Update(gameTime);
            }

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
        }

        private void add_footprints(GameTime gameTime){
            footprints_elapsed += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
            //check to add footprints
            if (footprints_elapsed >= footprints_delay) {
                //create new footprint and set elapsed back to 0
                footprints.Add(new Footprints(depth_sort_position, 1f, Constant.footprint_tex, rotation, 0.5f, 0.01f, Color.Black));
                footprints_elapsed = 0;
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
            Vector2 center = entity.get_hurtbox().position;
            Vector2 base_pos = get_base_position();
            Vector2 center_to_player = base_pos - center;
            center_to_player.Normalize();
            base_position += center_to_player * movement_speed;
            draw_position += center_to_player * movement_speed;
        }

        public RRect get_future_hurtbox() {
            //need to update this to factor in dash direction and attack dash direction
            Vector2 draw = draw_position + direction * movement_speed;
            // if (!dash_active && !attack_active) {
            //     draw = draw_position + direction * movement_speed;
            // } else if (dash_active) {
            //     draw = draw_position + dash_direction * movement_speed;
            // } else if (attack_active) {
            //     draw = draw_position + dash_direction * movement_speed;
            // }
            RRect future_hurtbox = new RRect(draw, player_size, player_size);
            future_hurtbox.update(rotation, draw_position);
            return future_hurtbox;
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

        private int key_down(KeyboardState current_keyboard_state, Keys key) {
            if (current_keyboard_state.IsKeyDown(key))
                return 1;
            else
                return 0;
        }

        public int get_dash_charge() {
            return dash_charge;
        }

        public int get_attack_charge() {
            return attack_charge;
        }

        public bool interacting() {
            return _interact != 0;
        }

        public bool is_dashing() {
            return dash_active;
        }

        public void reset_dash_cooldown(float elapsed_value) {
            //reset dash cooldown for use outside Player.cs to avoid clashing inputs
            dash_cooldown_elapsed = elapsed_value;
            dash_charge = 0;
        }

        public RRect get_hurtbox() {
            return hurtbox;
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

        public int get_arrow_charges() {
            return arrow_charge;
        }

        public void Draw(SpriteBatch spriteBatch) {
            //draw footprints
            foreach (Footprints f in footprints) {
                f.Draw(spriteBatch);
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

            //draw arrow (TEMP)
            if (arrow != null) {
                arrow.Draw(spriteBatch);
            }

            //draw particle systems for player
            foreach (ParticleSystem ps in particle_systems) {
                ps.Draw(spriteBatch);
            }

            //draw debug info
            if (DEBUG){
                //Draw positions
                Renderer.DrawALine(spriteBatch, Constant.pixel, 2, Color.Blue, 1f, base_position, base_position + new Vector2(0, -20));
                Renderer.DrawALine(spriteBatch, Constant.pixel, 2, Color.White, 1f, draw_position + rotation_point, draw_position + rotation_point + new Vector2(0, -10));
                Renderer.DrawALine(spriteBatch, Constant.pixel, 2, Color.Yellow, 1f, depth_sort_position, depth_sort_position + new Vector2(0, -10));
                Renderer.DrawALine(spriteBatch, Constant.pixel, 2, Color.Purple, 1f, attack_draw_position, attack_draw_position + new Vector2(0, -10));
                //Draw collision information
                hurtbox.draw(spriteBatch);
                hitbox.draw(spriteBatch);
                Renderer.DrawALine(spriteBatch, Constant.pixel, 2, Color.Orange, 1f, draw_position, draw_position + new Vector2(0, -20));
                Renderer.DrawALine(spriteBatch, Constant.pixel, 2, Color.Red, 1f, hitbox_center, hitbox_center + new Vector2(0, -5));
                Renderer.DrawALine(spriteBatch, Constant.pixel, 2, Color.Gold, 1f, hitbox_draw_position, hitbox_draw_position + new Vector2(0, -5));
                Renderer.DrawALine(spriteBatch, Constant.pixel, 2, Color.Magenta, 1f, center_position, center_position + new Vector2(0, -10));
                spriteBatch.DrawString(Constant.arial, "" + attack_charge, attack_draw_position, Color.Black, -rotation, Vector2.Zero, 0.12f, SpriteEffects.None, 0f);
                spriteBatch.DrawString(Constant.arial, $"charging:{charging_active}", attack_draw_position + new Vector2(0, 10), Color.Black, -rotation, Vector2.Zero, 0.12f, SpriteEffects.None, 0f);
                spriteBatch.DrawString(Constant.arial, $"charging_elapsed:{attack_charged_elapsed}", attack_draw_position + new Vector2(0, 20), Color.Black, -rotation, Vector2.Zero, 0.12f, SpriteEffects.None, 0f);
            }
        }
    }
}