using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace gate.Core
{
    //emotions
    //each emotion affects gameplay differently
    //need to figure out if there's some gameplay that you can do to get rid of them quickly (pick ups / item inventory like talismans in Elden Ring?)
    public enum Emotion {
        Fear = 0, //movement speed reduction - stop moving for it to go away more quickly
        Anxiety = 1, //attack power reduction - 
        Calm = 2 //normal
    }
    
    public class Constant
    {
        //window title
        public static string window_title = "The Gate";
        //screen width and height
        public static float window_width = 1280f;
        public static float window_height = 720f;

        //textures, colors and fonts
        public static Texture2D tile_tex;
        public static Texture2D tile_tex2;
        public static Texture2D tile_tex3;
        public static Texture2D tile_tex4;
        public static Texture2D tree4_tex;
        public static Texture2D tan_tile_tex;
        public static Texture2D grass_tile_tex;
        public static Texture2D grass_tex;
        public static Texture2D player_tex;
        public static Texture2D player_dash_tex;
        public static Texture2D player_attack_tex;
        public static Texture2D player_heavy_attack_tex;
        public static Texture2D player_charging_tex;
        public static Texture2D player_aim_tex;
        public static Texture2D arrow_tex;
        public static Texture2D attack_sprites_tex;
        public static Texture2D pixel;
        public static Texture2D footprint_tex;
        public static Texture2D ghastly_tex;
        public static Texture2D sign_tex;
        public static Texture2D hit_confirm_spritesheet;
        public static Texture2D slash_confirm_spritesheet;
        public static Texture2D shadow_tex;
        public static Texture2D nightmare_tex;
        public static Texture2D nightmare_attack_tex;
        public static Texture2D wall_tex;
        public static Texture2D fence_spritesheet;
        public static Texture2D rock_spritesheet;
        public static Texture2D grass_spritesheet;
        public static Texture2D Y_tex;
        public static Texture2D orange_tree;
        public static Texture2D yellow_tree;
        public static Texture2D green_tree;
        public static Texture2D flower_tex;
        public static Texture2D stacked_grass;
        public static Texture2D trail_tex;
        public static Texture2D sand_tex;
        public static Texture2D tombstone_tex;
        public static Texture2D sword_tex;
        public static Texture2D box_spritesheet;
        public static Texture2D house_spritesheet;
        public static Texture2D scarecrow_tex;

        public static Texture2D fear_tex, anxiety_tex;

        public static Texture2D marker_spritesheet;
        public static Texture2D lamppost_spritesheet;
        public static Texture2D tree_spritesheet;

        public static Effect pixelate_effect;

        public static Color rich_black = new Color(0,8,7);
        public static Color manatee = new Color(162,163,187);
        public static Color blue_bell = new Color(147,149,211);
        public static Color maximum_blue_purple = new Color(179,183,238);
        public static Color ghost_white = new Color(251,249,255);

        /*Particle parameters*/
        public static List<Color> green_particles = new List<Color>() {Color.White, Color.Green, Color.Black};
        public static List<Color> red_particles = new List<Color>() {Color.White, Color.Red, Color.Black};
        public static List<Color> white_particles = new List<Color>() {Color.White, Color.White, Color.White};

        //gba color palette
        public static Color sandy = new Color(219,215,182);
        public static Color off_white = new Color(200,200,200);

        public static SpriteFont arial;
        public static SpriteFont arial_small;
        public static SpriteFont arial_mid_reg;

        //Reference directions in relation to the game world
        public static Vector2 direction_down = new Vector2(0, 1);
        public static Vector2 direction_up = new Vector2(0, -1);
        public static Vector2 direction_up_left = new Vector2(-1, -1);
        public static Vector2 direction_up_right = new Vector2(1, -1);
        public static Vector2 direction_up_left_middle = new Vector2(-0.25f, -1);
        public static Vector2 direction_up_right_middle = new Vector2(0.25f, -1);

        //Texbox constants
        //position of textboxes on the screen
        //NOTE: this Vector changes based on the size of the window
        public static Vector2 textbox_screen_position = new Vector2(10, window_height - (window_height/3));
        public static float textbox_width = 500, textbox_height = 150;

        /*UI CONSTANTS CONFIG*/
        public static int dash_charge_ui_size = 30;
        public static Vector2 dash_charge_ui_screen_position = new Vector2(window_width - (dash_charge_ui_size*2) - 10f, 10f);

        /*Camera Constants*/
        public static float camera_shake_milliseconds = 300f;
        public static float camera_shake_angle = 1f;
        public static float camera_shake_hit_radius = 3.8f;

        /*Key Bindings*/ //rememeber editor is bound to E
        public static Keys KEY_UP = Keys.W;
        public static Keys KEY_DOWN = Keys.S;
        public static Keys KEY_LEFT = Keys.A;
        public static Keys KEY_RIGHT = Keys.D;
        public static Keys KEY_INTERACT = Keys.Y;
        public static Keys KEY_DASH = Keys.Space;
        public static Keys KEY_ATTACK = Keys.V;
        public static Keys KEY_HEAVY_ATTACK = Keys.B;
        public static Keys KEY_FIRE = Keys.K;
        public static Keys KEY_AIM = Keys.J;

        /*GAMEPLAY FLAGS*/
        public bool debug = true;
        public static int stack_distance = 3;
        public static int stack_distance1 = 1;

        /*AI BEHAVIOR CONFIG*/
        public static float nightmare_engagement_distance = 200f;
        public static float nightmare_aggro_engagement_distance = 500f;

        /*PLAYER CONFIG*/
        public static int player_dash_charge = 2, player_attack_charge = 2, player_arrow_charge = 2, player_max_arrows = 2;
        public static float player_dash_cooldown = 800f;
        public static float player_doubledash_cooldown = 800f;
        public static float player_dash_speed = 6.5f;
        public static float player_dash_length = 100f;
        public static float player_attack_cooldown = 600f;
        public static float player_doubleattack_cooldown = 800f;
        public static float player_attack_movement_speed = 0.5f;

        /*SHADERS*/
        public static float pixels = 1700.0f;
        public static float pixelation = 3.55f;

        public static Dictionary<int, (Texture2D, Color)> emotion_texture_map;
        public static Dictionary<int, (Texture2D, Color)> generate_emotion_texture_map() {
            Dictionary<int, (Texture2D, Color)> tex_map = new Dictionary<int, (Texture2D, Color)>();
            tex_map.Add((int)Emotion.Fear, (Constant.fear_tex, Color.Pink));
            tex_map.Add((int)Emotion.Anxiety, (anxiety_tex, Color.Cyan));
            return tex_map;
        }

        /*ENTITY FLAGS*/
        public static string ENTITY_ACTIVE = "active"; //players, enemies, collision geometry entities
        public static string ENTITY_PASSIVE = "passive"; //plants, grass, environment entities

        public static List<string> projectile_collision_identifiers = new List<string>() {
            "fence",
            "sign",
            "lamp",
            "marker",
            "wall",
            "box"
        };

        /*EDITOR*/
        public static List<string> get_object_identifiers() {
            List<string> identifiers = new List<string>();
            identifiers.Add("player");
            identifiers.Add("tree");
            identifiers.Add("grass");
            identifiers.Add("sign");
            identifiers.Add("ghastly");
            identifiers.Add("marker");
            identifiers.Add("lamp");
            identifiers.Add("big_tile");
            identifiers.Add("cracked_tile");
            identifiers.Add("reg_tile");
            identifiers.Add("round_tile");
            identifiers.Add("nightmare");
            identifiers.Add("wall");
            identifiers.Add("fence");
            identifiers.Add("tan_tile");
            identifiers.Add("grass_tile");
            identifiers.Add("orange_tree");
            identifiers.Add("yellow_tree");
            identifiers.Add("green_tree");
            identifiers.Add("flower");
            identifiers.Add("grass2");
            identifiers.Add("trail_tile");
            identifiers.Add("sand_tile");
            identifiers.Add("sword");
            identifiers.Add("box");
            identifiers.Add("house");
            identifiers.Add("scarecrow");
            return identifiers;
        }

        //function to generate a list of rectangles to draw for one stack
        public static List<Rectangle> generate_rectangles_for_stack(Texture2D texture, int sprite_count){
            //Calculate width of one sprite
            float width = texture.Width/sprite_count;
            float height = texture.Height;
            Vector2 start = Vector2.Zero;
            List<Rectangle> sprites = new List<Rectangle>();
            for (int i = 0; i < sprite_count; i++){
                Rectangle sprite = new Rectangle((int)(start.X + i*width), (int)start.Y, (int)width, (int)height);
                sprites.Add(sprite);
            }
            return sprites;
        }

        public static float get_object_depth(float rotation, Vector2 position) {
            float angleRadians = rotation;
            float posX = position.X;
            float posY = position.Y;
            // Use sine and cosine functions to generate depth value based on both rotation and position
            // You can adjust the multiplier to change the frequency and amplitude of the cycle
            float depthValue = ((float)Math.Sin(angleRadians) * posX + (float)Math.Cos(angleRadians) * posY) * 0.5f + 0.5f;
            return depthValue;
        }

        public static float get_depth(float rotation, Vector2 depth_sort_position) {
            // wrap angle and convert to degrees
            float depth = 0;
            float wrapped_degrees2 = MathHelper.ToDegrees(MathHelper.WrapAngle(rotation));
            if ((wrapped_degrees2 < 90 && wrapped_degrees2 > 0) || (wrapped_degrees2 < 0 && wrapped_degrees2 > -90)){
                depth = depth_sort_position.Y;
            } else {
                depth = -depth_sort_position.Y;
            }
            //depth = depth_sort_position.Y;
            return depth;
        }

        public static Vector2 PerpendicularClockwise(Vector2 vector2, float length)
        {
            return new Vector2(vector2.Y, -vector2.X) * length;
        }

        public static Vector2 PerpendicularCounterClockwise(Vector2 vector2, float length)
        {
            return new Vector2(-vector2.Y, vector2.X) * length;
        }

        public static Vector2 rotate_point(Vector2 rotation_point, float rotation, float distance, Vector2 direction) {
            return rotation_point + distance * new Vector2(direction.X * (float)Math.Cos(-rotation) - direction.Y * (float)Math.Sin(-rotation), direction.Y * (float)Math.Cos(-rotation) + direction.X * (float)Math.Sin(-rotation));
        }

        public static Texture2D convert_to_white(SpriteBatch spriteBatch, Texture2D texture) {
            Color[] color_data = new Color[texture.Width*texture.Height];
            texture.GetData<Color>(color_data);

            List<Color> white_color_data = new List<Color>();
            //get new color data transformed to white if not transparent
            foreach (Color c in color_data) {
                if (c == Color.Transparent) {
                    white_color_data.Add(c);
                } else {
                    white_color_data.Add(new Color(255, 255, 255));
                }
            }

            //set new color data
            Texture2D new_tex = new Texture2D(spriteBatch.GraphicsDevice, texture.Width, texture.Height);
            new_tex.SetData<Color>(white_color_data.ToArray());
            return new_tex;
        }

        //screen to world position
        public static Vector2 world_position_transform(Vector2 position, Camera camera)
        {
            return Vector2.Transform(position, Matrix.Invert(camera.Transform));
        }

        //world to screen position
        public static Vector2 world_position_to_screen_position(Vector2 position, Camera camera) {
            return Vector2.Transform(position, camera.Transform);
        }

        public static void update_ui_positions_on_screen_size_change() {
            dash_charge_ui_screen_position = new Vector2(window_width - (dash_charge_ui_size*2) - 10f, 10f);
            textbox_screen_position = new Vector2(10, window_height - (window_height/3));
        }

        public static double truncate_to_sig_digits(double d, int digits) {
            if(d == 0)
                return 0;

            double scale = Math.Pow(10, Math.Floor(Math.Log10(Math.Abs(d))) + 1 - digits);
            return scale * Math.Truncate(d / scale);
        }

        public static bool isNaN(float f) {
            return float.IsNaN(f);
        }

        public static float WrapAngle(float angle) {
            angle %= 360f;
            if (angle < 0f)
            {
                angle += 360f;
            }
            return angle;
        }

        public static int TranslateAngleToCompassDirection(float angle, float rotationAngle) {
            // Adjust angle based on the rotation of the world
            angle += rotationAngle;
            // Ensure angle is within 0 to 360 degrees range
            if (angle < 0) {
                angle += 360;
            }
            else if (angle >= 360) {
                angle -= 360;
            }
            //wrap angle
            angle = WrapAngle(angle);

            if ((angle > 337.5 && angle <= 360) || (angle >= 0 && angle <= 22.5)) {
                //east
                return 2;
            } else if (angle > 22.5 && angle <= 67.5) {
                //se
                return 3;
            } else if (angle > 67.5 && angle <= 112.5) {
                //south
                return 4;
            } else if (angle > 112.5 && angle <= 157.5) {
                //sw
                return 5;
            } else if (angle > 157.5 && angle <= 202.5) {
                //west
                return 6;
            } else if (angle > 202.5 && angle <= 247.5) {
                //nw
                return 7;
            } else if (angle > 247.5 && angle <= 292.5) {
                //north
                return 0;
            } else if (angle > 292.5 && angle <= 337.5) {
                //ne
                return 1;
            } else { //if all else fails
                return 0;
            }
        }

        public static float get_angle(Vector2 a, Vector2 b) {
            return (float)Math.Atan2(b.Y - a.Y, b.X - a.X);
        }
    }
}