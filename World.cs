using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using gate.Particles;
using gate.Serialize;
using gate.Interface;
using gate.Core;
using gate.Entities;
using gate.Triggers;
using gate.Collision;
using gate.Conditions;
using gate.Sounds;
using gate.Lighting;

namespace gate
{
    public class World
    {
        //Game object
        Game1 game;

        //debug variables
        float fps;

        //Graphics and game objects
        public GraphicsDeviceManager _graphics;
        public string content_root_directory;
        public ContentManager Content;

        //loaded textures
        List<string> loaded_textures;
        //bool loading = false;
        bool debug_triggers = true;

        public string load_file_name = "crossroads2.json", current_level_id;
        public string player_attribute_file_name = "player_attributes.json";
        string save_file_name = "untitled_sandbox.json";

        //game config
        bool object_persistence = false;
        public bool run_intro_text = true;

        //objects present in every world
        Camera camera;
        Player player;
        RRect camera_bounds;
        Vector2 camera_bounds_center;

        //data structures needed in every world
        List<IEntity> plants;
        RenderList entities_list;
        List<IEntity> collision_entities;
        List<ForegroundEntity> foreground_entities;
        List<BackgroundEntity> background_entities;
        List<FloorEntity> floor_entities;
        List<TempTile> temp_tiles;
        List<ITrigger> triggers;
        List<IEntity> collision_geometry;
        List<IAiEntity> enemies;
        List<IAiEntity> npcs;
        List<IEntity> projectiles;
        ConditionManager condition_manager;
        Dictionary<IEntity, bool> collision_geometry_map;
        Dictionary<IEntity, bool> collision_tile_map;
        List<IEntity> switches;
        SoundManager sound_manager;
        List<Light> lights;
        List<IEntity> light_excluded_entities;
        Dictionary<string, bool> condition_tags;
        List<(IEntity, Vector2, float)> explosion_list;
        Dictionary<IEntity, float> dropped_items; //entity -> elapsed time
        //editor ONLY objects
        List<IEntity> editor_only_objects;

        //clear variables
        List<IEntity> entities_to_clear;

        //script parser
        ScriptParser world_script_parser;

        //dictionary for level loaded counts
        Dictionary<string, int> level_loaded_map;

        public TextBox current_textbox = null;
        public Sign current_sign = null;
        public NPC current_npc = null;

        float rotation = 0f;
        
        //world variables
        private float render_distance = 600;
        private int freeze_frames = 0;
        //level transition variables
        private bool transition_active = false;
        private string next_level_id = null;
        private LevelTrigger current_level_trigger = null;
        private float transition_elapsed, transition_threshold = 1000f;
        private float transition_percentage = 0f;
        private bool transition1_complete = false;
        //level rotation variables
        private bool rotation_active = false;
        private float camera_goal_rotation;
        //script variables
        private bool post_level_load_script_trigger = false, gameplay_script_trigger = false;
        private ScriptTrigger current_script_trigger = null;
        //checkpoint variables
        private float checkpoint_interact_elapsed = 0f, checkpoint_interact_threshold = 500f;
        private string checkpoint_level_id = null, shade_level_id = null;

        //camera world variables
        private bool camera_shake = false;
        private float shake_elapsed;
        private float shake_threshold;
        private Vector2 shake_offset = Vector2.Zero;
        private float shake_angle = 1f;
        private float shake_radius = 5f;
        private List<int> viewport_points_outside_collider = new List<int>();
        float camera_gamepad_h_input = 0f;
        private bool player_camera_rotate_enabled = true, camera_invert = false;
        public bool player_camera_tethered = true;

        //intro text variables
        public bool intro_text_playing = false;
        public List<string> intro_text;
        public float intro_text_opacity = 1f, intro_text_elapsed = 0f, intro_text_threshold, intro_text_finish_elapsed = 0f, intro_text_finished_threshold;

        //editor variables
        private bool editor_active = false, editor_enabled = true;
        private int selected_object = 1;
        private float selection_cooldown = 200f, selection_elapsed;
        private float editor_object_rotation = 0f, editor_object_scale = 1f;
        //private Dictionary<int, Texture2D> object_map;
        private Dictionary<int, IEntity> obj_map;
        private Vector2 mouse_world_position, create_position;
        private RRect mouse_hitbox, brush_box;
        private int previous_scroll_value;
        private Keys previous_key;
        private ICondition selected_condition;
        //editor tools: (object_placement, object/linkage editor)
        private int editor_tool_idx, editor_object_idx, editor_layer, editor_layer_count, editor_tile_idx;
        private Dictionary<int, string> editor_tool_name_map = new Dictionary<int, string>() {
            {0, "place"},
            {1, "cond."},
            {2, "delete"},
            {3, "tile"},
            {4, "tree_brush"},
            {5, "tile2_snap"}
        };
        private Dictionary<Vector2, int> editor_floor_tile_map = new Dictionary<Vector2, int>();
        private Dictionary<int, (Texture2D, string, int)> editor_tiles_map;

        //Random variable
        private Random random = new Random();

        /*PARTICLE SYSTEM*/
        private List<ParticleSystem> particle_systems;
        private List<ParticleSystem> dead_particle_systems;

        //Lights
        private bool lights_enabled = true;
        RenderTarget2D light_map_target;

        public World(Game1 game, GraphicsDeviceManager _graphics, string content_root_directory, ContentManager Content) {
            //set game objects
            this.game = game;
            this._graphics = _graphics;
            this.content_root_directory = content_root_directory;
            this.Content = Content;

            //set up camera to draw with
            camera = new Camera(_graphics.GraphicsDevice.Viewport, Vector2.Zero);
            camera.Zoom = 1.75f;

            //set up mouse hitbox
            mouse_hitbox = new RRect(Vector2.Zero, 10, 10);
            mouse_hitbox.set_color(Color.Pink);
            //set up brush hitbox
            brush_box = new RRect(Vector2.Zero, 300, 300);
            brush_box.set_color(Color.Black);

            //init load textures
            loaded_textures = new List<string>();

            //create list of plants
            plants = new List<IEntity>();
            //create list of collision entities
            collision_entities = new List<IEntity>();
            //create list of collision geometry
            collision_geometry = new List<IEntity>();
            //create list of foreground objects to be used as tree tops, etc
            foreground_entities = new List<ForegroundEntity>();
            //create list of background objects to be used as floor pieces
            background_entities = new List<BackgroundEntity>();
            //create list of floor objects to be used behind floor pieces
            floor_entities = new List<FloorEntity>();
            //list of fx entities
            temp_tiles = new List<TempTile>();
            //create list of triggers
            triggers = new List<ITrigger>();
            //create list of enemies
            enemies = new List<IAiEntity>();
            //crease list of npcs
            npcs = new List<IAiEntity>();
            //create list of projectiles
            projectiles = new List<IEntity>();
            //create conditions manager
            condition_manager = new ConditionManager(this);
            //create dict for collision map for level geometry
            collision_geometry_map = new Dictionary<IEntity, bool>();
            collision_tile_map = new Dictionary<IEntity, bool>();
            //list for switches
            switches = new List<IEntity>();
            //sound manager
            sound_manager = new SoundManager(this, Content);
            //list for lights
            lights = new List<Light>();
            light_excluded_entities = new List<IEntity>();
            //condition tags
            condition_tags = new Dictionary<string, bool>();
            //explosion list
            explosion_list = new List<(IEntity, Vector2, float)>();
            //dropped items list
            dropped_items = new Dictionary<IEntity, float>();

            //editor ONLY objects
            editor_only_objects = new List<IEntity>();

            //weapons
            entities_to_clear = new List<IEntity>();

            //set dictionary for level loading
            level_loaded_map = new Dictionary<string, int>();

            //create entities list and add player
            entities_list = new RenderList();

            //initialize particle systems for world effects
            this.particle_systems = new List<ParticleSystem>();
            this.dead_particle_systems = new List<ParticleSystem>();

            //light resources
            //set up light render target
            light_map_target = new RenderTarget2D(_graphics.GraphicsDevice, _graphics.GraphicsDevice.Viewport.Width, _graphics.GraphicsDevice.Viewport.Height);

            //load first level
            load_level(content_root_directory, _graphics, load_file_name);

            /*Editor Initialization*/
            editor_init();

            //run first sort so everything looks good initially
            entities_list.sort_list_by_depth(camera.Rotation, player.get_base_position(), render_distance);
        }

        public World(Game1 game, GraphicsDeviceManager _graphics, string content_root_directory, ContentManager Content, string level_id) : this(game, _graphics, content_root_directory, Content) {
            unload_level();
            load_level(content_root_directory, _graphics, level_id);
        }

        #region editor_initialization
        private void editor_init() {
            /*Editor Initialization*/
            editor_tool_idx = 0;
            editor_layer = 0;
            editor_layer_count = 2;
            editor_tile_idx = 0;
            //obj map init
            obj_map = new Dictionary<int, IEntity>();
            obj_map.Add(1, new Tree(Vector2.Zero, 1f, Constant.tree_spritesheet, false, "tree", -1));
            obj_map.Add(2, new Grass(Vector2.Zero, 1f, -1));
            obj_map.Add(3, new Ghastly(Constant.ghastly_tex, Vector2.Zero, 1f, Constant.hit_confirm_spritesheet, -1));
            obj_map.Add(4, new StackedObject("marker", Constant.marker_spritesheet, Vector2.Zero, 1f, 32, 32, 15, Constant.stack_distance, 0f, -1));
            obj_map.Add(5, new Lamppost(Vector2.Zero, 1f, -1));
            obj_map.Add(6, new Tile(Vector2.Zero, 1f, Constant.tile_tex, "big_tile", (int)DrawWeight.Light, -1));
            obj_map.Add(7, new Tile(Vector2.Zero, 1f, Constant.tile_tex2, "cracked_tile", (int)DrawWeight.Light, -1));
            obj_map.Add(8, new Tile(Vector2.Zero, 1f, Constant.tile_tex3, "reg_tile", (int)DrawWeight.Light, -1));
            obj_map.Add(9, new Tile(Vector2.Zero, 1f, Constant.tile_tex4, "round_tile", (int)DrawWeight.Light, -1));
            obj_map.Add(10, new Tile(Vector2.Zero, 2f, Constant.tan_tile_tex, "tan_tile", (int)DrawWeight.Medium, -1));
            obj_map.Add(11, new Tree(Vector2.Zero, 4f, Constant.tree_spritesheet, true, "tree",  -1));
            obj_map.Add(12, new StackedObject("wall", Constant.wall_tex, Vector2.Zero, 1f, 32, 32, 8, Constant.stack_distance, 0f, -1));
            obj_map.Add(13, new StackedObject("fence", Constant.fence_spritesheet, Vector2.Zero, 1f, 32, 32, 18, Constant.stack_distance1, 0f, -1));
            obj_map.Add(14, new InvisibleObject("deathbox", Vector2.Zero, 1f, 48, 48, 0f, -1));
            obj_map.Add(15, new Nightmare(Constant.nightmare_tex, Constant.nightmare_attack_tex, Vector2.Zero, 1f, Constant.hit_confirm_spritesheet, player, -1, "nightmare"));
            //set obj 14 to visible so we can see it
            InvisibleObject io = (InvisibleObject)obj_map[14];
            io.set_debug(true);
            Nightmare nightmare1 = (Nightmare)obj_map[15];
            //turn off ai for editor
            nightmare1.set_behavior_enabled(false);
            //set up condition in editor
            obj_map.Add(16, new PlaceHolderEntity(Vector2.Zero, "Condition(EDROC)", -1));
            obj_map.Add(17, new Tile(Vector2.Zero, 2f, Constant.grass_tile_tex, "grass_tile", (int)DrawWeight.Heavy, -1));
            obj_map.Add(18, new StackedObject("orange_tree", Constant.orange_tree, Vector2.Zero, 1f, 64, 64, 26, Constant.stack_distance, 0f, -1));
            obj_map.Add(19, new StackedObject("yellow_tree", Constant.yellow_tree, Vector2.Zero, 1f, 64, 64, 26, Constant.stack_distance, 0f, -1));
            obj_map.Add(20, new StackedObject("green_tree", Constant.green_tree, Vector2.Zero, 1f, 64, 64, 26, Constant.stack_distance, 0f, -1));
            obj_map.Add(21, new StackedObject("flower", Constant.flower_tex, Vector2.Zero, 1f, 32, 32, 12, Constant.stack_distance1, 0f, -1));
            obj_map.Add(22, new StackedObject("grass2", Constant.tall_grass_tex, Vector2.Zero, 1f, 32, 32, 9, Constant.stack_distance1, 0f, -1));
            obj_map.Add(23, new Tile(Vector2.Zero, 2f, Constant.trail_tex, "trail_tile", (int)DrawWeight.Medium, -1));
            obj_map.Add(24, new Tile(Vector2.Zero, 2f, Constant.sand_tex, "sand_tile", (int)DrawWeight.Medium, -1));
            obj_map.Add(25, new StackedObject("sword", Constant.sword_tex, Vector2.Zero, 1f, 16, 16, 22, Constant.stack_distance1, 0f, -1));
            obj_map.Add(26, new StackedObject("box", Constant.box_spritesheet, Vector2.Zero, 1f, 32, 32, 18, Constant.stack_distance1, 0f, -1));
            obj_map.Add(27, new StackedObject("house", Constant.house_spritesheet, Vector2.Zero, 1f, 128, 128, 54, Constant.stack_distance1, 0f, -1));
            obj_map.Add(28, new PlaceHolderEntity(Vector2.Zero, "ParticleSystem", -1));
            obj_map.Add(29, new NPC(Constant.scarecrow_tex, Vector2.Zero, 1, 32, (int)AIBehavior.Stationary, null, "", Constant.hit_confirm_spritesheet, player, -1, "scarecrow", this, true));
            obj_map.Add(30, new Ghost(Constant.ghastly_tex, Vector2.Zero, 1f, Constant.hit_confirm_spritesheet, player, -1, "ghost", this));
            Ghost ghost1 = (Ghost)obj_map[30];
            //turn off ai for editor
            ghost1.set_behavior_enabled(false);
            obj_map.Add(31, new HitSwitch("hitswitch", Constant.switch_active, Constant.switch_inactive, Vector2.Zero, 1f, 16, 16, 8, Constant.stack_distance1, 0f, -1));
            obj_map.Add(32, new PlaceHolderEntity(Vector2.Zero, "Condition(SwitchC)", -1));
            obj_map.Add(33, new BillboardSprite(Constant.dash_cloak_pickup_tex, Vector2.Zero, 1f, "dash_cloak", -1));
            obj_map.Add(34, new StackedObject("cracked_rocks", Constant.cracked_rocks_spritesheet, Vector2.Zero, 1f, 32, 32, 4, Constant.stack_distance1, 0f, -1));
            obj_map.Add(35, new BillboardSprite(Constant.player_chip_tex, Vector2.Zero, 1f, "player_chip", -1));
            obj_map.Add(36, new BillboardSprite(Constant.bow_pickup_tex, Vector2.Zero, 1f, "bow", -1));
            obj_map.Add(37, new Haunter(Constant.haunter_tex, Vector2.Zero, 1f, Constant.hit_confirm_spritesheet, player, -1, "haunter", this));
            Haunter haunter1 = (Haunter)obj_map[37];
            haunter1.set_behavior_enabled(false);
            obj_map.Add(38, new StackedObject("checkpoint", Constant.checkpoint_marker_spritesheet, Vector2.Zero, 1f, 32, 32, 15, Constant.stack_distance, 0f, -1));
            obj_map.Add(39, new Skeleton(Constant.skelly_tex, Constant.skelly_attack_tex, Constant.skelly_attack_charge_tex, Vector2.Zero, 1f, Constant.hit_confirm_spritesheet, player, -1, "skeleton"));
            Skeleton skeleton1 = (Skeleton)obj_map[39];
            skeleton1.set_behavior_enabled(false);
            obj_map.Add(40, new ShadowKnight(Constant.shadow_knight_tex, Constant.shadow_knight_attack_tex, Constant.shadow_knight_charge_attack_tex, Vector2.Zero, 1f, Constant.hit_confirm_spritesheet, player, -1, "shadowknight"));
            ShadowKnight sk = (ShadowKnight)obj_map[40];
            sk.set_behavior_enabled(false);
            obj_map.Add(41, new StackedObject("gate", Constant.gate_spritesheet, Vector2.Zero, 1f, 32, 32, 28, Constant.stack_distance1, 0f, -1));
            obj_map.Add(42, new StackedObject("green_wall", Constant.green_wall_tex, Vector2.Zero, 1f, 32, 32, 8, Constant.stack_distance, 0f, -1));
            obj_map.Add(43, new StackedObject("torii", Constant.torii_spritesheet, Vector2.Zero, 1f, 64, 64, 43, Constant.stack_distance, 0f, -1));
        }
        #endregion

        public string read_gameworld_file_contents(string root_dir, string path, string lvl_id) {
            string file_contents;
            //set file path and pull all contents
            var file_path = Path.Combine(root_dir, path + lvl_id);
            Console.WriteLine("loading file path:" + file_path);
            //pull file contents from path with stream reader
            using (var stream = TitleContainer.OpenStream(file_path)){
                using (var reader = new StreamReader(stream)){
                    //pull all contents out of file
                    file_contents = reader.ReadToEnd();
                }
            }
            return file_contents;
        }

        public string read_from_file(string root_dir, string file_path, string file_name) {
            var path = Path.Combine(root_dir, file_path + file_name);
            using (var file_stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(file_stream)) {
                return reader.ReadToEnd();
            }
        }

        public bool file_exists(string file_path) {
            return File.Exists(file_path);
        }
        
        //function to return the modified level file full path to load from
        //if modified level file does not exist, it just retuns level id
        public string get_modified_level_file(string root_dir, string file_path, string level_id) {
            //if object persistence is not enabled, just return level id as is
            if (!get_object_persistence()) {
                Console.WriteLine($"object persistence disabled({get_object_persistence()}). returning un-modified default level id");
                return level_id;
            }

            //in the case of object persistence being enabled we need to look for the modified level file to return
            var modified_level_id = Constant.level_mod_prefix + level_id;
            //combine path
            var path = Path.Combine(root_dir, file_path + modified_level_id);
            //check for file existence and return based on file existence
            if (file_exists(path)) {
                Console.WriteLine($"modified level file found({path})");
                return Constant.level_mod_prefix + level_id;
            } else {
                Console.WriteLine($"no modified level file found({path})");
                Console.WriteLine($"level_id_path:{Path.Combine(root_dir, file_path + level_id)}");
                return level_id;
            }
        }

        #region load_level
        //function to load level files into the world
        /*
        * NOTE: Eventually we will get to the place where we want to start saving the modifications to level files
        * This essentially means that we will have to save a level on level unload and check what has been destroyed / changed
        * then save that off to a separate level_change file and on level load, read the change file and intermix the changes
        * to hold state from the level the player left should they come back to it. Hopefully won't be too difficult, however
        * an open question is how we handle IDs for objects that have been removed or destroyed? Otherwise should we not have
        * that object loaded the load function will throw and Incongruency Error...
        */
        public void load_level(string root_directory, GraphicsDeviceManager _graphics, string level_id, Vector2? player_start_point = null) {
            //set current level id
            current_level_id = level_id;
            //load context for fx
            Constant.pixelate_effect = Content.Load<Effect>("fx/pixelate");
            Constant.pixelate_effect.Parameters["pixels"].SetValue(Constant.pixels);
            Constant.pixelate_effect.Parameters["pixelation"].SetValue(Constant.pixelation);
            Constant.color_palette_effect = Content.Load<Effect>("fx/color_palette");
            Constant.color_palette_effect.Parameters["Palette"].SetValue(Constant.palette_colors);
            Constant.color_palette_effect.Parameters["PaletteSize"].SetValue(Constant.palette_colors.Length);
            Constant.scanline2_effect = Content.Load<Effect>("fx/scanline2");
            Constant.scanline2_effect.Parameters["screen_height"].SetValue(Constant.window_height);
            //load content not specific to an object
            Constant.tile_tex = Content.Load<Texture2D>("sprites/tile3");
            Constant.pixel = Content.Load<Texture2D>("sprites/white_pixel");
            Constant.footprint_tex = Content.Load<Texture2D>("sprites/footprint");
            Constant.attack_sprites_tex = Content.Load<Texture2D>("sprites/attack_sprites2");
            Constant.hit_confirm_spritesheet = Content.Load<Texture2D>("sprites/hit_confirm_spritesheet");
            Constant.slash_confirm_spritesheet = Content.Load<Texture2D>("sprites/slash_confirm_spritesheet");
            Constant.shadow_tex = Content.Load<Texture2D>("sprites/shadow0");
            Constant.green_tree = Content.Load<Texture2D>("sprites/green_tree1_1_26");
            Constant.read_interact_tex = Content.Load<Texture2D>("sprites/read_interact");
            Constant.crystal_tex = Content.Load<Texture2D>("sprites/crystal2");
            //load fonts
            Constant.arial = Content.Load<SpriteFont>("fonts/arial");
            Constant.arial_small = Content.Load<SpriteFont>("fonts/arial_small");
            Constant.arial_mid_reg = Content.Load<SpriteFont>("fonts/arial_mid_reg");
            Constant.pxf_font = Content.Load<SpriteFont>("fonts/pxf_font");
            Constant.pxf_thin = Content.Load<SpriteFont>("fonts/pxf_thin_font");
            //load emotions
            Constant.fear_tex = Content.Load<Texture2D>("sprites/fear_anger");
            Constant.anxiety_tex = Content.Load<Texture2D>("sprites/anxiety");
            //set up texture map once textures are loaded
            Constant.emotion_texture_map = Constant.generate_emotion_texture_map();

            //load universal sounds
            sound_manager.load_sfx(ref Constant.hit1_sfx, "sfx/hitHurt1");
            sound_manager.load_sfx(ref Constant.typewriter_sfx, "sfx/typewriter");
            sound_manager.load_sfx(ref Constant.explosion_sfx, "sfx/explosion");

            //set pixel shader to active once it has been loaded
            game.set_pixel_shader_active(true);

            //debug fps initialization
            game.fps = new FpsCounter(game, Constant.arial_small, Vector2.Zero);
            game.fps.Initialize();

            //set camera tether to player on every level load
            this.player_camera_tethered = true;

            List<string> loaded_obj_identifiers = new List<string>();

            //set up and read level file
            string level_file_path = "levels/";
            string dialogue_file_path = "dialogue/";
            string player_attribute_file_path = "player_attributes/";
            //check for existence of modified level file to load for gameplay and world continuity sake
            string modified_level_id = get_modified_level_file(root_directory, level_file_path, level_id);
            string file_contents = read_from_file(root_directory, level_file_path, modified_level_id);
            /*Read object from contents as json*/
            GameWorldFile world_file_contents = JsonSerializer.Deserialize<GameWorldFile>(file_contents);
            //set up script parser
            world_script_parser = new ScriptParser(this, world_file_contents.world_script, level_loaded_map.ContainsKey(level_id) ? level_loaded_map[level_id] : 1);
            //parse world script
            int command_parse_count = 0;
            if (!world_script_parser.parse_script(world_file_contents.world_script, out command_parse_count)) {
                //parse has failed something is wrong
                throw new Exception("World Script Parser Error. Please fix world script.");
            }
            //check commands parsed
            if (command_parse_count != world_file_contents.world_script.Count) {
                throw new Exception($"World Script Parser Error: Commands parsed {command_parse_count} while world file contains {world_file_contents.world_script.Count} commands");
            }
            //check deserialization
            if (world_file_contents != null) {
                //variables to check world file as loading occurs
                int player_count = 0;
                //parse camera bounds for level
                if (world_file_contents.camera_bounds == null) {
                    Console.WriteLine("world file camera bounds set to null");
                } else if (world_file_contents.camera_bounds != null) {
                    GameWorldObject bounds = world_file_contents.camera_bounds;
                    camera_bounds_center = new Vector2(bounds.x_position, bounds.y_position);
                    camera_bounds = new RRect(camera_bounds_center, bounds.width, bounds.height);
                }
                //parse checkpoint level id
                if (world_file_contents.checkpoint_level_id != null) {
                    //set checkpoint level id
                    this.checkpoint_level_id = world_file_contents.checkpoint_level_id;
                }
                //parse and set clear color
                if (world_file_contents.clear_color != null) {
                    //try to parse clear color
                    try {
                        game.clear_color = Constant.FromHex(world_file_contents.clear_color);
                    } catch (Exception e) {
                        Console.WriteLine($"Warning: Unable to parse clear color hex value from world file. Exception:{e.ToString()}");
                        //default color
                        game.clear_color = Color.CornflowerBlue;
                    }
                } else {
                    //default color if clear color is unspecified
                    game.clear_color = Color.CornflowerBlue;
                }
                //Iterate over world objects
                Dictionary<int, string> unique_obj_id_map = new Dictionary<int, string>();
                for (int i = 0; i < world_file_contents.world_objects.Count; i++) {
                    GameWorldObject w_obj = world_file_contents.world_objects[i];
                    //check if unique object map contains this object id alread (obj ids are meant to be unique)
                    if (unique_obj_id_map.ContainsKey(w_obj.object_id_num)) {
                        throw new Exception($"World File Error: Two objects with the same object_id_num({w_obj.object_id_num}) found in world file. Object ids are meant to be unique. Tags for objects: {w_obj.object_identifier},{unique_obj_id_map[w_obj.object_id_num]}");
                    }
                    //set the key value in the unqiue obj id map
                    unique_obj_id_map[w_obj.object_id_num] = w_obj.object_identifier;
                    //check if the loaded objects contains the object we are trying to load, if not add it saying we have loaded the textures
                    //this is basically to find textures we have not loaded for the editor
                    if (!loaded_obj_identifiers.Contains(w_obj.object_identifier)) {
                        loaded_obj_identifiers.Add(w_obj.object_identifier);
                    }
                    Vector2 obj_position = new Vector2(w_obj.x_position, w_obj.y_position);
                    if (w_obj.object_id_num != i) {
                        Console.WriteLine($"Incongruency between ith object and id_num: w_obj:{w_obj.object_identifier},w_obj_id_num:{w_obj.object_id_num}");
                        throw new Exception($"Object id and ith object count incongruency. w_obj:{w_obj.object_identifier},w_obj_id_num:{w_obj.object_id_num}");
                    }

                    switch (w_obj.object_identifier) {
                        case "player_chip":
                            if (player_count > 0) {
                                throw new Exception("World File Error: Too many player objects found in world file. There can only be one player to rule them all.");
                            }
                            //load ui icons
                            check_and_load_tex(ref Constant.dash_icon, "sprites/dash_icon");
                            check_and_load_tex(ref Constant.sword_icon, "sprites/sword_icon");
                            check_and_load_tex(ref Constant.bow_icon, "sprites/bow_icon");
                            //load textures for player
                            check_and_load_tex(ref Constant.player_tex, "sprites/test_player_spritesheet7");
                            check_and_load_tex(ref Constant.player_dash_tex, "sprites/test_player_dash_spritesheet1");
                            check_and_load_tex(ref Constant.player_attack_tex, "sprites/test_player_attacks_spritesheet6");
                            check_and_load_tex(ref Constant.player_heavy_attack_tex, "sprites/test_player_heavy_attack_spritesheet1");
                            check_and_load_tex(ref Constant.player_charging_tex, "sprites/test_player_charging_spritesheet1");
                            check_and_load_tex(ref Constant.player_aim_tex, "sprites/test_player_bow_aim_spritesheet1");
                            check_and_load_tex(ref Constant.arrow_tex, "sprites/arrow1");
                            check_and_load_tex(ref Constant.player_chip_tex, "sprites/player_chip");
                            check_and_load_tex(ref Constant.grenade_tex, "sprites/tnt1");
                            //load sounds for player
                            sound_manager.load_sfx(ref Constant.footstep_sfx, "sfx/cfootstep");
                            sound_manager.load_sfx(ref Constant.dash_sfx, "sfx/dash2");
                            sound_manager.load_sfx(ref Constant.bow_up_aim_sfx, "sfx/bow_up_aim");
                            sound_manager.load_sfx(ref Constant.sword_slash_sfx, "sfx/sword_slash");
                            sound_manager.load_sfx(ref Constant.coin_pickup_sfx, "sfx/coin_pickup");
                            //create player object
                            if (player_start_point.HasValue){
                                obj_position = player_start_point.Value;
                            }
                            //NOTE: we should probably do something about the object_id_num for player since it's basically not relevant now for save files
                            Player p = new Player(obj_position, w_obj.scale, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight, w_obj.object_id_num, this);
                            //read player attribute file to initialize player
                            string player_file_contents = read_from_file(root_directory, player_attribute_file_path, player_attribute_file_name);
                            GameWorldPlayerAttributeFile player_attribute_file_contents = JsonSerializer.Deserialize<GameWorldPlayerAttributeFile>(player_file_contents);
                            if (player_attribute_file_contents != null) {
                                foreach (GameWorldPlayerAttribute pa in player_attribute_file_contents.player_attributes) {
                                    //set attribute active / inactive
                                    p.set_attribute(pa.identifier, pa.active);
                                    //set charges for attributes accordingly if attribute is active
                                    if (pa.active && pa.charges > 0) {
                                        p.set_attribute_charges(pa.identifier, pa.charges);
                                    } else {
                                        p.set_attribute_charges(pa.identifier, 0);
                                    }
                                }
                                //read and set player money
                                p.set_money(player_attribute_file_contents.player_money);
                            }
                            entities_list.Add(p);
                            collision_entities.Add(p);
                            //make sure to set player reference and increment count
                            player = p;
                            player_count++;
                            //set up player chip in editor only objects
                            BillboardSprite p_chip = new BillboardSprite(Constant.player_chip_tex, obj_position, w_obj.scale, "player_chip", w_obj.object_id_num);
                            editor_only_objects.Add(p_chip);
                            break;
                        case "tree":
                            //load texture
                            check_and_load_tex(ref Constant.tree_spritesheet, "sprites/tree_spritesheet5");
                            Tree t = new Tree(obj_position, w_obj.scale, Constant.tree_spritesheet, w_obj.canopy, w_obj.object_identifier, w_obj.object_id_num);
                            if (w_obj.canopy) {
                                foreground_entities.Add(t);
                            } else {
                                entities_list.Add(t);
                                plants.Add(t);
                            }
                            break;
                        case "orange_tree":
                            //load texture
                            check_and_load_tex(ref Constant.orange_tree, "sprites/orange_tree4_6_26");
                            StackedObject otree = new StackedObject(w_obj.object_identifier, Constant.orange_tree, obj_position, w_obj.scale, 64, 64, 26, Constant.stack_distance, w_obj.rotation, w_obj.object_id_num);
                            otree.set_sway(true);
                            entities_list.Add(otree);
                            plants.Add(otree);
                            break;
                        case "yellow_tree":
                            //load texture
                            check_and_load_tex(ref Constant.yellow_tree, "sprites/yellow_tree1_4_26");
                            StackedObject ytree = new StackedObject(w_obj.object_identifier, Constant.yellow_tree, obj_position, w_obj.scale, 64, 64, 26, Constant.stack_distance, w_obj.rotation, w_obj.object_id_num);
                            ytree.set_sway(true);
                            entities_list.Add(ytree);
                            plants.Add(ytree);
                            break;
                        case "green_tree":
                            //load texture
                            check_and_load_tex(ref Constant.green_tree, "sprites/green_tree1_1_26");
                            StackedObject gtree = new StackedObject(w_obj.object_identifier, Constant.green_tree, obj_position, w_obj.scale, 64, 64, 26, Constant.stack_distance, w_obj.rotation, w_obj.object_id_num);
                            gtree.set_sway(true);
                            entities_list.Add(gtree);
                            plants.Add(gtree);
                            break;
                        case "grass":
                            //load texture
                            // check_and_load_tex(ref Constant.grass_tex, "sprites/grass_0");
                            // Grass g = new Grass(obj_position, w_obj.scale, w_obj.object_id_num);
                            // plants.Add(g);
                            // entities_list.Add(g);
                            break;
                        case "sign":
                            //load texture
                            check_and_load_tex(ref Constant.sign_tex, "sprites/sign1");
                            Sign s = new Sign(Constant.sign_tex, obj_position, w_obj.scale, w_obj.sign_messages, w_obj.object_id_num, this);
                            entities_list.Add(s);
                            collision_entities.Add(s);
                            break;
                        case "ghastly":
                            //load texture
                            check_and_load_tex(ref Constant.ghastly_tex, "sprites/ghastly1");
                            Ghastly ghast = new Ghastly(Constant.ghastly_tex, obj_position, w_obj.scale, Constant.hit_confirm_spritesheet, w_obj.object_id_num);
                            entities_list.Add(ghast);
                            collision_entities.Add(ghast);
                            break;
                        case "ghost":
                            check_and_load_tex(ref Constant.ghastly_tex, "sprites/ghastly1");
                            check_and_load_tex(ref Constant.sludge_tex, "sprites/sludge1");
                            Ghost ghost = new Ghost(Constant.ghastly_tex, obj_position, w_obj.scale, Constant.hit_confirm_spritesheet, player, w_obj.object_id_num, w_obj.object_identifier, this);
                            entities_list.Add(ghost);
                            collision_entities.Add(ghost);
                            enemies.Add(ghost);
                            break;
                        case "marker":
                            //load texture
                            check_and_load_tex(ref Constant.marker_spritesheet, "sprites/marker_spritesheet5");
                            StackedObject m = new StackedObject(w_obj.object_identifier, Constant.marker_spritesheet, obj_position, w_obj.scale, 32, 32, 15, Constant.stack_distance, w_obj.rotation, w_obj.object_id_num);
                            entities_list.Add(m);
                            collision_geometry.Add(m);
                            collision_geometry_map[m] = false;
                            break;
                        case "checkpoint":
                            check_and_load_tex(ref Constant.checkpoint_marker_spritesheet, "sprites/checkpoint_spritesheet1_15");
                            StackedObject cpm = new StackedObject(w_obj.object_identifier, Constant.checkpoint_marker_spritesheet, obj_position, w_obj.scale, 32, 32, 15, Constant.stack_distance, w_obj.rotation, w_obj.object_id_num, true);
                            entities_list.Add(cpm);
                            collision_geometry.Add(cpm);
                            collision_geometry_map[cpm] = false;
                            break;
                        case "lamp":
                            //load texture
                            check_and_load_tex(ref Constant.lamppost_spritesheet, "sprites/lamppost_spritesheet3");
                            Lamppost l = new Lamppost(obj_position, w_obj.scale, w_obj.object_id_num);
                            //check if object has light assigned
                            if (w_obj.light != null) {
                                Light light = new Light(l.get_base_position(), 250f, 0.45f, Color.White, l);
                                lights.Add(light);
                                if (w_obj.light_excluded) {
                                    light_excluded_entities.Add(l);
                                }
                            }
                            entities_list.Add(l);
                            break;
                        case "big_tile":
                            //load texture
                            check_and_load_tex(ref Constant.tile_tex, "sprites/tile3");
                            Tile tile = new Tile(obj_position, w_obj.scale, Constant.tile_tex, w_obj.object_identifier, (int)DrawWeight.Light, w_obj.object_id_num);
                            background_entities.Add(tile);
                            break;
                        case "cracked_tile":
                            //load texture
                            check_and_load_tex(ref Constant.tile_tex2, "sprites/tile4");
                            Tile c_tile = new Tile(obj_position, w_obj.scale, Constant.tile_tex2, w_obj.object_identifier, (int)DrawWeight.Light, w_obj.object_id_num);
                            background_entities.Add(c_tile);
                            break;
                        case "reg_tile":
                            //load texture
                            check_and_load_tex(ref Constant.tile_tex3, "sprites/tile5");
                            Tile r_tile = new Tile(obj_position, w_obj.scale, Constant.tile_tex3, w_obj.object_identifier, (int)DrawWeight.Light, w_obj.object_id_num);
                            background_entities.Add(r_tile);
                            break;
                        case "round_tile":
                            //load texture
                            check_and_load_tex(ref Constant.tile_tex4, "sprites/tile6");
                            Tile round_tile = new Tile(obj_position, w_obj.scale, Constant.tile_tex4, w_obj.object_identifier, (int)DrawWeight.Light, w_obj.object_id_num);
                            background_entities.Add(round_tile);
                            break;
                        case "nightmare":
                            check_and_load_tex(ref Constant.nightmare_tex, "sprites/test_nightmare_spritesheet2");
                            check_and_load_tex(ref Constant.nightmare_attack_tex, "sprites/test_nightmare_attacks_spritesheet1");
                            Nightmare nightmare = new Nightmare(Constant.nightmare_tex, Constant.nightmare_attack_tex, obj_position, w_obj.scale, Constant.hit_confirm_spritesheet, player, w_obj.object_id_num, w_obj.object_identifier);
                            entities_list.Add(nightmare);
                            collision_entities.Add(nightmare);
                            enemies.Add(nightmare);
                            break;
                        case "npc":
                            //check for conversation file specified
                            GameWorldDialogueFile dialogue_file = null;
                            if (w_obj.npc_conversation_file_id != null) {
                                //conversation file specified, read the file
                                string dialogue_file_contents = read_gameworld_file_contents(root_directory, dialogue_file_path, w_obj.npc_conversation_file_id);
                                /*Read file via json*/
                                dialogue_file = JsonSerializer.Deserialize<GameWorldDialogueFile>(dialogue_file_contents);
                            }
                            //initialize npc as stationary to start
                            NPC npc = new NPC(Constant.player_tex, obj_position, w_obj.scale, 32, (int)AIBehavior.Stationary, dialogue_file, w_obj.npc_conversation_file_id, Constant.hit_confirm_spritesheet, player, w_obj.object_id_num, w_obj.object_identifier, this);
                            //if there are path points specified then set the path points and set the behavior to loop
                            if (w_obj.npc_path_entity_ids != null && w_obj.npc_path_entity_ids.Count > 0) {
                                //add all path points to npc
                                foreach (int entity_id in w_obj.npc_path_entity_ids) {
                                    npc.add_path_point(find_entity_by_id(entity_id));
                                }
                                //set behavior to loop path
                                npc.set_ai_behavior(AIBehavior.LoopPath);
                            }
                            //add to entities list and npcs
                            entities_list.Add(npc);
                            collision_entities.Add(npc);
                            npcs.Add(npc);
                            break;
                        case "scarecrow":
                            //no need for dialogue file (yet)
                            check_and_load_tex(ref Constant.scarecrow_tex, "sprites/scarecrow1");
                            NPC scrow = new NPC(Constant.scarecrow_tex, obj_position, w_obj.scale, 32, (int)AIBehavior.Stationary, null, "", Constant.hit_confirm_spritesheet, player, w_obj.object_id_num, w_obj.object_identifier, this, true);
                            scrow.set_health(10000);
                            //add to entities list and npcs
                            entities_list.Add(scrow);
                            collision_entities.Add(scrow);
                            npcs.Add(scrow);
                            break;
                        case "wall":
                            check_and_load_tex(ref Constant.wall_tex, "sprites/box_spritesheet1");
                            StackedObject w = new StackedObject(w_obj.object_identifier, Constant.wall_tex, obj_position, w_obj.scale, 32, 32, 8, Constant.stack_distance, w_obj.rotation, w_obj.object_id_num);
                            entities_list.Add(w);
                            collision_geometry.Add(w);
                            collision_geometry_map[w] = false;
                            break;
                        case "fence":
                            check_and_load_tex(ref Constant.fence_spritesheet, "sprites/fence1");
                            StackedObject f = new StackedObject(w_obj.object_identifier, Constant.fence_spritesheet, obj_position, w_obj.scale, 32, 32, 18, Constant.stack_distance1, w_obj.rotation, w_obj.object_id_num);
                            entities_list.Add(f);
                            collision_geometry.Add(f);
                            collision_geometry_map[f] = false;
                            break;
                        case "gate":
                            check_and_load_tex(ref Constant.gate_spritesheet, "sprites/closed_gate1_28");
                            StackedObject closedgate = new StackedObject(w_obj.object_identifier, Constant.gate_spritesheet, obj_position, w_obj.scale, 32, 32, 28, Constant.stack_distance1, w_obj.rotation, w_obj.object_id_num);
                            entities_list.Add(closedgate);
                            collision_geometry.Add(closedgate);
                            collision_geometry_map[closedgate] = false;
                            break;
                        case "box":
                            check_and_load_tex(ref Constant.box_spritesheet, "sprites/box1_2_18");
                            StackedObject box = new StackedObject(w_obj.object_identifier, Constant.box_spritesheet, obj_position, w_obj.scale, 32, 32, 18, Constant.stack_distance1, w_obj.rotation, w_obj.object_id_num);
                            entities_list.Add(box);
                            collision_geometry.Add(box);
                            collision_geometry_map[box] = false;
                            break;
                        case "tan_tile":
                            check_and_load_tex(ref Constant.tan_tile_tex, "sprites/tile_tan1");
                            Tile t_tile = new Tile(obj_position, w_obj.scale, Constant.tan_tile_tex, w_obj.object_identifier, (int)DrawWeight.Medium, w_obj.object_id_num);
                            add_floor_entity(t_tile);
                            break;
                        case "grass_tile":
                            check_and_load_tex(ref Constant.grass_tile_tex, "sprites/grass_tile1");
                            Tile gt_tile = new Tile(obj_position, w_obj.scale, Constant.grass_tile_tex, w_obj.object_identifier, (int)DrawWeight.Heavy, w_obj.object_id_num);
                            if (editor_enabled && !editor_floor_tile_map.ContainsKey(obj_position)) {
                                //add to editor tile map
                                editor_floor_tile_map.Add(obj_position, 0);
                                add_floor_entity(gt_tile);
                            }
                            break;
                        case "stone_tile":
                            check_and_load_tex(ref Constant.stone_tile_tex, "sprites/stone_tile3");
                            Tile st_tile = new Tile(obj_position, w_obj.scale, Constant.stone_tile_tex, w_obj.object_identifier, (int)DrawWeight.Heavy, w_obj.object_id_num);
                            add_floor_entity(st_tile);
                            if (editor_enabled && !editor_floor_tile_map.ContainsKey(obj_position)) {
                                //add to editor tile map
                                editor_floor_tile_map.Add(obj_position, 1);
                            }
                            break;
                        case "deathbox":
                            //don't need to check and load texture because this object is meant to be invisible/not drawn
                            InvisibleObject io = new InvisibleObject(w_obj.object_identifier, obj_position, w_obj.scale, 48, 48, w_obj.rotation, w_obj.object_id_num);
                            collision_geometry.Add(io);
                            break;
                        case "flower":
                            check_and_load_tex(ref Constant.flower_tex, "sprites/flower1_1_12");
                            StackedObject fl = new StackedObject(w_obj.object_identifier, Constant.flower_tex, obj_position, w_obj.scale, 32, 32, 12, Constant.stack_distance1, w_obj.rotation, w_obj.object_id_num);
                            fl.set_sway(true);
                            entities_list.Add(fl);
                            plants.Add(fl);
                            break;
                        case "grass2":
                            check_and_load_tex(ref Constant.short_grass_tex, "sprites/short_grass2_4");
                            check_and_load_tex(ref Constant.tall_grass_tex, "sprites/tall_grass2_9");
                            StackedObject g2 = new StackedObject(w_obj.object_identifier, Constant.tall_grass_tex, obj_position, w_obj.scale, 32, 32, 9, Constant.stack_distance1, w_obj.rotation, w_obj.object_id_num);
                            g2.set_sway(true);
                            entities_list.Add(g2);
                            plants.Add(g2);
                            break;
                        case "trail_tile":
                            check_and_load_tex(ref Constant.trail_tex, "sprites/trail1");
                            Tile trail_tile = new Tile(obj_position, w_obj.scale, Constant.trail_tex, w_obj.object_identifier, (int)DrawWeight.Medium, w_obj.object_id_num);
                            add_floor_entity(trail_tile);
                            break;
                        case "sand_tile":
                            check_and_load_tex(ref Constant.sand_tex, "sprites/sand1");
                            Tile sand_tile = new Tile(obj_position, w_obj.scale, Constant.sand_tex, w_obj.object_identifier, (int)DrawWeight.Medium, w_obj.object_id_num);
                            add_floor_entity(sand_tile);
                            break;
                        case "sword":
                            check_and_load_tex(ref Constant.sword_tex, "sprites/sword1_22");
                            StackedObject sword = new StackedObject(w_obj.object_identifier, Constant.sword_tex, obj_position, w_obj.scale, 16, 16, 22, Constant.stack_distance1, w_obj.rotation, w_obj.object_id_num);
                            entities_list.Add(sword);
                            collision_entities.Add(sword);
                            break;
                        case "house":
                            check_and_load_tex(ref Constant.house_spritesheet, "sprites/house2_1_54");
                            StackedObject house = new StackedObject(w_obj.object_identifier, Constant.house_spritesheet, obj_position, w_obj.scale, 128, 128, 54, Constant.stack_distance1, w_obj.rotation, w_obj.object_id_num);
                            entities_list.Add(house);
                            collision_geometry.Add(house);
                            collision_geometry_map[house] = false;
                            break;
                        case "hitswitch":
                            check_and_load_tex(ref Constant.switch_active, "sprites/switch2_active_8");
                            check_and_load_tex(ref Constant.switch_inactive, "sprites/switch2_inactive_8");
                            HitSwitch hs = new HitSwitch(w_obj.object_identifier, Constant.switch_active, Constant.switch_inactive, obj_position, w_obj.scale, 16, 16, 8, Constant.stack_distance1, w_obj.rotation, w_obj.object_id_num);
                            entities_list.Add(hs);
                            collision_entities.Add(hs);
                            collision_geometry.Add(hs);
                            collision_geometry_map[hs] = false;
                            switches.Add(hs);
                            break;
                        case "dash_cloak":
                            check_and_load_tex(ref Constant.dash_cloak_pickup_tex, "sprites/dash_cloak_pickup");
                            BillboardSprite dash_cloak = new BillboardSprite(Constant.dash_cloak_pickup_tex, obj_position, w_obj.scale, w_obj.object_identifier, w_obj.object_id_num, true);
                            entities_list.Add(dash_cloak);
                            collision_entities.Add(dash_cloak);
                            break;
                        case "cracked_rocks":
                            check_and_load_tex(ref Constant.cracked_rocks_spritesheet, "sprites/cracked_rocks_4");
                            StackedObject cracked_rocks = new StackedObject(w_obj.object_identifier, Constant.cracked_rocks_spritesheet, obj_position, w_obj.scale, 32, 32, 4, Constant.stack_distance1, w_obj.rotation, w_obj.object_id_num);
                            entities_list.Add(cracked_rocks);
                            collision_geometry.Add(cracked_rocks);
                            collision_geometry_map[cracked_rocks] = false;
                            break;
                        case "bow":
                            check_and_load_tex(ref Constant.bow_pickup_tex, "sprites/bow_pickup");
                            BillboardSprite bow = new BillboardSprite(Constant.bow_pickup_tex, obj_position, w_obj.scale, w_obj.object_identifier, w_obj.object_id_num, true);
                            entities_list.Add(bow);
                            collision_entities.Add(bow);
                            break;
                        case "haunter":
                            check_and_load_tex(ref Constant.haunter_tex, "sprites/haunter1");
                            check_and_load_tex(ref Constant.hex1_tex, "sprites/hex1");
                            check_and_load_tex(ref Constant.haunter_attack_tex, "sprites/haunter_attack1");
                            Haunter haunter = new Haunter(Constant.haunter_tex, obj_position, w_obj.scale, Constant.hit_confirm_spritesheet, player, w_obj.object_id_num, w_obj.object_identifier, this);
                            entities_list.Add(haunter);
                            collision_entities.Add(haunter);
                            enemies.Add(haunter);
                            break;
                        case "skeleton":
                            check_and_load_tex(ref Constant.skelly_tex, "sprites/skelly_spritesheet1");
                            check_and_load_tex(ref Constant.skelly_attack_tex, "sprites/skelly_attacks_spritesheet1");
                            check_and_load_tex(ref Constant.skelly_attack_charge_tex, "sprites/skelly_charge_attack_spritesheet1");
                            Skeleton skelly = new Skeleton(Constant.skelly_tex, Constant.skelly_attack_tex, Constant.skelly_attack_charge_tex, obj_position, w_obj.scale, Constant.hit_confirm_spritesheet, player, w_obj.object_id_num, w_obj.object_identifier);
                            entities_list.Add(skelly);
                            collision_entities.Add(skelly);
                            enemies.Add(skelly);
                            break;
                        case "shadowknight":
                            check_and_load_tex(ref Constant.shadow_knight_tex, "sprites/shadow_knight_spritesheet1");
                            check_and_load_tex(ref Constant.shadow_knight_attack_tex, "sprites/shadow_knight_attacks_spritesheet1");
                            check_and_load_tex(ref Constant.shadow_knight_charge_attack_tex, "sprites/shadow_knight_charge_attack_spritesheet1");
                            ShadowKnight sk = new ShadowKnight(Constant.shadow_knight_tex, Constant.shadow_knight_attack_tex, Constant.shadow_knight_charge_attack_tex, obj_position, w_obj.scale, Constant.hit_confirm_spritesheet, player, w_obj.object_id_num, w_obj.object_identifier);
                            entities_list.Add(sk);
                            collision_entities.Add(sk);
                            enemies.Add(sk);
                            break;
                        case "green_wall":
                            check_and_load_tex(ref Constant.green_wall_tex, "sprites/green_box2_8");
                            StackedObject gw = new StackedObject(w_obj.object_identifier, Constant.green_wall_tex, obj_position, w_obj.scale, 32, 32, 8, Constant.stack_distance, w_obj.rotation, w_obj.object_id_num);
                            entities_list.Add(gw);
                            collision_geometry.Add(gw);
                            collision_geometry_map[gw] = false;
                            break;
                        case "torii":
                            check_and_load_tex(ref Constant.torii_spritesheet, "sprites/torii_gate_43");
                            StackedObject torii = new StackedObject(w_obj.object_identifier, Constant.torii_spritesheet, obj_position, w_obj.scale, 64, 64, 43, Constant.stack_distance, w_obj.rotation, w_obj.object_id_num);
                            entities_list.Add(torii);
                            //not adding to collision geometry because we essentially are going to hide a trigger under it
                            //basically just for decoration
                            break;
                        default:
                            break;
                    }
                }
                //set editor object_idx to whatever i is for editor current object idx later on
                editor_object_idx = world_file_contents.world_objects.Count - 1;

                //check for absence of player
                if (player_count < 1) {
                    throw new Exception("World File Error: No player_chip found in file for player to spawn at. Please add a player chip object to world file.");
                }

                //set up triggers
                if (world_file_contents.world_triggers != null) {
                    for (int i = 0; i < world_file_contents.world_triggers.Count; i++) {
                        GameWorldTrigger w_trigger = world_file_contents.world_triggers[i];
                        editor_object_idx++;
                        ITrigger trigger = null;
                        Vector2 trigger_position = new Vector2(w_trigger.x_position, w_trigger.y_position);
                        switch (w_trigger.object_identifier) {
                            case "level_trigger":
                                Vector2 entry_position = new Vector2(w_trigger.entry_x_position, w_trigger.entry_y_position);
                                trigger = new LevelTrigger(trigger_position, w_trigger.width, w_trigger.height, w_trigger.level_id, entry_position, player, w_trigger.object_id_num);
                                break;
                            case "rotation_trigger":
                                trigger = new RotationTrigger(trigger_position, w_trigger.width, w_trigger.height, w_trigger.goal_rotation, player, w_trigger.object_id_num);
                                break;
                            case "script_trigger":
                                trigger = new ScriptTrigger(trigger_position, w_trigger.width, w_trigger.height, w_trigger.previously_activated, w_trigger.retrigger, player, w_trigger.object_id_num);
                                break;
                            default:
                                //nothing
                                break;
                        }
                        //add trigger if not null
                        if (trigger != null) {
                            triggers.Add(trigger);
                        }
                    }
                }

                //set up conditions
                if (world_file_contents.conditions != null) {
                    for (int i = 0; i < world_file_contents.conditions.Count; i++) {
                        GameWorldCondition w_condition = world_file_contents.conditions[i];
                        editor_object_idx++;
                        ICondition c = null;
                        //add condition to condition tags map
                        if (w_condition.obj_ids_to_remove.Count == 0) {
                            //if the condition has empty ids, that means they were removed via the condition being met
                            //save as the condition has been met (true)
                            upsert_condition_tags(w_condition.tag, true);
                        } else {
                            //the condition still has entities to remove meaning it has not been met
                            upsert_condition_tags(w_condition.tag, false);
                        }
                        //create condition in world
                        switch (w_condition.object_identifier) {
                            case "all_enemies_dead_remove_objs":
                                if (w_condition.enemy_ids == null || w_condition.enemy_ids.Count == 0) {
                                    c = new EnemiesDeadRemoveObjCondition(editor_object_idx, this, enemies, w_condition.obj_ids_to_remove, new Vector2(w_condition.x_position, w_condition.y_position), w_condition.tag);
                                } else {
                                    c = new EnemiesDeadRemoveObjCondition(editor_object_idx, this, enemies, w_condition.enemy_ids, w_condition.obj_ids_to_remove, new Vector2(w_condition.x_position, w_condition.y_position), w_condition.tag);
                                }
                                break;
                            case "switch_condition":
                                if (w_condition.enemy_ids != null && w_condition.enemy_ids.Count > 0) {
                                    c = new SwitchCondition(editor_object_idx, this, switches, w_condition.enemy_ids, w_condition.obj_ids_to_remove, new Vector2(w_condition.x_position, w_condition.y_position), w_condition.tag);
                                }
                                break;
                            default:
                                break;
                        }
                        //add created condition to condition manager
                        condition_manager.add_condition(c);
                    }
                }
                //increment editor object idx
                editor_object_idx++;

                //print dict
                Console.WriteLine("condition_tags:");
                foreach (KeyValuePair<string, bool> kv in condition_tags) {
                    Console.WriteLine($"{kv.Key}-{kv.Value}");
                }

                //do not try to access particle systems should it not exist in the file
                if (world_file_contents.particle_systems != null) {
                    for (int i = 0; i < world_file_contents.particle_systems.Count; i++) {
                        GameWorldParticleSystem game_particle_system = world_file_contents.particle_systems[i];
                        particle_systems.Add(
                            new ParticleSystem(
                                game_particle_system.in_world,
                                new Vector2(game_particle_system.x_position, game_particle_system.y_position),
                                game_particle_system.max_speed,
                                game_particle_system.life_duration,
                                game_particle_system.frequency,
                                game_particle_system.min_scale,
                                game_particle_system.max_scale,
                                Constant.white_particles,
                                new List<Texture2D>() { Constant.footprint_tex }
                            )
                        );
                        editor_object_idx++;
                    }
                }
                
                //handle loading in the shade if needed
                if (world_file_contents.shade != null) {
                    GameWorldObject shade = world_file_contents.shade;
                    Nightmare n = new Nightmare(Constant.player_tex, Constant.player_attack_tex, new Vector2(shade.x_position, shade.y_position), shade.scale, Constant.hit_confirm_spritesheet, player, editor_object_idx, "shade");
                    n.set_placement_source((int)ObjectPlacement.Level);
                    entities_list.Add(n);
                    collision_entities.Add(n);
                    enemies.Add(n);
                    editor_object_idx++;
                }

                //set ai entities for all ai entities
                set_ai_entities_for_all_ais();

                Console.WriteLine($"editor object idx:{editor_object_idx}");
            } else {
                throw new Exception("Cannot deserialize JSON world objects!");
            }
            
            Console.WriteLine($"editor_status:{editor_enabled}");
            if (editor_enabled) {
                Console.WriteLine("loading unloaded objects and textures for editor...");
                //find objects that are not present in the level that we need to load textures for due to editor being active
                var unloaded_objects = Constant.get_object_identifiers().Where(i => loaded_obj_identifiers.All(i2 => i2 != i));
                //load textures not present in the level
                foreach (string i in unloaded_objects) {
                    switch (i) {
                        case "player":
                            check_and_load_tex(ref Constant.player_tex, "sprites/test_player_spritesheet7");
                            check_and_load_tex(ref Constant.player_dash_tex, "sprites/test_player_dash_spritesheet1");
                            check_and_load_tex(ref Constant.player_attack_tex, "sprites/test_player_attacks_spritesheet6");
                            check_and_load_tex(ref Constant.player_heavy_attack_tex, "sprites/test_player_heavy_attack_spritesheet1");
                            check_and_load_tex(ref Constant.player_charging_tex, "sprites/test_player_charging_spritesheet1");
                            check_and_load_tex(ref Constant.player_aim_tex, "sprites/test_player_bow_aim_spritesheet1");
                            check_and_load_tex(ref Constant.arrow_tex, "sprites/arrow1");
                            check_and_load_tex(ref Constant.dash_icon, "sprites/dash_icon");
                            check_and_load_tex(ref Constant.sword_icon, "sprites/sword_icon");
                            check_and_load_tex(ref Constant.bow_icon, "sprites/bow_icon");
                            break;
                        case "tree":
                            check_and_load_tex(ref Constant.tree_spritesheet, "sprites/tree_spritesheet5");
                            break;
                        case "orange_tree":
                            check_and_load_tex(ref Constant.orange_tree, "sprites/orange_tree4_6_26");
                            break;
                        case "yellow_tree":
                            check_and_load_tex(ref Constant.yellow_tree, "sprites/yellow_tree1_4_26");
                            break;
                        case "green_tree":
                            check_and_load_tex(ref Constant.green_tree, "sprites/green_tree1_1_26");
                            break;
                        case "grass":
                            check_and_load_tex(ref Constant.short_grass_tex, "sprites/short_grass2_4");
                            check_and_load_tex(ref Constant.tall_grass_tex, "sprites/tall_grass2_9");
                            break;
                        case "sign":
                            check_and_load_tex(ref Constant.sign_tex, "sprites/sign1");
                            check_and_load_tex(ref Constant.read_interact_tex, "sprites/read_interact");
                            break;
                        case "ghastly":
                            check_and_load_tex(ref Constant.ghastly_tex, "sprites/ghastly1");
                            break;
                        case "marker":
                            check_and_load_tex(ref Constant.marker_spritesheet, "sprites/marker_spritesheet5");
                            break;
                        case "lamp":
                            check_and_load_tex(ref Constant.lamppost_spritesheet, "sprites/lamppost_spritesheet3");
                            break;
                        case "big_tile":
                            check_and_load_tex(ref Constant.tile_tex, "sprites/tile3");
                            break;
                        case "cracked_tile":
                            check_and_load_tex(ref Constant.tile_tex2, "sprites/tile4");
                            break;
                        case "reg_tile":
                            check_and_load_tex(ref Constant.tile_tex3, "sprites/tile5");
                            break;
                        case "round_tile":
                            check_and_load_tex(ref Constant.tile_tex4, "sprites/tile6");
                            break;
                        case "nightmare":
                            check_and_load_tex(ref Constant.nightmare_tex, "sprites/test_nightmare_spritesheet2");
                            check_and_load_tex(ref Constant.nightmare_attack_tex, "sprites/test_nightmare_attacks_spritesheet1");
                            break;
                        case "wall":
                            check_and_load_tex(ref Constant.wall_tex, "sprites/box_spritesheet1");
                            break;
                        case "fence":
                            check_and_load_tex(ref Constant.fence_spritesheet, "sprites/fence1");
                            break;
                        case "tan_tile":
                            check_and_load_tex(ref Constant.tan_tile_tex, "sprites/tile_tan1");
                            break;
                        case "grass_tile":
                            check_and_load_tex(ref Constant.grass_tile_tex, "sprites/grass_tile1");
                            break;
                        case "stone_tile":
                            check_and_load_tex(ref Constant.stone_tile_tex, "sprites/stone_tile3");
                            break;
                        case "flower":
                            check_and_load_tex(ref Constant.flower_tex, "sprites/flower1_1_12");
                            break;
                        case "grass2":
                            check_and_load_tex(ref Constant.short_grass_tex, "sprites/short_grass2_4");
                            check_and_load_tex(ref Constant.tall_grass_tex, "sprites/tall_grass2_9");
                            break;
                        case "trail_tile":
                            check_and_load_tex(ref Constant.trail_tex, "sprites/trail1");
                            break;
                        case "sand_tile":
                            check_and_load_tex(ref Constant.sand_tex, "sprites/sand1");
                            break;
                        case "sword":
                            check_and_load_tex(ref Constant.sword_tex, "sprites/sword1_22");
                            break;
                        case "box":
                            check_and_load_tex(ref Constant.box_spritesheet, "sprites/box1_2_18");
                            break;
                        case "house":
                            check_and_load_tex(ref Constant.house_spritesheet, "sprites/house2_1_54");
                            break;
                        case "scarecrow":
                            check_and_load_tex(ref Constant.scarecrow_tex, "sprites/scarecrow1");
                            break;
                        case "ghost":
                            check_and_load_tex(ref Constant.ghastly_tex, "sprites/ghastly1");
                            check_and_load_tex(ref Constant.sludge_tex, "sprites/sludge1");
                            break;
                        case "hitswitch":
                            check_and_load_tex(ref Constant.switch_active, "sprites/switch2_active_8");
                            check_and_load_tex(ref Constant.switch_inactive, "sprites/switch2_inactive_8");
                            break;
                        case "dash_cloak":
                            check_and_load_tex(ref Constant.dash_cloak_pickup_tex, "sprites/dash_cloak_pickup");
                            break;
                        case "cracked_rocks":
                            check_and_load_tex(ref Constant.cracked_rocks_spritesheet, "sprites/cracked_rocks_4");
                            break;
                        case "player_chip":
                            //load player chip
                            check_and_load_tex(ref Constant.player_chip_tex, "sprites/player_chip");
                            //load player assets because they will be needed later
                            check_and_load_tex(ref Constant.player_tex, "sprites/test_player_spritesheet7");
                            check_and_load_tex(ref Constant.player_dash_tex, "sprites/test_player_dash_spritesheet1");
                            check_and_load_tex(ref Constant.player_attack_tex, "sprites/test_player_attacks_spritesheet6");
                            check_and_load_tex(ref Constant.player_heavy_attack_tex, "sprites/test_player_heavy_attack_spritesheet1");
                            check_and_load_tex(ref Constant.player_charging_tex, "sprites/test_player_charging_spritesheet1");
                            check_and_load_tex(ref Constant.player_aim_tex, "sprites/test_player_bow_aim_spritesheet1");
                            check_and_load_tex(ref Constant.arrow_tex, "sprites/arrow1");
                            break;
                        case "bow":
                            check_and_load_tex(ref Constant.bow_pickup_tex, "sprites/bow_pickup");
                            break;
                        case "haunter":
                            check_and_load_tex(ref Constant.haunter_tex, "sprites/haunter1");
                            check_and_load_tex(ref Constant.hex1_tex, "sprites/hex1");
                            check_and_load_tex(ref Constant.haunter_attack_tex, "sprites/haunter_attack1");
                            break;
                        case "checkpoint":
                            check_and_load_tex(ref Constant.checkpoint_marker_spritesheet, "sprites/checkpoint_spritesheet1_15");
                            break;
                        case "skeleton":
                            check_and_load_tex(ref Constant.skelly_tex, "sprites/skelly_spritesheet1");
                            check_and_load_tex(ref Constant.skelly_attack_tex, "sprites/skelly_attacks_spritesheet1");
                            check_and_load_tex(ref Constant.skelly_attack_charge_tex, "sprites/skelly_charge_attack_spritesheet1");
                            break;
                        case "shadowknight":
                            check_and_load_tex(ref Constant.shadow_knight_tex, "sprites/shadow_knight_spritesheet1");
                            check_and_load_tex(ref Constant.shadow_knight_attack_tex, "sprites/shadow_knight_attacks_spritesheet1");
                            check_and_load_tex(ref Constant.shadow_knight_charge_attack_tex, "sprites/shadow_knight_charge_attack_spritesheet1");
                            break;
                        case "gate":
                            check_and_load_tex(ref Constant.gate_spritesheet, "sprites/closed_gate1_28");
                            break;
                        case "green_wall":
                            check_and_load_tex(ref Constant.green_wall_tex, "sprites/green_box2_8");
                            break;
                        case "torii":
                            check_and_load_tex(ref Constant.torii_spritesheet, "sprites/torii_gate_43");
                            break;
                        default:
                            //don't load anything
                            Console.WriteLine($"WARNING: unknown object({i}) in Constant.get_object_identifiers()");
                            break;
                    }
                }

                editor_tiles_map = new Dictionary<int, (Texture2D, string, int)>() {
                    {0, (Constant.grass_tile_tex, "grass_tile", (int)DrawWeight.Heavy)},
                    {1, (Constant.stone_tile_tex, "stone_tile", (int)DrawWeight.Heavy)}
                };
                
                editor_init();
                Console.WriteLine("initialized editor");
            }

            Console.WriteLine("Created all world objects");

            //reset camera on level load
            camera.Rotation = 0f;
            //sort the entities list once so things are not drawn out of order in terms of depth values
            entities_list.sort_list_by_depth(camera.Rotation, player.get_base_position(), render_distance);
            //set post level load
            post_level_load_script_trigger = true;

            //update level loaded count
            //set/update level loaded dictionary with level currently being loaded
            update_level_loaded_map(world_file_contents.level_loaded_count, level_id);
            //print level loaded map
            Console.WriteLine("level_loaded_map:");
            foreach (KeyValuePair<string, int> kv in level_loaded_map) {
                Console.WriteLine($"{kv.Key}-{kv.Value}");
            }
        }

        public void set_ai_entities_for_all_ais() {
            //gather all ai entities and set references for each ai entity
            List<IAiEntity> all_ai_entities = new List<IAiEntity>();
            foreach (IAiEntity ai in enemies) { all_ai_entities.Add(ai); }
            foreach (IAiEntity ai in npcs) { all_ai_entities.Add(ai); }
            //set fellow enemies for all ais (this is so each entity knows where the others are to avoid overlaps)
            foreach (IAiEntity ai in enemies) { ai.set_ai_entities(all_ai_entities); }
            foreach (IAiEntity ai in npcs) { ai.set_ai_entities(all_ai_entities); }
        }

        public void upsert_condition_tags(string tag_id, bool value) {
            //check empty string and null
            if (tag_id == null || tag_id.Length == 0) {
                return;
            }

            if (condition_tags.ContainsKey(tag_id)) {
                //update
                condition_tags[tag_id] = value;
            } else {
                //add
                condition_tags.Add(tag_id, value);
            }
        }
        #endregion

        private void check_and_load_tex(ref Texture2D tex, string texture_path) {
            if (!loaded_textures.Contains(texture_path)) {
                //add to loaded textures
                loaded_textures.Add(texture_path);
                tex = this.Content.Load<Texture2D>(texture_path);
                Console.WriteLine($"Loaded texture from path: {texture_path}");
            }
        }

        public void unload_level() {
            Console.WriteLine("clearing and unloading objects...");
            //clear effects
            game.set_pixel_shader_active(false);
            //clear out all object lists
            plants.Clear();
            collision_entities.Clear();
            foreground_entities.Clear();
            background_entities.Clear();
            floor_entities.Clear();
            temp_tiles.Clear();
            collision_geometry.Clear();
            triggers.Clear();
            entities_list.Clear();
            enemies.Clear();
            projectiles.Clear();
            condition_manager.clear_conditions();
            collision_geometry_map.Clear();
            collision_tile_map.Clear();
            switches.Clear();
            lights.Clear();
            light_excluded_entities.Clear();
            condition_tags.Clear();
            dropped_items.Clear();
            condition_tags.Clear();
            switches.Clear();
            //do not need to clear or nullify world script parser as it is set to a new object on every level load
            explosion_list.Clear();

            //clear editor only objects
            editor_only_objects.Clear();
            editor_floor_tile_map.Clear();
            editor_tiles_map.Clear();
            //clear sound manager
            sound_manager.clear();
            //clear entities
            clear_entities();
            //unload content
            Content.Unload();
            loaded_textures.Clear();

            //set world variables if needed on unload
            rotation_active = false;
        }

        private void update_level_loaded_map(int level_loaded_count, string level_id) {
            //clean level id if needed
            string stripped_level_id = level_id.StartsWith(Constant.level_mod_prefix) ? level_id.Substring(Constant.level_mod_prefix.Length) : level_id;
            if (level_loaded_map.ContainsKey(stripped_level_id)) {
                //we already have the level id in the map so increase the count
                int level_loaded_value = level_loaded_map[stripped_level_id];
                //if the value from the dictionary is not the same as the passed in count, take the parameter
                if (level_loaded_value != level_loaded_count) {
                    level_loaded_map[stripped_level_id] = (level_loaded_count + 1);
                } else {
                    level_loaded_map[stripped_level_id] = (level_loaded_value + 1);
                }
            } else {
                //must be the first time level is being loaded, add it to dictionary
                level_loaded_map.Add(stripped_level_id, 1);
            }

            //save modified file with updated count
            //we actually want to save with the mod prefix here
            //because we will be able to read it from the mod file in the load level function the next time we enter this level
            save_world_level_to_file(
                entities_list.get_all_entities().Keys.ToList(),
                foreground_entities,
                background_entities,
                level_loaded_map[current_level_id],
                Constant.level_mod_prefix + current_level_id
            );
        }

        public void Update(GameTime gameTime){
            //calculate fps
            fps = 1 / (float)gameTime.ElapsedGameTime.TotalSeconds;

            //handle freeze frames
            if (freeze_frames >= 0) {
                freeze_frames--;
                return;
            }

            //keep level transitions updated
            if (next_level_id != null) {
                level_transition(gameTime, next_level_id, current_level_trigger);
            }
            
            //keep goal rotations updated
            update_goal_rotation(gameTime);
            
            //check whether to execute world scripts post level load
            if (post_level_load_script_trigger) {
                //post level load run on_load world script
                world_script_parser.execute_on_load_script(gameTime, level_loaded_map[current_level_id], world_script_parser.get_on_load_script().Where(command => (command.level_load_count_required == level_loaded_map[current_level_id] || command.level_load_count_required == -1)).ToList());
                //set post level load to the value of the script finishing execution
                post_level_load_script_trigger = !world_script_parser.finished_execution();
            }

            //check whether to execute gameplay world scripts
            if (gameplay_script_trigger && current_script_trigger != null) {
                //run gameplay script)
                world_script_parser.execute_gameplay_trigger_script(gameTime, current_script_trigger, world_script_parser.get_on_gameplay_script(current_script_trigger.get_obj_ID_num()));
                //set gameplay script trigger based on whether or not the script parser has finished the execution
                gameplay_script_trigger = !world_script_parser.finished_execution();
                //if gameplay script trigger is set to false after checking for finished execution
                //invalidate script trigger
                if (!gameplay_script_trigger) {
                    //set focused trigger previously activated to true
                    if (!current_script_trigger.get_previously_activated()) {
                        //save the level, because now the script trigger previously activated value is true
                        //if it already has been previously activated of true we don't need to save the file to reset it
                        current_script_trigger.set_previously_activated(true);
                        //save the world level file with the newly updated script trigger value
                        save_world_level_to_file(
                            entities_list.get_all_entities().Keys.ToList(),
                            foreground_entities,
                            background_entities,
                            level_loaded_map[current_level_id],
                            Constant.level_mod_prefix + current_level_id
                        );
                    }
                    //set focused script trigger to null
                    current_script_trigger = null;
                }
            }

            //handle textbox
            if ((current_sign != null || current_npc != null) && current_textbox != null) {
                //update the current sign (which will update the current textbox)
                if (current_sign != null) {
                    current_sign.Update(gameTime, camera.Rotation);
                } else if (current_npc != null) {
                    current_npc.Update(gameTime, camera.Rotation);
                }
                player.update_animation(gameTime);
                player.update_particle_systems(gameTime, camera.Rotation);

                //check for end of text
                if (current_textbox.text_ended()) {
                    //reset textbox
                    current_textbox.reset();
                    //remove reference to current_sign, current_textbox and current_npc
                    current_sign = null;
                    current_textbox = null;
                    current_npc = null;
                    //reset the players dash cooldown to avoid input clash
                    player.reset_dash_cooldown(0f);
                }
                //pause the rest of the game world as the player reads the textbox
                return;
            }

            #region update render entities
            //UPDATE RENDERLIST ENTITIES
            //update active entities (also updates some collision entities)
            Constant.profiler.start("world_entities_update");
            for (int i = 0; i < entities_list.get_entities().Count; i++) {
                IEntity e = entities_list.get_entities()[i];
                if (e.get_flag().Equals(Constant.ENTITY_ACTIVE)) {
                    e.Update(gameTime, camera.Rotation);
                }
            }
            Constant.profiler.end("world_entities_update");
            #endregion

            foreach (IAiEntity e in enemies) {
                if (e is Nightmare) {
                    Nightmare n = (Nightmare)e;
                    if (n.is_dead()) {
                        entities_to_clear.Add(n);
                    }
                }
            }

            //handle clearing arrows
            foreach (IEntity e in projectiles) {
                if (e is Arrow) {
                    Arrow a = (Arrow)e;
                    if (a.is_dead()) {
                        entities_to_clear.Add(e);
                    }
                } else if (e is Grenade) {
                    Grenade g = (Grenade)e;
                    if (g.is_dead()) {
                        //explosion
                        //add to explosion list (point of origin for explosion, radius of explosion)
                        explosion_list.Add((g, e.get_base_position(), Constant.grenade_radius));
                        //play sound
                        play_spatial_sfx(Constant.explosion_sfx, e.get_base_position(), 0f, get_render_distance());
                        //clear grenade entity
                        entities_to_clear.Add(e);
                        //particle effects with noise
                        particle_systems.Add(new ParticleSystem(true, Constant.rotate_point(e.get_base_position(), camera.Rotation, 1f, Constant.direction_up), 8, 400f, 2, 8, 1, 6, Constant.white_particles, new List<Texture2D>() { Constant.footprint_tex }));
                        particle_systems.Add(new ParticleSystem(true, Constant.add_random_noise(Constant.rotate_point(e.get_base_position(), camera.Rotation, 1f, Constant.direction_up), (float)random.NextDouble(), (float)random.NextDouble()*50f), 8, 400f, 2, 8, 1, 6, Constant.white_particles, new List<Texture2D>() { Constant.footprint_tex }));
                        particle_systems.Add(new ParticleSystem(true, Constant.add_random_noise(Constant.rotate_point(e.get_base_position(), camera.Rotation, 1f, Constant.direction_up), (float)random.NextDouble(), (float)random.NextDouble()*60f), 8, 400f, 2, 8, 1, 6, Constant.white_particles, new List<Texture2D>() { Constant.footprint_tex }));
                        //shake camera a bit more
                        set_camera_shake(Constant.camera_shake_milliseconds*2, Constant.camera_shake_angle*3, Constant.camera_shake_hit_radius*2);
                    }
                }
            }

            foreach (IEntity g in collision_geometry) {
                if (g is InvisibleObject) {
                    g.Update(gameTime, camera.Rotation);
                }
            }

            //update plants
            update_forest_geometry(gameTime, camera.Rotation);

            //update conditions
            condition_manager.Update(gameTime, camera.Rotation, mouse_hitbox);

            if (player.is_moving()){
                /*
                * TODO: potential improvement
                * Could be to instead of sorting every element, just move the player 
                * up and down the linked list based on their depth value so as to not 
                * sort things that don't move like trees
                * NOTE(5-6-2024):This probably won't work because when rotation happens
                * we would need to re-sort everything anyways. However, with a static scene
                * this approach actually could work well
                */
                //Depth sort while player is moving as well
                //render distance re-calculation for entities happens within the sort_list_by_distance function
                entities_list.sort_list_by_depth(camera.Rotation, player.get_base_position(), render_distance);
            }
            
            //sort on camera position if not tethered
            if (!player_camera_tethered) {
                entities_list.sort_list_by_depth(camera.Rotation, camera.get_camera_center(), render_distance);
            }

            #region player_death
            if (player.get_health() <= 0) {
                //player has died, transition and reload to the saved checkpoint level
                //transition on player death
                if (!transition_active) {
                    //set shade for current level if there is not a current shade
                    if (object_persistence) {
                        if (shade_level_id != null) {
                            //shade already exists in a level, need to delete that shade and set a new one for the current level and player
                            remove_shade_from_level_file(shade_level_id);
                        }
                        spawn_shade();
                    }

                    if (checkpoint_level_id == null) {
                        //reload current level
                        set_transition(true, current_level_id, null);
                    } else {
                        set_transition(true, checkpoint_level_id, null);
                    }
                }
            }

            if (Keyboard.GetState().IsKeyDown(Keys.B)) {
                set_transition(true, current_level_id, null);
            }
            #endregion

            //handle player idle by rotating the camera slowly
            if (player.is_idle() && !editor_active) {
                //update the camera rotation
                camera.Rotation += 0.0005f;
                //keep the plants updated (watered lol)
                for (int i = 0; i < plants.Count; i++){
                    plants[i].Update(gameTime, camera.Rotation);
                }
                foreach (ForegroundEntity f in foreground_entities) {
                    f.Update(gameTime, camera.Rotation);
                }
                //calculate rotation in degrees
                rotation = MathHelper.ToDegrees(MathHelper.WrapAngle(camera.Rotation));
                //if the camera is rotating then sort entities for depth
                entities_list.sort_list_by_depth(camera.Rotation, player.get_base_position(), render_distance);
            }

            // Camera rotation and zoom controls
            if (Keyboard.GetState().IsKeyDown(Keys.Up))
                camera.Zoom += 0.01f;
            else if (Keyboard.GetState().IsKeyDown(Keys.Down))
                camera.Zoom -= 0.01f;
            
            if (Mouse.GetState().ScrollWheelValue < previous_scroll_value) {
                camera.Zoom -= 0.1f;
            } else if (Mouse.GetState().ScrollWheelValue > previous_scroll_value) {
                camera.Zoom += 0.1f;
            }
            previous_scroll_value = Mouse.GetState().ScrollWheelValue;

            //flag to allow or disallow players rotating the camera
            if (player_camera_rotate_enabled) {
                if (!GamePad.GetState(PlayerIndex.One).IsConnected) { //keyboard camera rotation control
                    if (Keyboard.GetState().IsKeyDown(Keys.Left)) {
                        camera.Rotation += 0.02f;
                        previous_key = Keys.Left;
                    } else if (Keyboard.GetState().IsKeyDown(Keys.Right)) {
                        camera.Rotation -= 0.02f;
                        previous_key = Keys.Right;
                    }
                } else {
                    //gamepad connected
                    //pull gamepad right thumbstick value
                    camera_gamepad_h_input = GamePad.GetState(PlayerIndex.One).ThumbSticks.Right.X;
                    //clamp to range between -0.02f and +0.02f
                    camera_gamepad_h_input *= 0.02f;
                    //add to rotation
                    camera.Rotation -= camera_invert ? -camera_gamepad_h_input : camera_gamepad_h_input;
                    //set previous key for editor
                    if (camera_gamepad_h_input < 0) previous_key = Keys.Left;
                    else if (camera_gamepad_h_input > 0) previous_key = Keys.Right;
                }
            }

            //update animations outside of camera rotations
            foreach (IEntity plant in plants){
                if (Vector2.Distance(plant.get_base_position(), player.get_base_position()) < render_distance){
                    plant.update_animation(gameTime);
                }
            }

            // updates that need to happen when the camera is moving
            if (Keyboard.GetState().IsKeyDown(Keys.Left) || 
                Keyboard.GetState().IsKeyDown(Keys.Right) || 
                GamePad.GetState(PlayerIndex.One).ThumbSticks.Right.X > 0.1f || 
                GamePad.GetState(PlayerIndex.One).ThumbSticks.Right.X < -0.1f) {
                
                //calculate rotation in degrees
                rotation = MathHelper.ToDegrees(MathHelper.WrapAngle(camera.Rotation));
                
                //if the camera is rotating then sort entities for depth
                entities_list.sort_list_by_depth(camera.Rotation, player.get_base_position(), render_distance);
            } else if (Keyboard.GetState().IsKeyDown(Keys.E) && previous_key != Keys.E) {
                previous_key = Keys.E;
                //editor mode swap current state
                editor_active = !editor_active;
                editor_object_rotation = 0;
                editor_object_scale = 1f;
                bool ai_behavior_enabled;
                //if active
                if (editor_active) {
                    //set camera rotation to zero
                    camera.Rotation = 0f;
                    //push an update to plants and foreground entities
                    update_forest_geometry(gameTime, camera.Rotation);
                    ai_behavior_enabled = false;

                    //turn off pixel shader
                    this.game.set_pixel_shader_active(false);
                } else {
                    ai_behavior_enabled = true;
                    //set enemies references for all ais
                    foreach (IAiEntity ai in enemies) {
                        ai.set_ai_entities(enemies);
                    }

                    //turn pixel shader back on
                    this.game.set_pixel_shader_active(true);
                }
                //re-enable enemy ai
                foreach (IAiEntity ai in enemies) {
                    ai.set_behavior_enabled(ai_behavior_enabled);
                }
                //push an update to invisible_objects
                update_invisible_objects(editor_active);
            }
            
            #region editor_code
            //handle world editing
            if (editor_active) {
                //set camera rotation to zero
                //camera.Rotation = 0f;
                //increased elapsed for cooldown
                selection_elapsed += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
                //keep track of mouse position

                mouse_world_position = Vector2.Transform(new Vector2(Mouse.GetState().X, Mouse.GetState().Y), Matrix.Invert(camera.Transform));
                create_position = mouse_world_position;
                mouse_hitbox.update(rotation, mouse_world_position);

                //keep selected object updated
                IEntity selected_entity = obj_map[selected_object];
                //modify selected entity position to tiled position if M key is held
                if (Keyboard.GetState().IsKeyDown(Keys.M)) {
                    selected_entity.set_base_position(
                        (new Vector2(
                            (float)Math.Floor(create_position.X / 32f),
                            (float)Math.Floor(create_position.Y / 32f)
                        )) * 32f
                    );
                } else {
                    selected_entity.set_base_position(mouse_world_position);
                }
                selected_entity.Update(gameTime, camera.Rotation);
                
                //scroll through available editor tools
                if (Keyboard.GetState().IsKeyDown(Keys.D1) && selection_elapsed >= selection_cooldown) {
                    editor_tool_idx--;
                    selection_elapsed = 0f;
                } else if (Keyboard.GetState().IsKeyDown(Keys.D2) && selection_elapsed >= selection_cooldown) {
                    editor_tool_idx++;
                    selection_elapsed = 0f;
                }
                //scroll through available editor layers
                if (Keyboard.GetState().IsKeyDown(Keys.D3) && selection_elapsed >= selection_cooldown) {
                    editor_layer--;
                    selection_elapsed = 0f;
                } else if (Keyboard.GetState().IsKeyDown(Keys.D4) && selection_elapsed >= selection_cooldown) {
                    editor_layer++;
                    selection_elapsed = 0f;
                }
                //scroll through the objects available
                if (Keyboard.GetState().IsKeyDown(Keys.I) && selection_elapsed >= selection_cooldown) {
                    selected_object--;
                    selection_elapsed = 0f;
                    editor_object_rotation = 0;
                    editor_object_scale = 1f;
                } else if (Keyboard.GetState().IsKeyDown(Keys.O) && selection_elapsed >= selection_cooldown) {
                    selected_object++;
                    selection_elapsed = 0f;
                    editor_object_rotation = 0;
                    editor_object_scale = 1f;
                }
                //scroll through tiles available
                if (Keyboard.GetState().IsKeyDown(Keys.D5) && selection_elapsed >= selection_cooldown) {
                    editor_tile_idx--;
                    selection_elapsed = 0f;
                } else if (Keyboard.GetState().IsKeyDown(Keys.D6) && selection_elapsed >= selection_cooldown) {
                    editor_tile_idx++;
                    selection_elapsed = 0f;
                }
                
                //handle object rotation for editor objects
                if (Keyboard.GetState().IsKeyDown(Keys.OemOpenBrackets)) {
                    editor_object_rotation += 10f;
                } else if (Keyboard.GetState().IsKeyDown(Keys.OemCloseBrackets)) {
                    editor_object_rotation -= 10f;
                }
                selected_entity.set_rotation_offset(MathHelper.ToDegrees(editor_object_rotation));
                //handle object scale for editor objects
                if (Keyboard.GetState().IsKeyDown(Keys.OemSemicolon)) {
                    editor_object_scale -= 0.1f;
                } else if (Keyboard.GetState().IsKeyDown(Keys.OemQuotes)) {
                    editor_object_scale += 0.1f;
                }
                //prevent negative scale
                if (editor_object_scale <= 0f) {
                    editor_object_scale = 0f;
                }
                selected_entity.set_scale(editor_object_scale);
                
                //wrap editor tool
                if (editor_tool_idx >= editor_tool_name_map.Count) {
                    Console.WriteLine("wrap selection down");
                    editor_tool_idx = 0;
                } else if (editor_tool_idx < 0) {
                    Console.WriteLine("wrap selection up");
                    editor_tool_idx = editor_tool_name_map.Count - 1;
                }
                //wrap editor layer
                if (editor_layer >= editor_layer_count) {
                    editor_layer = 0;
                } else if (editor_layer < 0) {
                    editor_layer = editor_layer_count - 1;
                }
                //wrap selected object
                if (selected_object > obj_map.Count) {
                    Console.WriteLine("wrap selection down");
                    selected_object = 1;
                } else if (selected_object <= 0) {
                    Console.WriteLine("wrap selection up");
                    selected_object = obj_map.Count;
                }
                //wrap selected tile
                if (editor_tile_idx >= editor_tiles_map.Count) {
                    editor_tile_idx = 0;
                } else if (editor_tile_idx < 0) {
                    editor_tile_idx = editor_tiles_map.Count - 1;
                }

                if (Keyboard.GetState().IsKeyDown(Keys.P)) {
                    Console.WriteLine("mouse_world_position:" + mouse_world_position);
                    Console.WriteLine("create_position:" + create_position);
                }
                
                //only able to place objects if the editor_tool_idx == 0 (object placement tool)
                if (editor_tool_idx == 0 && (Mouse.GetState().LeftButton == ButtonState.Pressed) && selection_elapsed >= selection_cooldown) {
                    selection_elapsed = 0f;
                    MouseState mouse_state = Mouse.GetState();
                    //mouse_world_position = Constant.world_position_transform(new Vector2(Mouse.GetState().X, Mouse.GetState().Y), camera);
                    //snap functionality for tiles
                    if (Keyboard.GetState().IsKeyDown(Keys.M)) {
                        //modify and snap create position
                        create_position = (new Vector2(
                            (float)Math.Floor(create_position.X / 32f),
                            (float)Math.Floor(create_position.Y / 32f)
                        )) * 32f;
                    }
                    previous_key = Keys.M;
                    //rotate point with rotation value
                    //create_position = Constant.rotate_point(mouse_world_position, -camera.Rotation, 0f, Constant.direction_down);
                    Console.WriteLine("mouse_world_position:" + mouse_world_position);
                    Console.WriteLine("create_position:" + create_position);
                    place_object(gameTime, camera.Rotation, selected_object, create_position, editor_object_rotation, (int)ObjectPlacement.Level);
                    Console.WriteLine($"Created object idx:{editor_object_idx}");
                    editor_object_idx++;
                } else if (Keyboard.GetState().IsKeyDown(Keys.S) && Keyboard.GetState().IsKeyDown(Keys.LeftControl) && selection_elapsed >= selection_cooldown) { //Ctrl+S
                    //pull all world entities from render list (whether or not they're currently being drawn)
                    List<IEntity> all_world_entities = entities_list.get_all_entities().Keys.ToList();
                    //save
                    save_world_level_to_file(all_world_entities, foreground_entities, background_entities, level_loaded_map[current_level_id], save_file_name);
                } else if (editor_tool_idx == 1) {
                    //condition tool
                    //right click
                    if (Mouse.GetState().RightButton == ButtonState.Pressed) {
                        //context for right mouse button pressed
                        //check if mouse hitbox is within hitbox for condition
                        ICondition c = condition_manager.find_condition_colliding(mouse_hitbox);
                        if (c != null) {
                            //collision
                            if (c is EnemiesDeadRemoveObjCondition) {
                                EnemiesDeadRemoveObjCondition edroc = (EnemiesDeadRemoveObjCondition)c;
                                edroc.set_selected(true);
                                selected_condition = edroc;
                            } else if (c is SwitchCondition) {
                                SwitchCondition s_cond = (SwitchCondition)c;
                                s_cond.set_selected(true);
                                selected_condition = s_cond;
                            }
                        }
                    
                        //check for mouse collision with entity here
                        //update connections if need be based on selected condition and function modes of condition
                        IEntity ce = find_entity_colliding(mouse_hitbox);
                        if (ce != null && selected_condition != null) {
                            //check function mode and connect/disconnect condition
                            if (selected_condition is EnemiesDeadRemoveObjCondition) {
                                EnemiesDeadRemoveObjCondition edroc = (EnemiesDeadRemoveObjCondition)selected_condition;
                                int function_mode = edroc.get_function_mode();
                                //this check is to know the difference in what to connect or remove from with regards to the condition
                                //if we click on an enemy/aientity we probably want that to be the input to the condition
                                //on the other hand if we don't click on an enemy/aientity then we probably want it to be the output of the condition
                                if (ce is IAiEntity) {
                                    //connect or disconnect from enemy ids
                                    if (function_mode == 0) {
                                        //connect
                                        edroc.add_enemy_id_to_check(ce.get_obj_ID_num());
                                    } else {
                                        //disconnect
                                        edroc.remove_enemy_id_from_check(ce.get_obj_ID_num());
                                    }
                                } else {
                                    //connect or disconnect from objs to remove
                                    if (function_mode == 0) {
                                        //connect
                                        edroc.add_obj_to_remove(ce.get_obj_ID_num());
                                    } else {
                                        //disconnect
                                        edroc.remove_obj_to_remove(ce.get_obj_ID_num());
                                    }
                                }
                            } else if (selected_condition is SwitchCondition) {
                                Console.WriteLine("selected condition is switch condition and colliding entity is not null");
                                SwitchCondition sw_cond = (SwitchCondition)selected_condition;
                                int function_mode = sw_cond.get_function_mode();
                                if (ce is HitSwitch) {
                                    //connect or disconnect from switches
                                    if (function_mode == 0) {
                                        //connect
                                        sw_cond.add_switch_id_to_check(ce.get_obj_ID_num());
                                    } else {
                                        //disconnect
                                        sw_cond.remove_switch_id_from_check(ce.get_obj_ID_num());
                                    }
                                } else {
                                    //connect or disconnect from objs to remove
                                    if (function_mode == 0) {
                                        //connect
                                        sw_cond.add_obj_to_remove(ce.get_obj_ID_num());
                                    } else {
                                        //disconnect
                                        sw_cond.remove_obj_to_remove(ce.get_obj_ID_num());
                                    }
                                }
                            }
                        }
                    }
                } else if (editor_tool_idx == 2) {
                    //delete tool
                    if (Mouse.GetState().RightButton == ButtonState.Pressed) {
                        //can use basic find entity colliding since plants are now stacked objects with collision
                        //find collision entity
                        IEntity mouse_collision_entity = find_entity_colliding(mouse_hitbox);
                        ParticleSystem mouse_collision_ps = find_particle_system_colliding(mouse_hitbox);
                        switch (editor_layer) {
                            case 0:
                                //floor layer
                                FloorEntity fe = find_floor_entity_colliding(mouse_hitbox);
                                if (fe is Tile) {
                                    Tile t = (Tile)fe;
                                    if (floor_entities.Contains(fe)) {
                                        //remove
                                        clear_entity(t);
                                        //check tile and remove from map
                                        Vector2 tile_position = (new Vector2(
                                            (float)Math.Floor(create_position.X / 64f),
                                            (float)Math.Floor(create_position.Y / 64f)
                                        )) * 64f;
                                        if (editor_floor_tile_map.ContainsKey(tile_position)) {
                                            editor_floor_tile_map.Remove(tile_position);
                                        }
                                    }
                                }
                                break;
                            case 1:
                                //collision layer
                                //same thing for all objects with collision
                                if (collision_geometry.Contains(mouse_collision_entity) || 
                                    collision_entities.Contains(mouse_collision_entity) || 
                                    plants.Contains(mouse_collision_entity)) {
                                    //remove
                                    clear_entity(mouse_collision_entity);
                                }
                                //handle collision with a particle system
                                if (particle_systems.Contains(mouse_collision_ps)) {
                                    dead_particle_systems.Add(mouse_collision_ps);
                                }
                                break;
                            default:
                                break;
                        }
                    }
                } else if (editor_tool_idx == 3) {
                    //tile tool
                    if (Mouse.GetState().LeftButton == ButtonState.Pressed && selection_elapsed >= selection_cooldown) {
                        selection_elapsed = 0f;
                        MouseState mouse_state = Mouse.GetState();
                        //calculate 10 tiles to the right and up
                        //Vector2 tile_start = new Vector2(create_position.X - 10*64, create_position.Y - 10*64);
                        Vector2 tile_start = (new Vector2(
                            (float)Math.Floor(create_position.X / 64f),
                            (float)Math.Floor(create_position.Y / 64f)
                        )) * 64f;
                        //iterate over 10 tiles and place them
                        for (int i = 0; i < 10; i++) {
                            for (int j = 0; j < 10; j++) {
                                //compute tile position
                                Vector2 tile_position = new Vector2(tile_start.X + i*64, tile_start.Y + j*64);
                                //check dictionary and add to level
                                if (!editor_floor_tile_map.ContainsKey(tile_position)) {
                                    Tile tile = new Tile(tile_position, editor_object_scale, editor_tiles_map[editor_tile_idx].Item1, editor_tiles_map[editor_tile_idx].Item2, editor_tiles_map[editor_tile_idx].Item3, editor_object_idx);
                                    add_floor_entity(tile);
                                    editor_floor_tile_map.Add(tile_position, editor_tile_idx);
                                    Console.WriteLine($"{editor_tiles_map[editor_tile_idx].Item2},{tile_position.X},{tile_position.Y},{editor_object_scale}");
                                    editor_object_idx++;
                                }
                            }
                        }
                    }
                } else if (editor_tool_idx == 4) {
                    //brush tool for trees
                    //update rectangle to generate trees in
                    brush_box.update(camera.Rotation, mouse_world_position);
                    //check for click
                    if (Mouse.GetState().LeftButton == ButtonState.Pressed && selection_elapsed >= selection_cooldown) {
                        selection_elapsed = 0f;
                        MouseState mouse_state = Mouse.GetState();
                        //generate random points within that box
                        List<Vector2> generated_points = generate_list_of_points(brush_box, 14, 25f);
                        int tree_type = 0;
                        foreach (Vector2 p in generated_points) {
                            float random_rotation = (float)random.Next(0, 360);
                            int selected_tree_obj;
                            switch (tree_type) {
                                case 0:
                                    selected_tree_obj = 18;
                                    break;
                                case 1:
                                    selected_tree_obj = 19;
                                    break;
                                case 2:
                                    selected_tree_obj = 20;
                                    break;
                                default:
                                    selected_tree_obj = 20;
                                    break;
                            }
                            place_object(gameTime, camera.Rotation, selected_tree_obj, p, random_rotation, (int)ObjectPlacement.Level);
                            editor_object_idx++;
                            tree_type++;
                            if (tree_type >= 3) {
                                tree_type = 0;
                            }
                        }
                    }
                } else if (editor_tool_idx == 5) {
                    //lock tile tool
                    if (Mouse.GetState().LeftButton == ButtonState.Pressed && selection_elapsed >= selection_cooldown) {
                        selection_elapsed = 0f;
                        //calculate tile position
                        Vector2 tile_position = (new Vector2(
                            (float)Math.Floor(create_position.X / 64f),
                            (float)Math.Floor(create_position.Y / 64f)
                        )) * 64f;
                        //only add to the floor in this certain spot if there is not a tile there already
                        if (!editor_floor_tile_map.ContainsKey(tile_position)) {
                            Tile tile = new Tile(tile_position, editor_object_scale, editor_tiles_map[editor_tile_idx].Item1, editor_tiles_map[editor_tile_idx].Item2, editor_tiles_map[editor_tile_idx].Item3, editor_object_idx);
                            add_floor_entity(tile);
                            //add to tile map
                            editor_floor_tile_map.Add(tile_position, editor_tile_idx);
                            Console.WriteLine($"{editor_tiles_map[editor_tile_idx].Item2},{tile_position.X},{tile_position.Y},{editor_object_scale}");
                            editor_object_idx++;
                        }
                    }
                }
            }
            #endregion

            //update temp tiles
            Constant.profiler.start("world_temp_tiles_update");
            update_temp_tiles(gameTime, camera.Rotation);
            Constant.profiler.end("world_temp_tiles_update");

            //update particle systems
            Constant.profiler.start("world_particle_systems_update");
            update_world_particle_systems(gameTime, camera.Rotation);
            Constant.profiler.end("world_particle_systems_update");

            //check collisions
            Constant.profiler.start("world_check_entity_collisions_update");
            check_entity_collision(gameTime);
            Constant.profiler.end("world_check_entity_collisions_update");

            //update and process explosions
            Constant.profiler.start("world_process_explosions_update");
            process_explosions(gameTime);
            Constant.profiler.end("world_process_explosions_update");

            //update dropped items
            Constant.profiler.start("world_update_dropped_items_update");
            update_dropped_items(gameTime, camera.Rotation);
            Constant.profiler.end("world_update_dropped_items_update");
            
            //clear entities
            clear_entities();

            //check trigger volumes
            Constant.profiler.start("world_check_triggers_update");
            check_triggers(gameTime, camera.Rotation);
            Constant.profiler.end("world_check_triggers_update");

            //shake camera update
            shake_camera(gameTime);

            //dashing camera shake
            check_player_dash_camera_shake();

            //update camera
            if (player_camera_tethered) {
                camera.Update(player.get_camera_track_position());
            }

            //update sound manager
            sound_manager.Update(gameTime, camera.Rotation);
            
            //update camera bounds
            //update_camera_bounds();

            //keep the lights on lol
            foreach (Light light in lights) {
                light.Update(gameTime, camera.Rotation);
            }

            //intro text timer update
            if (intro_text_playing) {
                //increase timer for intro text
                intro_text_elapsed += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
            }
            
            Constant.profiler.print_frame_summary();
        }

        /*editor tooling*/
        public void place_object(GameTime gameTime, float rotation, int selected_obj, Vector2 create_position, float editor_object_rotation, int placement_source) {
            switch (selected_obj) {
                case 1:
                    Tree t = new Tree(create_position, 1f, Constant.tree_spritesheet, false, "tree", editor_object_idx);
                    plants.Add(t);
                    entities_list.Add(t);
                    Console.WriteLine("tree," + create_position.X + "," + create_position.Y + ",1");
                    break;
                case 2:
                    Grass g = new Grass(create_position, 1f, editor_object_idx);
                    plants.Add(g);
                    entities_list.Add(g);
                    Console.WriteLine("grass," + create_position.X + "," + create_position.Y + ",1");
                    break;
                case 3:
                    Ghastly ghast = new Ghastly(Constant.ghastly_tex, create_position, 1f, Constant.hit_confirm_spritesheet, editor_object_idx);
                    entities_list.Add(ghast);
                    collision_entities.Add(ghast);
                    Console.WriteLine("ghastly," + create_position.X + "," + create_position.Y + ",1");
                    break;
                case 4:
                    StackedObject m = new StackedObject("marker", Constant.marker_spritesheet, create_position, 1f, 32, 32, 15, Constant.stack_distance, MathHelper.ToDegrees(editor_object_rotation), editor_object_idx);
                    entities_list.Add(m);
                    collision_geometry.Add(m);
                    collision_geometry_map[m] = false;
                    Console.WriteLine("marker," + create_position.X + "," + create_position.Y + ",1");
                    break;
                case 5:
                    Lamppost l = new Lamppost(create_position, 1f, editor_object_idx);
                    Light light = new Light(l.get_base_position(), 250f, 0.45f, Color.White, l);
                    //add light + light excluded
                    lights.Add(light);
                    light_excluded_entities.Add(l);
                    entities_list.Add(l);
                    Console.WriteLine($"added light");
                    Console.WriteLine("lamp," + create_position.X + "," + create_position.Y + ",1");
                    break;
                case 6:
                    Tile tile1 = new Tile(create_position, 3f, Constant.tile_tex, "big_tile", (int)DrawWeight.Light, editor_object_idx);
                    background_entities.Add(tile1);
                    Console.WriteLine("big_tile," + create_position.X + "," + create_position.Y + ",3");
                    break;
                case 7:
                    Tile tile2 = new Tile(create_position, 3f, Constant.tile_tex2, "cracked_tile", (int)DrawWeight.Light, editor_object_idx);
                    background_entities.Add(tile2);
                    Console.WriteLine("cracked_tile," + create_position.X + "," + create_position.Y + ",3");
                    break;
                case 8:
                    Tile tile3 = new Tile(create_position, 3f, Constant.tile_tex3, "reg_tile", (int)DrawWeight.Light, editor_object_idx);
                    background_entities.Add(tile3);
                    Console.WriteLine("reg_tile," + create_position.X + "," + create_position.Y + ",3");
                    break;
                case 9:
                    Tile tile4 = new Tile(create_position, 3f, Constant.tile_tex4, "round_tile", (int)DrawWeight.Light, editor_object_idx);
                    background_entities.Add(tile4);
                    Console.WriteLine("round_tile," + create_position.X + "," + create_position.Y + ",3");
                    break;
                case 10:
                    Tile tan_tile = new Tile(create_position, editor_object_scale, Constant.tan_tile_tex, "tan_tile", (int)DrawWeight.Medium, editor_object_idx);
                    add_floor_entity(tan_tile);
                    Console.WriteLine("tan_tile," + create_position.X + "," + create_position.Y + ",2");
                    break;
                case 11:
                    Tree c = new Tree(create_position, 4f, Constant.tree_spritesheet, true, "tree", editor_object_idx);
                    plants.Add(c);
                    entities_list.Add(c);
                    Console.WriteLine("tree," + create_position.X + "," + create_position.Y + ",4,true");
                    break;
                case 12:
                    StackedObject w = new StackedObject("wall", Constant.wall_tex, create_position, 1f, 32, 32, 8, Constant.stack_distance, MathHelper.ToDegrees(editor_object_rotation), editor_object_idx);
                    w.Update(gameTime, rotation);
                    entities_list.Add(w);
                    collision_geometry.Add(w);
                    collision_geometry_map[w] = false;
                    Console.WriteLine("wall," + create_position.X + "," + create_position.Y + ",1");
                    break;
                case 13:
                    StackedObject f = new StackedObject("fence", Constant.fence_spritesheet, create_position, 1f, 32, 32, 18, Constant.stack_distance1, MathHelper.ToDegrees(editor_object_rotation), editor_object_idx);
                    f.Update(gameTime, rotation);
                    entities_list.Add(f);
                    collision_geometry.Add(f);
                    collision_geometry_map[f] = false;
                    Console.WriteLine("fence," + create_position.X + "," + create_position.Y + ",1");
                    break;
                case 14:
                    InvisibleObject io = new InvisibleObject("deathbox", create_position, 1f, 48, 48, MathHelper.ToDegrees(editor_object_rotation), editor_object_idx);
                    io.set_debug(true);
                    io.Update(gameTime, rotation);
                    collision_geometry.Add(io);
                    collision_geometry_map[io] = false;
                    Console.WriteLine($"deathbox,{create_position.X},{create_position.Y},1,{MathHelper.ToDegrees(editor_object_rotation)}");
                    break;
                case 15:
                    Nightmare n = new Nightmare(Constant.nightmare_tex, Constant.nightmare_attack_tex, create_position, 1f, Constant.hit_confirm_spritesheet, player, editor_object_idx, "nightmare");
                    n.set_behavior_enabled(false);
                    n.set_placement_source(placement_source);
                    entities_list.Add(n);
                    collision_entities.Add(n);
                    enemies.Add(n);
                    //set ai entities for all entities
                    set_ai_entities_for_all_ais();
                    Console.WriteLine($"nightmare,{create_position.X},{create_position.Y},1,{MathHelper.ToDegrees(editor_object_rotation)}");
                    break;
                case 16:
                    ICondition cond = new EnemiesDeadRemoveObjCondition(editor_object_idx, this, enemies, new List<int>(), create_position, "");
                    condition_manager.add_condition(cond);
                    Console.WriteLine("edroc condition created!");
                    break;
                case 17:
                    Tile gt_tile = new Tile(create_position, editor_object_scale, Constant.grass_tile_tex, "grass_tile", (int)DrawWeight.Heavy, editor_object_idx);
                    add_floor_entity(gt_tile);
                    Console.WriteLine("grass_tile," + create_position.X + "," + create_position.Y + ",2");
                    break;
                case 18:
                    StackedObject otree = new StackedObject("orange_tree", Constant.orange_tree, create_position, 1f, 64, 64, 26, Constant.stack_distance, MathHelper.ToDegrees(editor_object_rotation), editor_object_idx);
                    plants.Add(otree);
                    entities_list.Add(otree);
                    Console.WriteLine("orange_tree," + create_position.X + "," + create_position.Y + ",1");
                    break;
                case 19:
                    StackedObject ytree = new StackedObject("yellow_tree", Constant.yellow_tree, create_position, 1f, 64, 64, 26, Constant.stack_distance, MathHelper.ToDegrees(editor_object_rotation), editor_object_idx);
                    plants.Add(ytree);
                    entities_list.Add(ytree);
                    Console.WriteLine("yellow_tree," + create_position.X + "," + create_position.Y + ",1");
                    break;
                case 20:
                    StackedObject gtree = new StackedObject("green_tree", Constant.green_tree, create_position, 1f, 64, 64, 26, Constant.stack_distance, MathHelper.ToDegrees(editor_object_rotation), editor_object_idx);
                    plants.Add(gtree);
                    entities_list.Add(gtree);
                    Console.WriteLine("green_tree," + create_position.X + "," + create_position.Y + ",1");
                    break;
                case 21:
                    StackedObject fl = new StackedObject("flower", Constant.flower_tex, create_position, 1f, 32, 32, 12, Constant.stack_distance1, MathHelper.ToDegrees(editor_object_rotation), editor_object_idx);
                    plants.Add(fl);
                    entities_list.Add(fl);
                    Console.WriteLine("flower," + create_position.X + "," + create_position.Y + ",1");
                    break;
                case 22:
                    StackedObject g2 = new StackedObject("grass2", Constant.tall_grass_tex, create_position, 1f, 32, 32, 9, Constant.stack_distance1, MathHelper.ToDegrees(editor_object_rotation), editor_object_idx);
                    plants.Add(g2);
                    entities_list.Add(g2);
                    Console.WriteLine("grass2," + create_position.X + "," + create_position.Y + ",1");
                    break;
                case 23:
                    Tile trail_tile = new Tile(create_position, editor_object_scale, Constant.trail_tex, "trail_tile", (int)DrawWeight.Medium, editor_object_idx);
                    add_floor_entity(trail_tile);
                    Console.WriteLine("trail_tile," + create_position.X + "," + create_position.Y + ",2");
                    break;
                case 24:
                    Tile sand_tile = new Tile(create_position, editor_object_scale, Constant.sand_tex, "sand_tile", (int)DrawWeight.Medium, editor_object_idx);
                    add_floor_entity(sand_tile);
                    Console.WriteLine("sand_tile," + create_position.X + "," + create_position.Y + ",2");
                    break;
                case 25:
                    StackedObject sword = new StackedObject("sword", Constant.sword_tex, create_position, 1f, 16, 16, 22, Constant.stack_distance1, MathHelper.ToDegrees(editor_object_rotation), editor_object_idx);
                    entities_list.Add(sword);
                    collision_entities.Add(sword);
                    Console.WriteLine("sword," + create_position.X + "," + create_position.Y + ",1");
                    break;
                case 26:
                    StackedObject box = new StackedObject("box", Constant.box_spritesheet, create_position, 1f, 32, 32, 18, Constant.stack_distance1, MathHelper.ToDegrees(editor_object_rotation), editor_object_idx);
                    box.Update(gameTime, rotation);
                    entities_list.Add(box);
                    collision_geometry.Add(box);
                    collision_geometry_map[box] = false;
                    Console.WriteLine("box," + create_position.X + "," + create_position.Y + ",1");
                    break;
                case 27:
                    StackedObject house = new StackedObject("house", Constant.house_spritesheet, create_position, 1f, 128, 128, 54, Constant.stack_distance1, MathHelper.ToDegrees(editor_object_rotation), editor_object_idx);
                    house.Update(gameTime, rotation);
                    entities_list.Add(house);
                    collision_geometry.Add(house);
                    collision_geometry_map[house] = false;
                    Console.WriteLine("house," + create_position.X + "," + create_position.Y + ",1");
                    break;
                case 28:
                    ParticleSystem ps = new ParticleSystem(true, create_position, 1, 800, 5, 2, 4, Constant.white_particles, new List<Texture2D>() { Constant.footprint_tex });
                    particle_systems.Add(ps);
                    Console.WriteLine($"particle_system,true,{create_position.X},{create_position.Y},1,800,5,2,4,white,footprint");
                    break;
                case 29:
                    NPC scrow = new NPC(Constant.scarecrow_tex, create_position, 1f, 32, (int)AIBehavior.Stationary, null, "", Constant.hit_confirm_spritesheet, player, editor_object_idx, "scarecrow", this, true);
                    scrow.set_health(10000);
                    entities_list.Add(scrow);
                    collision_entities.Add(scrow);
                    npcs.Add(scrow);
                    Console.WriteLine($"scarecrow,{create_position.X},{create_position.Y}");
                    break;
                case 30:
                    Ghost ghost = new Ghost(Constant.ghastly_tex, create_position, 1f, Constant.hit_confirm_spritesheet, player, editor_object_idx, "ghost", this);
                    ghost.set_behavior_enabled(false);
                    ghost.set_placement_source(placement_source);
                    entities_list.Add(ghost);
                    collision_entities.Add(ghost);
                    enemies.Add(ghost);
                    set_ai_entities_for_all_ais();
                    Console.WriteLine($"ghost,{create_position.X},{create_position.Y},1,{MathHelper.ToDegrees(editor_object_rotation)}");
                    break;
                case 31:
                    HitSwitch hs = new HitSwitch("hitswitch", Constant.switch_active, Constant.switch_inactive, create_position, 1f, 16, 16, 8, Constant.stack_distance1, MathHelper.ToDegrees(editor_object_rotation), editor_object_idx);
                    hs.Update(gameTime, rotation);
                    entities_list.Add(hs);
                    collision_entities.Add(hs);
                    collision_geometry.Add(hs);
                    collision_geometry_map[hs] = false;
                    switches.Add(hs);
                    break;
                case 32:
                    ICondition s_cond = new SwitchCondition(editor_object_idx, this, switches, new List<int>(), new List<int>(), create_position, "");
                    condition_manager.add_condition(s_cond);
                    Console.WriteLine("switch condition created!");
                    break;
                case 33:
                    BillboardSprite dash_cloak = new BillboardSprite(Constant.dash_cloak_pickup_tex, create_position, 1f, "dash_cloak", editor_object_idx, true);
                    entities_list.Add(dash_cloak);
                    collision_entities.Add(dash_cloak);
                    break;
                case 34:
                    StackedObject cracked_rocks = new StackedObject("cracked_rocks", Constant.cracked_rocks_spritesheet, create_position, 1f, 32, 32, 4, Constant.stack_distance1, MathHelper.ToDegrees(editor_object_rotation), editor_object_idx);
                    entities_list.Add(cracked_rocks);
                    collision_geometry.Add(cracked_rocks);
                    collision_geometry_map[cracked_rocks] = false;
                    break;
                case 35:
                    BillboardSprite player_chip = new BillboardSprite(Constant.player_chip_tex, create_position, 1f, "player_chip", editor_object_idx);
                    editor_only_objects.Add(player_chip);
                    break;
                case 36:
                    BillboardSprite bow = new BillboardSprite(Constant.bow_pickup_tex, create_position, 1f, "bow", editor_object_idx, false);
                    entities_list.Add(bow);
                    collision_entities.Add(bow);
                    break;
                case 37:
                    Haunter haunter = new Haunter(Constant.haunter_tex, create_position, 1f, Constant.hit_confirm_spritesheet, player, editor_object_idx, "haunter", this);
                    haunter.set_behavior_enabled(false);
                    haunter.set_placement_source(placement_source);
                    entities_list.Add(haunter);
                    collision_entities.Add(haunter);
                    enemies.Add(haunter);
                    set_ai_entities_for_all_ais();
                    break;
                case 38:
                    StackedObject cpm = new StackedObject("checkpoint", Constant.checkpoint_marker_spritesheet, create_position, 1f, 32, 32, 15, Constant.stack_distance, MathHelper.ToDegrees(editor_object_rotation), editor_object_idx, true);
                    entities_list.Add(cpm);
                    collision_geometry.Add(cpm);
                    collision_geometry_map[cpm] = false;
                    Console.WriteLine("checkpoint," + create_position.X + "," + create_position.Y + ",1");
                    break;
                case 39:
                    Skeleton skelly = new Skeleton(Constant.skelly_tex, Constant.skelly_attack_tex, Constant.skelly_attack_charge_tex, create_position, 1f, Constant.hit_confirm_spritesheet, player, editor_object_idx, "skeleton");
                    skelly.set_behavior_enabled(false);
                    skelly.set_placement_source(placement_source);
                    entities_list.Add(skelly);
                    collision_entities.Add(skelly);
                    enemies.Add(skelly);
                    set_ai_entities_for_all_ais();
                    break;
                case 40:
                    ShadowKnight sk = new ShadowKnight(Constant.shadow_knight_tex, Constant.shadow_knight_attack_tex, Constant.shadow_knight_charge_attack_tex, create_position, 1f, Constant.hit_confirm_spritesheet, player, editor_object_idx, "shadowknight");
                    sk.set_behavior_enabled(false);
                    sk.set_placement_source(placement_source);
                    entities_list.Add(sk);
                    collision_entities.Add(sk);
                    enemies.Add(sk);
                    set_ai_entities_for_all_ais();
                    break;
                case 41:
                    StackedObject closedgate = new StackedObject("gate", Constant.gate_spritesheet, create_position, 1f, 32, 32, 28, Constant.stack_distance1, MathHelper.ToDegrees(editor_object_rotation), editor_object_idx);
                    closedgate.Update(gameTime, rotation);
                    entities_list.Add(closedgate);
                    collision_geometry.Add(closedgate);
                    collision_geometry_map[closedgate] = false;
                    Console.WriteLine("gate," + create_position.X + "," + create_position.Y + ",1");
                    break;
                case 42:
                    StackedObject gw = new StackedObject("green_wall", Constant.green_wall_tex, create_position, 1f, 32, 32, 8, Constant.stack_distance, MathHelper.ToDegrees(editor_object_rotation), editor_object_idx);
                    gw.Update(gameTime, rotation);
                    entities_list.Add(gw);
                    collision_geometry.Add(gw);
                    collision_geometry_map[gw] = false;
                    Console.WriteLine("green_wall," + create_position.X + "," + create_position.Y + ",1");
                    break;
                case 43:
                    StackedObject torii = new StackedObject("torii", Constant.torii_spritesheet, create_position, 1f, 64, 64, 43, Constant.stack_distance, MathHelper.ToDegrees(editor_object_rotation), editor_object_idx);
                    torii.Update(gameTime, rotation);
                    entities_list.Add(torii);
                    Console.WriteLine("torii," + create_position.X + "," + create_position.Y + ",1");
                    break;
                default:
                    break;
            }
            //increment_editor_idx();
        }

        public int get_editor_object_idx() {
            return editor_object_idx;
        }

        public void increment_editor_idx() {
            editor_object_idx++;
        }
        
        /*PARTICLE SYSTEMS CODE*/
        public void update_world_particle_systems(GameTime gameTime, float rotation) {
            //update and manage world particle systems
            foreach (ParticleSystem ps in particle_systems) {
                if (Vector2.Distance(ps.get_base_position(), player.get_base_position()) <= render_distance) {
                    ps.Update(gameTime, rotation);
                }
                //add dead systems to dead list
                if (ps.is_finished()) {
                    dead_particle_systems.Add(ps);
                }
            }
            //clear dead particle systems
            foreach (ParticleSystem ps in dead_particle_systems) {
                //remove from original list
                particle_systems.Remove(ps);
            }
            //clear
            dead_particle_systems.Clear();
        }

        public bool get_object_persistence() {
            return object_persistence;
        }
        
        #region save_world_file
        public List<GameWorldObject> build_world_objs(List<IEntity> entities, List<ForegroundEntity> foreground_entities, List<BackgroundEntity> background_entities, out IEntity shade) {
            //track player chip count
            int player_chip_count = 0;
            //set up object lists to return
            List<GameWorldObject> world_objs = new List<GameWorldObject>();

            //iterate over editor only objects (ex. player_chip)
            foreach (IEntity e in editor_only_objects) {
                world_objs.Add(e.to_world_level_object());
                if (e.get_id().Equals("player_chip")) {
                    player_chip_count++;
                }
            }
            //check for player chip to save level (should only be one within the level)
            if (player_chip_count != 1) {
                throw new Exception($"File Save Error. There should be exactly 1 player chip (player spawn point) in the level, but {player_chip_count} were found.");
            }
            //iterate over all entities
            shade = null;
            foreach (IEntity e in entities) {
                //set out param to shade
                if (e.get_id().Equals("shade")) {
                    shade = e;
                }
                //we need to exclude deleted objects from being saved to the world level file
                //we also need to exclude the player form being saved as we are using player chips to denote spawn points
                if (!entities_to_clear.Contains(e) && 
                    !(e is Player) && 
                    !(e is Arrow) && 
                    !(e is Grenade) && 
                    !(e is Footprints) && 
                    !e.get_id().Equals("money") && 
                    !e.get_id().Equals("shade")) {
                    //also need to check further for AI and exclude those added via script
                    if (((e is Nightmare) || (e is Ghost)) && !(e is NPC)) {
                        //check for object placement source
                        //can just cast to nightmare since ghost inherits from nightmare
                        Nightmare n = (Nightmare)e;
                        int placement_src = n.get_placement_source();
                        //skip entity if the placement source is via script
                        if (placement_src == (int)ObjectPlacement.Script) {
                            continue;
                        } else {
                            //add if not placed by script
                            world_objs.Add(e.to_world_level_object());    
                        }
                    } else {
                        //add if not nightmare or ghost
                        world_objs.Add(e.to_world_level_object());
                    }
                }
            }
            //iterate over foreground entities
            foreach (ForegroundEntity fe in foreground_entities) {
                if (fe is Tree) {
                    Tree tree = (Tree)fe;
                    world_objs.Add(tree.to_world_level_object());
                }
            }
            //iterate over background entities
            foreach (BackgroundEntity be in background_entities) {
                if (be is Tile) {
                    Tile tile = (Tile)be;
                    if (!entities_to_clear.Contains(tile)) {
                        world_objs.Add(tile.to_world_level_object());
                    }
                }
            }
            //iterate over floor entities
            foreach (FloorEntity fe in floor_entities) {
                if (fe is Tile) {
                    Tile tile = (Tile)fe;
                    if (!entities_to_clear.Contains(tile)) {
                        world_objs.Add(tile.to_world_level_object());
                    }
                }
            }

            //handle saving collision geometry that is not drawn
            //(invisible objects)
            foreach (IEntity g in collision_geometry) {
                if (g is InvisibleObject) {
                    InvisibleObject io = (InvisibleObject)g;
                    if (!entities_to_clear.Contains(io)) {
                        world_objs.Add(io.to_world_level_object());
                    }
                }
            }

            return world_objs;
        }

        public void save_world_level_to_file(List<IEntity> entities,  List<ForegroundEntity> foreground_entities,  List<BackgroundEntity> background_entities, int level_load_count, string file_save_name) {
            //set up object lists to save to file
            List<GameWorldTrigger> world_triggers = new List<GameWorldTrigger>();
            List<GameWorldCondition> conditions = condition_manager.get_world_level_list();
            List<GameWorldParticleSystem> world_particle_systems = new List<GameWorldParticleSystem>();
            
            //iterate over level triggers
            foreach (ITrigger t in triggers) {
                world_triggers.Add(t.to_world_level_trigger());
            }

            //handle saving particle systems
            foreach(ParticleSystem ps in particle_systems) {
                world_particle_systems.Add(ps.to_world_level_particle_system());
            }

            //sort world objects by numerical id
            IEntity shade = null;
            List<GameWorldObject> sorted_world_objs = build_world_objs(entities, foreground_entities, background_entities, out shade).OrderBy (o => o.object_id_num).ToList();
            List<GameWorldTrigger> sorted_world_triggers = world_triggers.OrderBy (t => t.object_id_num).ToList();

            //pull and store all of the conditions into a separate data structure in this function
            //we can use it to reconstruct the correct connections after the re-issuing of ids has occurred
            //this is done to prevent objects that have been deleted from causing conditions to lose the original entities they were assigned to listen and remove
            //causing this hole in the ordered ints that make up the id system can cause the window of objects that conditions listen and delete to shift
            //this dictionary holds the new ids and reassigns the conditions to their new ids to fix this bug
            Dictionary<ICondition, (List<int>, List<int>)> condition_obj_id_data_map = new Dictionary<ICondition, (List<int>, List<int>)>();
            
            //sanity check and re-issue ids to objects to prevent any gaps in object ids that could result in an issue on level loading
            //trying to collapse gaps in ids mostly because it throws off the level loading process
            //this is mostly because we introduced the concept of object deletion which then leads to gaps in ids
            //in addition to this through gameplay, entities may be created or destroyed which can throw off the running editor id count
            //this re-issuing of the ids fixes this issue and allows us to save partial states or changes to the world due to gameplay
            int obj_reissue_id = 0;
            for (int i = 0; i < sorted_world_objs.Count; i++) {
                //keep conditions updated as we assign new ids
                //as we re-assign ids, construct data structure of conditions and entity ids through previous entity id and then build a list to hot swap with the new id
                //also update entity in world with new id
                int previous_id = sorted_world_objs[i].object_id_num;
                //pull condition to get data to make update for hotswap
                (ICondition, int) c = condition_manager.get_condition_by_entity_id(previous_id);
                //valid condition check
                if (c.Item2 != -1) {
                    //pull condition and list type
                    ICondition cond = c.Item1;
                    int list_type = c.Item2;
                    //update dictionary appropriately
                    if (condition_obj_id_data_map.ContainsKey(cond)) {
                        //pull and update list
                        if (list_type == 0) {
                            //listen
                            condition_obj_id_data_map[cond].Item1.Add(obj_reissue_id);
                        } else {
                            //remove
                            condition_obj_id_data_map[cond].Item2.Add(obj_reissue_id);
                        }
                    } else {
                        //add
                        if (list_type == 0) {
                            //listen
                            condition_obj_id_data_map.Add(cond, (new List<int> { obj_reissue_id }, new List<int>()));
                        } else {
                            //remove
                            condition_obj_id_data_map.Add(cond, (new List<int>(), new List<int> { obj_reissue_id }));
                        }
                    }
                }

                if (previous_id == 3266) {
                    Console.WriteLine($"found it, obj_id_to_assign: {obj_reissue_id}");
                }
                
                //find and assign new object id to entity
                IEntity render_list_entity = entities_list.get_entity_by_id(previous_id);
                if (render_list_entity != null) {
                    //found the entity in the render list and can set the id accordingly
                    render_list_entity.set_obj_ID_num(obj_reissue_id);
                } else {
                    //object was not found, however it might exist in a different list not included in the render list (floor entities)
                    //check floor entities
                    IEntity non_render_list_entity = search_non_render_list_entity(previous_id);
                    if (non_render_list_entity != null) {
                        non_render_list_entity.set_obj_ID_num(obj_reissue_id);
                    } else {
                        throw new Exception("Missing Entity Error: We fucked up. Cannot find the entity to save via id in any data structure or list. Check more lists???");
                    }
                }
                //increase reissue id
                obj_reissue_id++;
            }
            //rebuild sorted world objs with new ids that have been assigned
            sorted_world_objs = build_world_objs(entities, foreground_entities, background_entities, out shade).OrderBy (o => o.object_id_num).ToList();

            //triggers
            for (int i = 0; i < sorted_world_triggers.Count; i++) {
                sorted_world_triggers[i].object_id_num = obj_reissue_id;
                obj_reissue_id++;
            }
            
            //hot swap condition lists being listened to and removed based on new ids assigned from entity id re-assignment
            foreach (KeyValuePair<ICondition, (List<int>, List<int>)> kv in condition_obj_id_data_map) {
                //pull data from key-value pair
                ICondition c = kv.Key;
                List<int> listen_ids = kv.Value.Item1;
                List<int> remove_ids = kv.Value.Item2;
                //hot swap each condition with their updated ids
                condition_manager.update_condition_listen_ids(c.condition_id(), listen_ids);
                condition_manager.update_condition_remove_ids(c.condition_id(), remove_ids);
            }
            //now we can set up sorted_world_conditions
            List<GameWorldCondition> sorted_world_conditions = condition_manager.get_world_level_list().OrderBy(c => c.object_id_num).ToList();
            //re-issue condition ids (thankfully this does not have repercussions for the actual ids inside the conditions)
            for (int i = 0; i < sorted_world_conditions.Count; i++) {
                sorted_world_conditions[i].object_id_num = obj_reissue_id;
                obj_reissue_id++;
            }

            //set world particle systems object id (this number is irrelevant to particle systems, but it should still be in order as good practice and for consistency)
            for (int i = 0; i < world_particle_systems.Count; i++) {
                world_particle_systems[i].object_id_num = obj_reissue_id;
                obj_reissue_id++;
            }
            
            GameWorldObject saved_shade = null;
            if (shade != null) {
                saved_shade = shade.to_world_level_object();
                saved_shade.object_id_num = obj_reissue_id;
                obj_reissue_id++;
                Console.WriteLine("SHADE SAVED");
            } else {
                Console.WriteLine("SHADE IS NULL");
            }

            //once object ids have been re-issued, modify the lists to add the light data in
            //build dictionary with entity ids
            //see comments below about performance and runtime
            Dictionary<Light, int> light_entity_ids = new Dictionary<Light, int>();
            foreach (Light light in lights) {
                light_entity_ids.Add(light, light.get_parent_entity().get_obj_ID_num());
            }
            //iterate over dictionary and set lights for game world objs
            foreach (GameWorldObject gwo in sorted_world_objs) {
                foreach (KeyValuePair<Light, int> kv in light_entity_ids) {
                    int light_entity_id = kv.Value;
                    Light light = kv.Key;
                    if (gwo.object_id_num == light_entity_id) {
                        //set light in sorted world objects
                        gwo.light = new GameWorldLight {
                            light_center_x = light.get_center_position().X,
                            light_center_y = light.get_center_position().Y,
                            radius = light.get_radius()
                        };
                    }
                }
            }
            //modify object list to add light exclusion data
            //yes this is n^2, but it only happens on saves which doesn't affect the player, only us lowly devs
            //plus there won't ever be that many light excluded entities
            foreach (GameWorldObject gwo in sorted_world_objs) {
                foreach (IEntity e in light_excluded_entities) {
                    if (e.get_obj_ID_num() == gwo.object_id_num) {
                        //set exclusion
                        gwo.light_excluded = true;
                    }
                }
            }

            //clear color saving
            string clear_color_hex = null;
            if (!game.clear_color.Equals(Color.CornflowerBlue)) {
                //convert clear color to hex
                clear_color_hex = $"#{game.clear_color.R:X2}{game.clear_color.G:X2}{game.clear_color.B:X2}";
            }
            
            //generate GameWorldFile object with all saved objects
            GameWorldFile world_file = new GameWorldFile {
                level_loaded_count = level_load_count,
                checkpoint_level_id = this.checkpoint_level_id,
                clear_color = clear_color_hex,
                world_objects = sorted_world_objs,
                world_triggers = sorted_world_triggers,
                conditions = sorted_world_conditions,
                particle_systems = world_particle_systems,
                shade = saved_shade,
                world_script = world_script_parser.get_game_world_script()
            };
            //take world file and serialize to json then write to file
            string file_contents = JsonSerializer.Serialize(world_file);
            //write
            write_to_file("levels/", file_save_name, file_contents);
        }

        public void write_to_file(string file_path, string file_name, string file_contents) {
            var path = Path.Combine(content_root_directory, file_path + file_name);
            Console.WriteLine($"saving file:{file_path+file_name}");
            //write file
            File.WriteAllText(path, file_contents);
        }
        #endregion

        //function to update forest / plant / foreground geometry (basically to populate camera rotation to all these objects in order to get the rotations correct)
        public void update_forest_geometry(GameTime gameTime, float rotation) {
            for (int i = 0; i < plants.Count; i++) {
                plants[i].Update(gameTime, rotation);
            }
            foreach (ForegroundEntity f in foreground_entities) {
                f.Update(gameTime, rotation);
            }
        }

        //function to update invisible objects based on editor
        public void update_invisible_objects(bool visible) {
            foreach (IEntity e in collision_geometry) {
                if (e is InvisibleObject) {
                    InvisibleObject io = (InvisibleObject)e;
                    io.set_debug(visible);
                }
            }
        }

        //update temp tiles
        public void update_temp_tiles(GameTime gameTime, float rotation) {
            foreach (TempTile t in temp_tiles) {
                t.Update(gameTime, rotation);
                //set up removal
                if (t.is_finished()) {
                    clear_entity(t);
                }
            }
        }

        public List<TextBox> get_all_textboxes() {
            List<TextBox> textboxes = new List<TextBox>();
            foreach (NPC npc in npcs) {
                TextBox t = npc.get_textbox();
                if (t != null) {
                    textboxes.Add(t);
                }
            }
            Dictionary<IEntity, int> all_entities = entities_list.get_all_entities();
            foreach (KeyValuePair<IEntity, int> kv in all_entities) {
                IEntity e = kv.Key;
                if (e is Sign) {
                    Sign s = (Sign)e;
                    //these textboxes will not ever be null because what is the point of a sign without text?
                    textboxes.Add(s.get_textbox());
                }
            }
            return textboxes;
        }

        public void process_explosions(GameTime gameTime) {
            //only process explosions if we have explosions to process
            if (explosion_list.Count > 0) {
                foreach ((IEntity, Vector2, float) explosion in explosion_list) {
                    IEntity grenade = explosion.Item1;
                    Vector2 explosion_origin = explosion.Item2;
                    float explosion_radius = explosion.Item3;
                    //calculate in range entities using radius
                    List<IAiEntity> in_range_enemies = new List<IAiEntity>();
                    foreach (IAiEntity enemy in enemies) {
                        if (enemy is IEntity) {
                            IEntity enemy_entity = (IEntity)enemy;
                            if (Vector2.Distance(enemy_entity.get_base_position(), explosion_origin) <= explosion_radius) {
                                in_range_enemies.Add(enemy);
                            }
                        }
                    }
                    foreach (IAiEntity enemy in in_range_enemies) {
                        //damage
                        //we have already validated they are all of class IEntity
                        if (enemy is ICollisionEntity) {
                            ICollisionEntity ce = (ICollisionEntity)enemy;
                            ce.take_hit(grenade, Constant.grenade_damage);
                        }
                    }
                    List<IEntity> in_range_geometry = new List<IEntity>();
                    foreach (IEntity e in collision_geometry) {
                        if (e.get_id().Equals("hitswitch") || e.get_id().Equals("box")) {
                            if (Vector2.Distance(e.get_base_position(), explosion_origin) <= explosion_radius) {
                                in_range_geometry.Add(e);
                            }
                        }
                    }
                    foreach (IEntity e in in_range_geometry) {
                        ICollisionEntity ce = (ICollisionEntity)e;
                        if (e.get_id().Equals("hitswitch")) {
                            ce.take_hit(grenade, 0);
                        } else if (e.get_id().Equals("box")) {
                            clear_entity(e);
                            particle_systems.Add(new ParticleSystem(true, Constant.rotate_point(e.get_base_position(), camera.Rotation, 1f, Constant.direction_up), 2, 500f, 1, 5, 1, 3, Constant.white_particles, new List<Texture2D>() { Constant.footprint_tex }));
                        }
                    }
                }
                explosion_list.Clear();
            }
        }

        //function to check player dash and shake camera
        private void check_player_dash_camera_shake() {
            if (player.is_dashing()) {
                set_camera_shake(10f, 1f, 1f);
            }
        }
        
        #region collision
        //function to check collisions with player hitbox/hurtbox
        public void check_entity_collision(GameTime gameTime) {
            //check collisions against player hitbox
            if (player.hitbox_active()) {
                Constant.profiler.start("world_check_entity_collisions_player_hitbox_check");
                foreach (IEntity e in collision_entities) {
                    //distance check
                    if (Vector2.Distance(e.get_base_position(), player.get_base_position()) < (render_distance/2)) {
                        //check player hitboxes collision with all entity hurtboxes
                        if ((e is Ghastly || e is Nightmare) && (e.get_id().Equals("scarecrow") || !(e is NPC))) {
                        //if ((e is Ghastly || e is Nightmare) && !(e is NPC)) {
                            ICollisionEntity ic = (ICollisionEntity)e;
                            if (ic.is_hurtbox_active()) {
                                bool collision = player.check_hitbox_collisions(ic.get_hurtbox());
                                if (collision) {
                                    //collision entity takes hit
                                    ic.take_hit(player, 1);
                                    //particle effect
                                    ParticleSystem p = new ParticleSystem(true, Constant.rotate_point(e.get_base_position(), camera.Rotation, 1f, Constant.direction_up), 2, 500f, 1, 5, 1, 3, Constant.black_particles, new List<Texture2D>() { Constant.footprint_tex });
                                    p.set_world(this);
                                    particle_systems.Add(p);
                                    //add to arrows charges for player
                                    player.add_arrow_charge(1);
                                    //freeze frames to add weight to collision
                                    freeze_frames = 2;
                                    //shake the camera
                                    set_camera_shake(Constant.camera_shake_milliseconds, Constant.camera_shake_angle, Constant.camera_shake_hit_radius);
                                    play_spatial_sfx(Constant.hit1_sfx, e.get_base_position(), 0f, get_render_distance());
                                    //drop item
                                    //every enemy extends nightmare so we can just cast it to that to get the health value regardless of the real class value
                                    Nightmare n = (Nightmare)e;
                                    if (n.get_health() <= 0) {
                                        if (e.get_id().Equals("shade")) {
                                            //we probably don't have to check null here since it is basically a given that the shade will be in this level if we get here (current level)
                                            if (shade_level_id != null) {
                                                remove_shade_from_level_file(shade_level_id);
                                            }
                                            //drop player cash from last time
                                        }
                                        //drop item on death
                                        add_dropped_item(
                                            new AnimatedSprite(Constant.crystal_tex, e.get_base_position(), 0.8f, 
                                                150f, 9, 0, 0, 16, 16, //animation variables
                                                "crystal_money", get_editor_object_idx()
                                            ),
                                            0f
                                        );
                                    }
                                }
                            }
                        }
                    }
                }
                Constant.profiler.end("world_check_entity_collisions_player_hitbox_check");
            }

            //check enemy collisions against player
            Constant.profiler.start("world_check_entity_collisions_enemy hitboxes");
            foreach (IEntity e in collision_entities) {
                //distance check to save cycles
                if (Vector2.Distance(e.get_base_position(), player.get_base_position()) < render_distance) {
                    //check enemy hitbox collision with player hurtboxes
                    if (e is Nightmare) {
                        if (e.get_id().Equals("nightmare")) {
                            Nightmare n = (Nightmare)e;
                            if (n.hitbox_active()) {
                                bool collision = player.check_hurtbox_collisions(n.get_hitbox());
                                if (collision && player.is_hurtbox_active()) {
                                    player.take_hit(n, n.get_damage());
                                    //freeze frames to add weight
                                    //freeze_frames = 2;
                                    //shake the camera
                                    set_camera_shake(Constant.camera_shake_milliseconds, Constant.camera_shake_angle, Constant.camera_shake_hit_radius);
                                }
                            }
                        } else if (e.get_id().Equals("ghost")) {
                            Ghost g = (Ghost)e;
                            if (player.is_hurtbox_active()) {
                                bool collision = player.check_hurtbox_collisions(g.get_hurtbox());
                                if (collision) {
                                    player.take_hit(g, g.get_damage());
                                    set_camera_shake(Constant.camera_shake_milliseconds, Constant.camera_shake_angle, Constant.camera_shake_hit_radius);
                                }
                            }
                        } else if (e.get_id().Equals("skeleton")) {
                            Skeleton skelly = (Skeleton)e;
                            bool collision = player.check_hurtbox_collisions(skelly.get_hitbox());
                            if (collision && player.is_hurtbox_active() && skelly.hitbox_active()) {
                                player.take_hit(skelly, skelly.get_damage());
                                set_camera_shake(Constant.camera_shake_milliseconds, Constant.camera_shake_angle, Constant.camera_shake_hit_radius);
                            }
                        } else if (e.get_id().Equals("shadowknight")) {
                            ShadowKnight sk = (ShadowKnight)e;
                            bool collision = player.check_hurtbox_collisions(sk.get_hitbox());
                            if (collision && player.is_hurtbox_active() && sk.hitbox_active()) {
                                player.take_hit(sk, sk.get_damage());
                                set_camera_shake(Constant.camera_shake_milliseconds, Constant.camera_shake_angle, Constant.camera_shake_hit_radius);
                            }
                        }
                    } else if (e is TempTile) {
                        TempTile tt = (TempTile)e;
                        if (e.get_id().Equals("sludge_tile")) {
                            if (player.is_hurtbox_active()) {
                                bool collision = player.check_hurtbox_collisions(tt.get_hurtbox());
                                //collision only results with true if the opacity of the temp tile is greater than a threshold
                                collision_tile_map[tt] = collision && (tt.get_opacity() >= 0.15f);
                            }
                        }
                    }

                    //handle projectile collisions
                    foreach (IEntity proj in projectiles) {
                        if (proj is Grenade) {
                            //skip grenades because they do damage on impact, not the projectile itself
                            continue;
                        }
                        if (!e.Equals(proj)) {
                            if (e is Nightmare) {
                                Nightmare nm = (Nightmare)e;
                                Arrow a = (Arrow)proj;
                                bool collision = nm.get_hurtbox().collision(a.get_hitbox());
                                if (collision) {
                                    nm.take_hit(a, 1);
                                    //particle effect for hit
                                    ParticleSystem p = new ParticleSystem(true, Constant.rotate_point(e.get_base_position(), camera.Rotation, 1f, Constant.direction_up), 2, 500f, 1, 5, 1, 3, Constant.black_particles, new List<Texture2D>() { Constant.footprint_tex });
                                    p.set_world(this);
                                    particle_systems.Add(p);
                                    //if the shot is not a power shot then clear it on impact immediately
                                    //it will be cleared when the speed runs out (dead)
                                    if (!a.is_power_shot()) { a.set_dead(true); entities_to_clear.Add(a); }
                                    play_spatial_sfx(Constant.hit1_sfx, e.get_base_position(), 0f, get_render_distance());
                                    //handle dropped items on enemy death from projectiles
                                    if (nm.get_health() <= 0) {
                                        if (e.get_id().Equals("shade")) {
                                            //we probably don't have to check null here since it is basically a given that the shade will be in this level if we get here (current level)
                                            if (shade_level_id != null) {
                                                remove_shade_from_level_file(shade_level_id);
                                            }
                                            //drop player cash from last time
                                        }
                                        //drop item on death
                                        add_dropped_item(
                                            new AnimatedSprite(Constant.crystal_tex, e.get_base_position(), 0.8f, 
                                                150f, 9, 0, 0, 16, 16, //animation variables
                                                "crystal_money", get_editor_object_idx()
                                            ),
                                            0f
                                        );
                                    }
                                }
                            }
                        }
                    }
                }
            }
            Constant.profiler.end("world_check_entity_collisions_enemy hitboxes");

            //check player interactions with collision_entities (dialogue specific - signs, npc dialogue, etc)
            Constant.profiler.start("world_check_entity_collisions_player_interaction");
            foreach (IEntity e in collision_entities) {
                if (Vector2.Distance(e.get_base_position(), player.get_base_position()) < (render_distance/3)) {
                    if (player.interacting()) {
                        if (e is Sign) {
                            Sign s = (Sign)e;
                            bool collision = player.check_hurtbox_collisions(s.get_interaction_box());
                            if (collision) {
                                //calculate screen textbox position
                                Vector2 textbox_screen_position = Constant.world_position_to_screen_position(s.get_overhead_position(), camera);
                                textbox_screen_position *= game.get_game_canvas().get_current_scale();
                                textbox_screen_position += new Vector2(game.get_game_canvas().get_destination_rectangle().X, game.get_game_canvas().get_destination_rectangle().Y);
                                //set textbox screen position
                                s.get_textbox().set_position(textbox_screen_position);
                                //set sign to display
                                s.display_textbox();
                                //set current textbox to correct instance
                                current_textbox = s.get_textbox();
                                current_sign = s;
                            }
                        } else if (e is NPC) {
                            if (!e.get_id().Equals("scarecrow")) {
                                NPC npc = (NPC)e;
                                bool collision = player.check_hurtbox_collisions(npc.get_interaction_box());
                                if (collision) {
                                    //orient npc to target for speaking
                                    npc.orient_to_target(player.get_base_position(), camera.Rotation);
                                    //calculate screen textbox position from overhead position of npc
                                    Vector2 screen_position = Vector2.Transform(npc.get_overhead_position(), camera.Transform);
                                    //scale position by current canvas scale
                                    screen_position *= game.get_game_canvas().get_current_scale();
                                    //offset position based on desination rectangle
                                    screen_position += new Vector2(game.get_game_canvas().get_destination_rectangle().X, game.get_game_canvas().get_destination_rectangle().Y);
                                    //find first matching tag
                                    string tag = npc.find_first_matching_tag(condition_tags);
                                    //filter npc textbox based on tag
                                    npc.get_textbox().filter_messages_on_tag(tag);
                                    //set textbox position
                                    npc.get_textbox().set_position(screen_position);
                                    //set npc to display
                                    npc.display_textbox();
                                    //set current textbox to correct instance
                                    current_textbox = npc.get_textbox();
                                    current_npc = npc;
                                }
                            }
                        }
                    }

                    if (e is Sign) { //check if interaction is available and display prompt for signs
                        Sign s = (Sign)e;
                        bool collision = player.check_hurtbox_collisions(s.get_interaction_box());
                        //set available interaction display
                        s.set_display_interaction(collision);
                    } else if (e is NPC) {
                        if (!e.get_id().Equals("scarecrow")) {
                            NPC npc = (NPC)e;
                            bool collision = player.check_hurtbox_collisions(npc.get_interaction_box());
                            //set available interaction display
                            npc.set_display_interaction(collision);
                        }
                    } else if (e is StackedObject) {
                        //interactions with picking up weapons
                        StackedObject stacked_object = (StackedObject)e;
                        bool collision = player.check_hurtbox_collisions(stacked_object.get_hurtbox());
                        if (collision && player.interacting()) {
                            if ((e.get_id().Equals("sword") || e.get_id().Equals("bow") || e.get_id().Equals("dash")) && player.get_attribute_active(e.get_id()) == false) {
                                player_item_pickup(player, e);
                                clear_entity(e);
                                save_world_level_to_file(
                                    entities_list.get_all_entities().Keys.ToList(),
                                    foreground_entities,
                                    background_entities,
                                    level_loaded_map[current_level_id],
                                    Constant.level_mod_prefix + current_level_id
                                );
                            }
                        }
                    } else if (e is BillboardSprite) {
                        //interactions with billboard (items in the world)
                        BillboardSprite sprite = (BillboardSprite)e;
                        bool collision = player.check_hurtbox_collisions(sprite.get_hurtbox());
                        sprite.set_display_interaction(collision);
                        if (collision && player.interacting()) {
                            //check for item specifics
                            if (e.get_id().Equals("dash_cloak")) {
                                //pickup item
                                player_item_pickup(player, sprite);
                                clear_entity(sprite);
                                save_world_level_to_file(
                                    entities_list.get_all_entities().Keys.ToList(),
                                    foreground_entities,
                                    background_entities,
                                    level_loaded_map[current_level_id],
                                    Constant.level_mod_prefix + current_level_id
                                );
                            } else if (e.get_id().Equals("bow")) {
                                //pickup item
                                player_item_pickup(player, sprite);
                                clear_entity(sprite);
                                save_world_level_to_file(
                                    entities_list.get_all_entities().Keys.ToList(),
                                    foreground_entities,
                                    background_entities,
                                    level_loaded_map[current_level_id],
                                    Constant.level_mod_prefix + current_level_id
                                );
                            }
                        }
                    }
                }
            }
            Constant.profiler.end("world_check_entity_collisions_player_interaction");

            //add to checkpoint interactions
            checkpoint_interact_elapsed += (float)gameTime.ElapsedGameTime.TotalMilliseconds;

            //check player-geometry collisions
            Constant.profiler.start("world_check_entity_collisions_player_geometry_collision");
            foreach (IEntity e in collision_geometry) {
                if (Vector2.Distance(e.get_base_position(), player.get_base_position()) < (render_distance/2) && !editor_active) {
                    if (e is StackedObject) {
                        StackedObject obj = (StackedObject)e;
                        bool collision = obj.check_hitbox_collisions(player.get_future_hurtbox());

                        //check and set interaction box
                        if (obj.get_interaction_box() != null) {
                            //interaction is enabled for this stacked object so set display interaction accordingly
                            bool interaction_collision = obj.get_interaction_box().collision(player.get_hurtbox());
                            obj.set_display_interaction(interaction_collision);
                        }

                        //specific check for cracked rocks / objects player can dash through
                        if (collision && e.get_id().Equals("cracked_rocks") && player.is_dashing()) {
                            //if there is a collision with cracked rocks specifically and the player is dashing, ignore the collision
                            collision_geometry_map[e] = false;
                        } else {
                            //default for stacked objects, just set the collision in the map
                            collision_geometry_map[e] = collision;
                        }

                        //player hitbox active
                        if (player.hitbox_active()) {
                            ICollisionEntity ic = (ICollisionEntity)e;
                            //don't need to check if hurtbox is active, it's an inanimate object, it should always be active
                            bool hitbox_collision = player.check_hitbox_collisions(ic.get_hurtbox());
                            if (hitbox_collision) {
                                if (e.get_id().Equals("box")) {
                                    //add particles for effect
                                    particle_systems.Add(new ParticleSystem(true, Constant.rotate_point(e.get_base_position(), camera.Rotation, 1f, Constant.direction_up), 2, 500f, 1, 5, 1, 3, Constant.white_particles, new List<Texture2D>() { Constant.footprint_tex }));
                                    //remove box
                                    clear_entity(e);
                                    //shake the camera
                                    set_camera_shake(Constant.camera_shake_milliseconds, Constant.camera_shake_angle, Constant.camera_shake_hit_radius);
                                    //add arrow charge because we hit something
                                    player.add_arrow_charge(1);
                                    play_spatial_sfx(Constant.hit1_sfx, e.get_base_position(), ((float)random.Next(-1, 2)), get_render_distance());
                                } else if (e.get_id().Equals("hitswitch")) {
                                    HitSwitch hs = (HitSwitch)ic;
                                    if (hs.is_hurtbox_active()) {
                                        hs.take_hit(player, 0);
                                        //shake the camera
                                        set_camera_shake(Constant.camera_shake_milliseconds, Constant.camera_shake_angle, Constant.camera_shake_hit_radius);
                                        //add arrow charge because we hit something
                                        player.add_arrow_charge(1);
                                    }
                                    play_spatial_sfx(Constant.hit1_sfx, e.get_base_position(), ((float)random.Next(-1, 2)), get_render_distance());
                                }
                            }
                        }

                        //check player-stacked object interactions
                        if (player.interacting()) {
                            if (e.get_id().Equals("checkpoint") && checkpoint_interact_elapsed >= checkpoint_interact_threshold) {
                                //save player checkpoint
                                checkpoint_interact_elapsed = 0f;
                                //update player chip position
                                update_player_chip(player.get_base_position());
                                //set current level id to checkpoint level
                                checkpoint_level_id = current_level_id;
                                //save modification to level file
                                save_world_level_to_file(
                                    entities_list.get_all_entities().Keys.ToList(),
                                    foreground_entities,
                                    background_entities,
                                    level_loaded_map[current_level_id],
                                    Constant.level_mod_prefix + current_level_id
                                );
                                //add particle effects to signify it worked
                                particle_systems.Add(new ParticleSystem(true, Constant.rotate_point(e.get_base_position(), camera.Rotation, 1f, Constant.direction_up), 2, 500f, 1, 5, 1, 3, Constant.green_particles, new List<Texture2D>() { Constant.footprint_tex }));
                                Console.WriteLine("updating player chip position...");
                            }
                        }
                    }

                    if (e is InvisibleObject) {
                        InvisibleObject io = (InvisibleObject)e;
                        bool collision = io.check_hitbox_collisions(player.get_hurtbox());
                        if (collision && !player.is_dashing() && !editor_active) {
                            Console.WriteLine("death!");
                            //transition to beginning of the level (reload world)
                            if (!transition_active) {
                                set_transition(true, current_level_id, null);
                            }
                        }
                    }
                } else {
                    if (e is StackedObject) {
                        collision_geometry_map[e] = false;
                    }
                }
            }
            player.set_collision_geometry_map(collision_geometry_map, collision_tile_map);
            Constant.profiler.end("world_check_entity_collisions_player_geometry_collision");

            //check projectile collision against geometry
            //TODO: why isn't collision geometry a collection of icollisionentity?
            Constant.profiler.start("world_check_entity_collisions_projectile_collision");
            foreach (IEntity e in collision_geometry) {
                foreach (IEntity proj in projectiles) {
                    //make sure we are not checking the same entity
                    if (!e.Equals(proj)) {
                        if (e is StackedObject && !e.get_id().Equals("hitswitch")) {
                            //check only for collisions with certain defined collision entities
                            if (Constant.projectile_collision_identifiers.Contains(e.get_id())) {
                                StackedObject so = (StackedObject)e;
                                if (proj is Arrow) {
                                    Arrow a = (Arrow)proj;
                                    bool collision = so.get_hurtbox().collision(a.get_hitbox());
                                    if (collision) {
                                        a.move_arrow(Vector2.Zero);
                                        //if the shot is not a power shot then clear it on impact immediately
                                        //it will be cleared when the speed runs out (dead)
                                        if (!a.is_power_shot()) { entities_to_clear.Add(a); }
                                    }
                                }
                            }
                        } else if (e is StackedObject && e.get_id().Equals("hitswitch")) {
                            //arrow collision with switch
                            HitSwitch hs = (HitSwitch)e;
                            if (hs.is_hurtbox_active()) {
                                if (proj is Arrow) {
                                    Arrow a = (Arrow)proj;
                                    bool collision = hs.get_hurtbox().collision(a.get_hitbox());
                                    if (collision) {
                                        hs.take_hit(a, 0);
                                        //shake the camera
                                        set_camera_shake(Constant.camera_shake_milliseconds, Constant.camera_shake_angle, Constant.camera_shake_hit_radius);
                                        //play hit sound effect
                                        play_spatial_sfx(Constant.hit1_sfx, hs.get_base_position(), 0f, get_render_distance());
                                    }
                                }
                            }
                        }
                    }
                }
            }
            Constant.profiler.end("world_check_entity_collisions_projectile_collision");
            
            //check projectiles against player
            Constant.profiler.start("world_check_entity_collisions_player_projectile_collision");
            foreach (IEntity proj in projectiles) {
                if (proj is Arrow) {
                    //check collision against player
                    Arrow arrow = (Arrow)proj;
                    bool collision = player.get_hurtbox().collision(arrow.get_hitbox());
                    if (collision) {
                        player.take_hit(arrow, 1);
                        arrow.set_dead(true);
                        entities_to_clear.Add(arrow);
                        //shake the camera
                        set_camera_shake(Constant.camera_shake_milliseconds, Constant.camera_shake_angle, Constant.camera_shake_hit_radius);
                        //play hit sound effect
                        play_spatial_sfx(Constant.hit1_sfx, player.get_base_position(), 0f, get_render_distance());
                    }
                }
            }
            Constant.profiler.end("world_check_entity_collisions_player_projectile_collision");

            //check dropped item collision
            Constant.profiler.start("world_check_entity_collisions_player_dropped_item_collision");
            foreach (KeyValuePair<IEntity, float> kv in dropped_items) {
                //every entity in here should be a collision entity, if not something is really messed up
                IEntity e = kv.Key;
                //convert to collision entity
                ICollisionEntity ic = (ICollisionEntity)e;
                bool collision = player.get_hurtbox().collision(ic.get_hurtbox());
                if (collision) {
                    //entity specific logic for different dropped items
                    if (e.get_id().Equals("crystal_money")) {
                        //collect money and remove entity
                        clear_entity(e);
                        //add money to player
                        player.set_money(player.get_money() + 1);
                        play_spatial_sfx(Constant.coin_pickup_sfx, player.get_base_position(), ((float)random.Next(-2, 1))/4f, render_distance, -0.15f);
                    }
                }
            }
            Constant.profiler.end("world_check_entity_collisions_player_dropped_item_collision");

            //check collisions with interactable plants
            Constant.profiler.start("world_check_entity_collisions_player_plant_hitbox_collision");
            foreach (IEntity e in plants) {
                //distance calculation to cut down on how many interactions we are checking
                if (Vector2.Distance(player.get_base_position(), e.get_base_position()) <= render_distance && player.hitbox_active()) {
                    if (e is ICollisionEntity) {
                        ICollisionEntity ic = (ICollisionEntity)e;
                        if (e.get_id().Equals("grass2")) {
                            StackedObject so = (StackedObject)e;
                            bool collision = player.check_hitbox_collisions(ic.get_hurtbox());
                            if (collision && so.get_texture() != Constant.short_grass_tex) {
                                //player has "cut" the grass so set the stack texture to the short grass texture
                                so.set_texture(Constant.short_grass_tex, 32, 32, 4, Constant.stack_distance1);
                                particle_systems.Add(new ParticleSystem(true, Constant.rotate_point(e.get_base_position(), camera.Rotation, 1f, Constant.direction_up), 2, 500f, 1, 5, 1, 3, Constant.green_particles, new List<Texture2D>() { Constant.footprint_tex }));
                                //random chance to drop money (16% chance)
                                if (random.Next(0, 6) == 1) {
                                    add_dropped_item(
                                        new AnimatedSprite(Constant.crystal_tex, e.get_base_position(), 0.8f, 
                                            150f, 9, 0, 0, 16, 16, //animation variables
                                            "crystal_money", get_editor_object_idx()
                                        ),
                                        0f
                                    );
                                }
                            }
                        }
                    }
                }
            }
            Constant.profiler.end("world_check_entity_collisions_player_plant_hitbox_collision");
        }

        private void player_item_pickup(Player p, IEntity item) {
            //split string to pull first string part to identify the attribute
            string player_attribute = item.get_id().Split("_")[0];
            switch (player_attribute) {
                case "dash":
                    p.set_attribute(player_attribute, true);
                    //if the player currently has more dash charges and picks this item up again somehow we do not want them to have their charges reset
                    if (p.get_dash_charge() <= Constant.player_default_dash_charge) {
                        //set dash charges
                        p.set_attribute_charges(player_attribute, Constant.player_default_dash_charge);
                    }
                    break;
                case "bow":
                    p.set_attribute(player_attribute, true);
                    if (p.get_max_arrows() <= Constant.player_default_max_arrows) {
                        //set max arrows
                        p.set_attribute_charges(player_attribute, Constant.player_default_max_arrows);
                        //refill arrow charges
                        p.add_arrow_charge(Constant.player_default_max_arrows);
                    }
                    break;
                case "sword":
                    p.set_attribute(player_attribute, true);
                    if (p.get_attack_charge() <= Constant.player_default_attack_charge) {
                        //set attach charges
                        p.set_attribute_charges(player_attribute, Constant.player_default_attack_charge);
                    }
                    break;
                default:
                    //nothing
                    break;
            }
            //write player attributes to file
            p.save_player_attributes();
        }

        private void check_triggers(GameTime gameTime, float rotation) {
            bool triggered = false;
            ITrigger trigger = null;

            foreach (ITrigger t in triggers) {
                if (Vector2.Distance(player.get_base_position(), t.get_trigger_collision_box().position) < render_distance) {
                    t.Update(gameTime, rotation);

                    //if trigger volume is triggered, set trigger to check, break and then handle trigger set
                    if (t.is_triggered()) {
                        triggered = true;
                        trigger = t;
                        break;
                    }
                }
            }

            //check trigger set and execute logic based on specific trigger
            if (triggered && trigger != null) {
                //individual trigger logic
                if (trigger.get_trigger_type() == TriggerType.Level) {
                    string level_id = ((LevelTrigger)trigger).get_level_id();
                    if (!transition_active) {
                        Console.WriteLine("LEVEL TRANSITION!");
                        Console.WriteLine($"level_transition:{level_id}");
                        //save level
                        save_world_level_to_file(
                            entities_list.get_all_entities().Keys.ToList(),
                            foreground_entities,
                            background_entities,
                            level_loaded_map[current_level_id],
                            Constant.level_mod_prefix + current_level_id
                        );
                        //transition to next level
                        set_transition(true, level_id, (LevelTrigger)trigger);
                        triggered = false;
                        //set trigger back to false
                        trigger.set_triggered(false);
                        return;
                    }
                } else if (trigger.get_trigger_type() == TriggerType.Rotation) {
                    //only set goal rotation if the rotation is not currently active
                    if (!rotation_active) {
                        float goal_rotation_value = ((RotationTrigger)trigger).get_rotation_value();
                        //Console.WriteLine($"setting goal rotation value:{goal_rotation_value}");
                        set_goal_rotation(true, goal_rotation_value);
                    }
                    //set triggered back to false
                    triggered = false;
                    //set the actual trigger back to false
                    trigger.set_triggered(false);
                    return;
                } else if (trigger.get_trigger_type() == TriggerType.Script) {
                    //check trigger for previous trigger value and retrigger value
                    bool previously_activated = ((ScriptTrigger)trigger).get_previously_activated();
                    bool retrigger = ((ScriptTrigger)trigger).get_retrigger();
                    //if the trigger has not been previously activated or we want to retrigger it (retrigger == true)
                    if (!previously_activated || (retrigger)) {
                        //set up variables to run script on next game update tick
                        //set current trigger
                        current_script_trigger = (ScriptTrigger)trigger;
                        //set gameplay script trigger to true
                        gameplay_script_trigger = true;
                    }

                    //set actual trigger back to false
                    //previously_activated will bet set to true after the script finishes running
                    triggered = false;
                    trigger.set_triggered(false);
                    return;
                }
                trigger = null;
            }
        }
        #endregion

        public void add_floor_entity(FloorEntity e) {
            //add entity to list
            floor_entities.Add(e);
            //sort by weight
            floor_entities = floor_entities.OrderByDescending(x => x.get_draw_weight()).ToList();
        }

        public IEntity search_non_render_list_entity(int id) {
            //search floor entities
            foreach (FloorEntity fe in floor_entities) {
                if (fe is IEntity) {
                    IEntity e = (IEntity)fe;
                    if (e.get_obj_ID_num() == id) {
                        return e;
                    }
                }
            }
            //search invisible objects
            foreach (IEntity e in collision_geometry) {
                if (e is InvisibleObject) {
                    if (e.get_obj_ID_num() == id) {
                        return e;
                    }
                }
            }
            return null;
        }

        public void remove_floor_entity(int id){
            IEntity e = find_entity_by_id(id);
            if (!(e is FloorEntity)) {
                Console.WriteLine($"attempting to remove entity from floor entities that is not a FloorEntity (obj_id:{id}). ignoring...");
                return;
            }
            FloorEntity fe  = (FloorEntity)e;
            floor_entities.Remove(fe);
            if (e is TempTile) {
                TempTile tt = (TempTile)e;
                temp_tiles.Remove(tt);
            }
        }

        public void remove_floor_entity(FloorEntity e) {
            floor_entities.Remove(e);
            if (e is TempTile) {
                TempTile tt = (TempTile)e;
                temp_tiles.Remove(tt);
            }
        }

        public void add_temp_tile(TempTile tt) {
            add_floor_entity(tt);
            temp_tiles.Add(tt);
            collision_entities.Add(tt);
        }

        public void add_dropped_item(IEntity e, float elapsed) {
            dropped_items.Add(e, elapsed);
            entities_list.Add(e);
        }

        public void update_dropped_items(GameTime gameTime, float rotation) {
            List<IEntity> keys = new List<IEntity>(dropped_items.Keys);
            foreach (IEntity k in keys) {
                //increase counter by updating value in dictionary
                float item_elapsed = dropped_items[k];
                item_elapsed += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
                dropped_items[k] = item_elapsed;
                //15 second timer for dropped items (15000 milliseconds)
                if (item_elapsed >= 15000f) {
                    clear_entity(k);
                }
            }
        }

        public void update_player_chip(Vector2 player_chip_position) {
            //access editor only objects and find player chip
            IEntity player_chip = null;
            foreach (IEntity e in editor_only_objects) {
                if (e.get_id().Equals("player_chip")) {
                    player_chip = e;
                    break;
                }
            }
            if (player_chip != null) {
                //set player_chip position to passed in position
                player_chip.set_base_position(player_chip_position);
            } else {
                throw new Exception("Entity Not Found Error: Player chip was not found within editor only objects when trying to update its position.");
            }
        }

        public void spawn_shade() {
            Console.WriteLine("spawning_shade");
            //set shade level
            shade_level_id = current_level_id;
            //create shade entity and save to modified level file
            Nightmare n = new Nightmare(Constant.player_tex, Constant.player_attack_tex, player.get_base_position(), 1f, Constant.hit_confirm_spritesheet, player, get_editor_object_idx(), "shade");
            n.set_behavior_enabled(false);
            n.set_placement_source((int)ObjectPlacement.Level);
            entities_list.Add(n);
            collision_entities.Add(n);
            enemies.Add(n);
            //NOTE: do not need to set the other ais into this enemy because presumably the level is going to be reloaded so no need to spend time setting it since it will be unloaded shortly
            //increment editor idx because we are saving this entity to the world level file
            increment_editor_idx();
            //spawn particle system
            particle_systems.Add(new ParticleSystem(true, Constant.rotate_point(n.get_base_position(), camera.Rotation, 1f, Constant.direction_up), 2, 500f, 1, 5, 1, 3, Constant.black_particles, new List<Texture2D>() { Constant.footprint_tex }));
            //save level with new shade
            save_world_level_to_file(
                entities_list.get_all_entities().Keys.ToList(),
                foreground_entities,
                background_entities,
                level_loaded_map[current_level_id],
                Constant.level_mod_prefix + current_level_id
            );
            Console.WriteLine("finished spawning shade");
        }

        public void remove_shade_from_level_file(string level_id) {
            Console.WriteLine("removing shade from level file");
            //set up and read level file
            string level_file_path = "levels/";
            //check for existence of modified level file to load for gameplay and world continuity sake
            string modified_level_id = get_modified_level_file(content_root_directory, level_file_path, level_id);
            string file_contents = read_from_file(content_root_directory, level_file_path, modified_level_id);
            /*Read object from contents as json*/
            GameWorldFile world_file_contents = JsonSerializer.Deserialize<GameWorldFile>(file_contents);
            //remove shade from world file contents
            //NOTE:do not need to worry about re-shifting or re-issuing ids because the shade is always guaranteed to be the last entity
            world_file_contents.shade = null;
            //take world file and serialize to json then write to file
            string save_file_contents = JsonSerializer.Serialize(world_file_contents);
            //write
            write_to_file(level_file_path, modified_level_id, save_file_contents);
            Console.WriteLine("finished removing shade from level file");
        }
        
        //function to set all or specific ai entity behavior enabled or disabled
        //can extend this function in the future to also pass in a value to set the ai behavior to a certain type
        public void set_ai_behavior_disabled(bool value, int? entity_id = null) {
            if (entity_id != null && entity_id.HasValue){
                //search and set specific entity behavior disabled value
                IEntity e = entities_list.get_entity_by_id(entity_id.Value);
                if (e is IAiEntity) {
                    //type check to make sure we're doing the right thing
                    IAiEntity ai = (IAiEntity)e;
                    //see comment below in for loop for why we flip the value
                    ai.set_behavior_enabled(!value);
                }
            } else {
                //set all
                //build list of all ai_entities
                List<IAiEntity> all_ai_entities = new List<IAiEntity>();
                all_ai_entities.AddRange(enemies);
                all_ai_entities.AddRange(npcs);
                foreach (IAiEntity ai in all_ai_entities) {
                    //value comes in as whether or not it is disabled, so we flip the value and pass to enable function
                    ai.set_behavior_enabled(!value);
                }
            }
        }
        
        #region level_transitions
        //function to set up transition
        //NOTE: make sure that you check for !transition_active before invoking the function,
        //otherwise the update() will run again and the transition will continually trigger without ever completing, thereby just freezing the game (softlock)
        public void set_transition(bool value, string level_id, LevelTrigger level_trigger) {
            //set transition to active and set elapsed and next level variables
            transition_active = value;
            next_level_id = level_id;
            transition_elapsed = 0f;
            player.set_movement_disabled(true);
            //set current level trigger
            current_level_trigger = level_trigger;
        }

        public void level_transition(GameTime gameTime, string n_level_id, LevelTrigger level_trigger) {
            //calculate how long the transition has taken
            transition_elapsed += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
            //calculate the percentage based on how long the transition has played
            transition_percentage = transition_elapsed / transition_threshold;
            //clamp values to between zero and 1
            transition_percentage = Math.Clamp(transition_percentage, 0f, 1f);
            //check for active transition
            if (transition_active) {
                //handle transition to black (transition1)
                if (!transition1_complete && transition_elapsed >= transition_threshold) {
                    transition_elapsed = 0f;
                    //once the transition time has fully expired we have fully transitioned to a black screen
                    //unload current level, load next level
                    unload_level();
                    if (level_trigger != null) {
                        load_level(content_root_directory, _graphics, n_level_id, level_trigger.get_entry_position());
                        Update(gameTime);
                    } else {
                        load_level(content_root_directory, _graphics, n_level_id);
                        Update(gameTime);
                    }
                    Update(gameTime);
                    //set transition 1 to complete
                    transition1_complete = true;
                } else {
                    //handle transition out of black (transition 2)
                    if (transition1_complete && transition_elapsed >= transition_threshold) {
                        //set transition active to false
                        transition_elapsed = 0f;
                        transition_active = false;
                        //set transition1 back to incomplete for next transition
                        transition1_complete = false;
                        player.set_movement_disabled(false);
                        //tether camera back to player
                        player_camera_tethered = true;
                        transition_percentage = 0;
                        next_level_id = null;
                    }
                }
            }
        }

        public void set_goal_rotation(bool value, float rotation_value) {
            rotation_active = value;
            camera_goal_rotation = MathHelper.ToRadians(rotation_value);
        }

        public void update_goal_rotation(GameTime gameTime) {
            if (rotation_active) {
                //calculate which way to rotate is closer
                camera.Rotation = MathHelper.Lerp(camera.Rotation, camera_goal_rotation, 0.0005f * (float)gameTime.ElapsedGameTime.TotalMilliseconds);
                float diff = Math.Abs(camera.Rotation - camera_goal_rotation);
                if (diff < 0.1f) {
                    //deactivate rotation when we are within the threshold of 0.1
                    rotation_active = false;
                }
            }
        }
        #endregion

        public void clear_entities() {
            //handle cleaning up weapons
            foreach (IEntity e in entities_to_clear) {
                //if this is a collision entity then remove it
                if (collision_entities.Contains(e)) {
                    collision_entities.Remove(e);
                }
                //remove from collisions geometry
                if (collision_geometry.Contains(e)) {
                    collision_geometry.Remove(e);
                }
                if (collision_geometry_map.ContainsKey(e)) {
                    collision_geometry_map.Remove(e);
                }
                //remove from plant entities
                if (plants.Contains(e)) {
                    plants.Remove(e);
                }
                //remove from foreground entities
                if (e is ForegroundEntity) {
                    ForegroundEntity fe = (ForegroundEntity)e;
                    if (foreground_entities.Contains(fe)) {
                        foreground_entities.Remove(fe);
                    }
                }
                //remove from background entities
                if (e is BackgroundEntity) {
                    BackgroundEntity be = (BackgroundEntity)e;
                    if (background_entities.Contains(be)) {
                        background_entities.Remove(be);
                    }
                }
                //remove from floor entities
                if (e is FloorEntity) {
                    FloorEntity fe = (FloorEntity)e;
                    if (floor_entities.Contains(fe)) {
                        floor_entities.Remove(fe);
                    }
                }
                //remove from temp tiles
                if (e is TempTile) {
                    TempTile tt = (TempTile)e;
                    if (temp_tiles.Contains(tt)) {
                        temp_tiles.Remove(tt);
                    }
                }
                //remove from dropped items
                if (dropped_items.ContainsKey(e)) {
                    dropped_items.Remove(e);
                }
                if (collision_geometry_map.ContainsKey(e)) {
                    collision_geometry_map.Remove(e);
                }
                if (collision_tile_map.ContainsKey(e)) {
                    collision_tile_map.Remove(e);
                }
                //switches
                if (e is HitSwitch) {
                    HitSwitch hs = (HitSwitch)e;
                    if (switches.Contains(hs)) {
                        switches.Remove(hs);
                    }
                }
                //if this is an ai, remove it from enemies
                if (e is IAiEntity) {
                    IAiEntity ai = (IAiEntity)e;
                    enemies.Remove(ai);
                }
                //if this is a projectile, remove it from projectiles
                if (projectiles.Contains(e)) {
                    projectiles.Remove(e);
                }
                //hard delete from entities list
                entities_list.Delete_Hard(e);
            }
            //clear out list after deletion has finished
            entities_to_clear.Clear();
        }

        public void clear_entity(IEntity entity_to_clear) {
            entities_to_clear.Add(entity_to_clear);
        }

        public void clear_entity_by_id(int obj_id) {
            IEntity e = entities_list.get_entity_by_id(obj_id);
            if (e == null) {
                Console.WriteLine($"RenderList Entity Not Found: no entity found with corresponding id: {obj_id}");
                return;
            }
            clear_entity(e);
        }

        //find entity by id (O(n)-time)
        //search all entities in the world
        public IEntity find_entity_by_id(int obj_id) {
            return entities_list.get_entity_by_id(obj_id);
        }

        //NOTE: TODO: need to update this to search all lists for the entity collision
        public IEntity find_entity_colliding(RRect r) {
            foreach (IEntity e in collision_entities) {
                //cast to collision entity
                Console.WriteLine($"entity_check:{e.get_id()}");
                if (e is ICollisionEntity) {
                    ICollisionEntity ce = (ICollisionEntity)e;
                    if (ce.get_hurtbox().collision(r)) {
                        return e;
                    }
                }
            }
            foreach (IEntity e in collision_geometry) {
                //cast to collision entity
                if (e is ICollisionEntity) {
                    ICollisionEntity ce = (ICollisionEntity)e;
                    if (ce.get_hurtbox().collision(r)) {
                        return e;
                    }
                }
            }
            foreach (IEntity e in plants) {
                if (e is ICollisionEntity) {
                    ICollisionEntity ce = (ICollisionEntity)e;
                    if (ce.get_hurtbox().collision(r)) {
                        return e;
                    }
                }
            }
            return null;
        }

        public ParticleSystem find_particle_system_colliding(RRect r) {
            foreach (ParticleSystem ps in particle_systems) {
                if (ps.get_hurtbox().collision(r)) {
                    return ps;
                }
            }
            return null;
        }

        public void add_world_particle_system(ParticleSystem ps) {
            particle_systems.Add(ps);
        }

        public FloorEntity find_floor_entity_colliding(RRect r) {
            //collision by distance tobase position?
            //since floor entities do not have collision geometry?
            float distance_threshold = 50f;
            //loop over floor entities
            foreach (FloorEntity fe in floor_entities) {
                //cast to tile
                if (fe is Tile) {
                    Tile tile_entity = (Tile)fe;
                    //calculate distance to tile from mouse_position
                    if (Vector2.Distance(tile_entity.draw_position, mouse_hitbox.position) < distance_threshold) {
                        //return
                        return fe;                        
                    }
                }
            }
            return null;
        }

        public Camera get_camera() {
            return this.camera;
        }

        public void resize_viewport(GraphicsDeviceManager _graphics) {
            //update the position of textboxes to fit on the screen
            Constant.textbox_screen_position = new Vector2(10, _graphics.GraphicsDevice.Viewport.Height - (_graphics.GraphicsDevice.Viewport.Height / 3));
            float bottom_screen_distance = Constant.window_height - Constant.textbox_screen_position.Y;
            if (bottom_screen_distance <= 10) {
                
            }
            //update camera viewport
            camera.set_camera_offset(Vector2.Zero);
            camera.update_viewport(_graphics.GraphicsDevice.Viewport, Constant.rotate_point(player.base_position, -camera.Rotation, 0f, Constant.direction_down));
        }

        private void set_camera_shake(float shake_milliseconds, float angle, float radius) {
            shake_threshold = shake_milliseconds;
            camera_shake = true;
            shake_radius = radius;
            shake_angle = angle;
        }

        private void shake_camera(GameTime gameTime) {
            if (shake_elapsed < shake_threshold && camera_shake) {
                //increase timer
                shake_elapsed += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
                //shake camera around center point via adding an offset
                camera.set_camera_offset(new Vector2((float)(Math.Sin(shake_angle) * shake_radius), (float)(Math.Cos(shake_angle) * shake_radius)));
                shake_radius -= 0.25f;
                shake_angle += (150 + random.Next(60));
                if (shake_radius <= 0) { //end camera shake if we exhaust the radius
                    camera_shake = false;
                    shake_elapsed = 0;
                    camera.set_camera_offset(Vector2.Zero);
                }
            } else {
                camera_shake = false;
                shake_elapsed = 0;
                camera.set_camera_offset(Vector2.Zero);
            }
        }

        public void set_camera_rotation_enabled(bool value) {
            this.player_camera_rotate_enabled = value;
        }

        public void add_projectile(IEntity projectile_entity) {
            projectiles.Add(projectile_entity);
            entities_list.Add(projectile_entity);
        }

        public bool is_editor_active() {
            return editor_active;
        }

        public List<Vector2> generate_list_of_points(RRect r, int num_points, float distance) {
            List<Vector2> random_points = new List<Vector2>();
            
            while (random_points.Count < num_points) {
                Vector2 p = r.top_left_position;
                float r_x = (float)random.Next(0, (int)r.width) + brush_box.width/5;
                float r_y = (float)random.Next(0, (int)r.height) + brush_box.height/5;

                Vector2 r_point = p + new Vector2(r_x, r_y);

                //r_point = Vector2.Transform(r_point - r.position, Matrix.CreateRotationZ(camera.Rotation)) + r.position;

                // Check distance from all other points
                bool is_valid = true;
                foreach (var point in random_points) {
                    if (Vector2.Distance(r_point, point) < distance) {
                        is_valid = false;
                        break;
                    }
                }

                // If valid, add the point
                if (is_valid) {
                    random_points.Add(r_point);
                }
            }

            return random_points;
        }

        public float get_render_distance() {
            return render_distance;
        }

        public Player get_player() {
            return player;
        }

        public void update_textbox_scale(Canvas canvas) {
            //scale ui elements
            //to be used in Game1 class on window scaling
            //textboxes
            List<TextBox> textboxes = get_all_textboxes();
            foreach (TextBox t in textboxes) {
                if (t != null) {
                    t.set_width(
                        t.get_original_width()*canvas.get_current_scale()
                    );
                    t.set_height(
                        t.get_original_height()*canvas.get_current_scale()
                    );
                }
            }
        }

        #region sounds
        public void play_spatial_sfx(SoundEffect sfx, Vector2 sfx_position, float pitch, float player_hearing_distance, float volume_offset = 0f) {
            sound_manager.play_spatial_sfx(sfx, sfx_position, player.get_base_position(), pitch, player_hearing_distance, volume_offset);
        }
        #endregion
        
        #region draw
        public void Draw(SpriteBatch _spriteBatch){
            Constant.profiler.start("world_entities_draw");
            // TODO: Add drawing code here

            _spriteBatch.Begin(SpriteSortMode.Deferred,
                                BlendState.AlphaBlend,
                                SamplerState.PointClamp, null, null, null,
                                camera.Transform);
            //draw floor itself
            //note: this draws entities in order, meaning our sorting after 
            //insertion should yield the proper back to front ordering that needs to be drawn
            for (int i = 0; i < floor_entities.Count; i++) {
                if (Vector2.Distance(floor_entities[i].get_base_position(), camera.get_camera_center()) < render_distance) {
                    floor_entities[i].Draw(_spriteBatch);
                }
            }
            //draw background objects (stuff on the floor)
            for (int i = 0; i < background_entities.Count; i++) {
                if (Vector2.Distance(background_entities[i].get_base_position(), player.get_base_position()) < render_distance) {
                    background_entities[i].Draw(_spriteBatch);
                }
            }

            //draw entities list
            if (player_camera_tethered) {
                entities_list.Draw(_spriteBatch, player.get_base_position(), render_distance);
            } else {
                entities_list.Draw(_spriteBatch, camera.get_camera_center(), render_distance);
            }

            /*PARTICLE SYSTEMS*/
            foreach (ParticleSystem ps in particle_systems) {
                if (Vector2.Distance(ps.get_base_position(), player.get_base_position()) <= render_distance) {
                    ps.Draw(_spriteBatch);
                }
            }

            //debug triggers
            if (debug_triggers) {
                //draw triggers
                foreach (ITrigger t in triggers) {
                    t.Draw(_spriteBatch);
                }
                foreach(IEntity i in collision_geometry) {
                    if (i.get_id().Equals("deathbox")) {
                        i.Draw(_spriteBatch);
                    }
                }
            }


            //draw foreground objects
            foreach (ForegroundEntity f in foreground_entities) {
                f.Draw(_spriteBatch);
            }
            
            //draw camera bounds
            // if (camera_bounds != null && debug_triggers) {
            //     //draw bounds rectangle
            //     camera_bounds.draw(_spriteBatch);
            //     //draw viewport boundary points
            //     foreach (Vector2 v in camera.get_viewport_boundary_points()) {
            //         Renderer.FillRectangle(_spriteBatch, v, 10, 10, Color.Blue);
            //     }
            // }

            //draw editor elements
            if (editor_active) {
                //tool specific draw
                if (editor_tool_idx == 0) {
                    //draw preview of object
                    IEntity selected_entity = obj_map[selected_object];
                    selected_entity.Draw(_spriteBatch);
                } else if (editor_tool_idx == 3 || editor_tool_idx == 5) {
                    _spriteBatch.Draw(editor_tiles_map[editor_tile_idx].Item1, create_position, null, Color.White);
                } else if (editor_tool_idx == 4) {
                    //draw brush area
                    brush_box.draw(_spriteBatch);
                }
                _spriteBatch.DrawString(Constant.arial_small, $"{editor_tool_name_map[editor_tool_idx]}", mouse_hitbox.position, Color.Black);
                
                Renderer.FillRectangle(_spriteBatch, create_position, 10, 10, Color.Purple);
                mouse_hitbox.draw(_spriteBatch);

                condition_manager.Draw(_spriteBatch);

                //draw editor only entities
                foreach (IEntity e in editor_only_objects) {
                    e.Draw(_spriteBatch);
                }

                //draw sound effects in editor
                sound_manager.Draw(_spriteBatch);
            }
            
            //draw_lights_world_space(lights, collision_geometry, collision_entities, light_excluded_entities, _spriteBatch);

            _spriteBatch.End();
            
            /* LIGHTING PASS */
            if (lights_enabled) {
                //draw lights over top of the scene if lights are enabled
                draw_lights(lights, collision_geometry, collision_entities, light_excluded_entities, _spriteBatch);
            }

            //draw transition
            if (transition_active) {
                _spriteBatch.Begin();
                //fill screen with differently faded black depending on which stage of transition we are in
                if (!transition1_complete) {
                    //transition to black (transition1)
                    Renderer.FillRectangle(_spriteBatch, Vector2.Zero, _graphics.GraphicsDevice.PresentationParameters.BackBufferWidth, _graphics.GraphicsDevice.PresentationParameters.BackBufferHeight, Color.Black, transition_percentage);
                } else {
                    //transition out of black (transition2)
                    Renderer.FillRectangle(_spriteBatch, Vector2.Zero, _graphics.GraphicsDevice.PresentationParameters.BackBufferWidth, _graphics.GraphicsDevice.PresentationParameters.BackBufferHeight, Color.Black, (1 - transition_percentage));
                }
                _spriteBatch.End();
            }
            Constant.profiler.end("world_entities_draw");

            if (intro_text_playing) {
                _spriteBatch.Begin();
                if (intro_text_elapsed < intro_text_threshold) {
                    intro_text_opacity = 1f;
                } else {
                    intro_text_opacity = 1 - (intro_text_finish_elapsed / intro_text_finished_threshold);
                }
                Renderer.FillRectangle(_spriteBatch, Vector2.Zero, _graphics.GraphicsDevice.PresentationParameters.BackBufferWidth, _graphics.GraphicsDevice.PresentationParameters.BackBufferHeight, Color.Black, intro_text_opacity);
                //draw text based on timer
                //calculate percentage
                float percentage = MathHelper.Clamp(intro_text_elapsed / intro_text_threshold, 0f, 1f);
                //calculate and return index based on percentage of message length
                int intro_tex_idx = (int)(percentage * (intro_text.Count - 1));
                for (int i = 0; i <= intro_tex_idx; i++) {
                    _spriteBatch.DrawString(Constant.pxf_thin, intro_text[i], Vector2.Zero + new Vector2(0, i * 50), Color.White * intro_text_opacity);
                }
                _spriteBatch.End();
            }
        }

        public void DrawTextOverlays(SpriteBatch _spriteBatch) {
            Constant.profiler.start("world_text_overlays_draw");
            //draw UI
            _spriteBatch.Begin();
            //display dash charges
            _spriteBatch.Draw(Constant.dash_icon, Constant.dash_charge_ui_screen_position, null, Color.White);
            _spriteBatch.DrawString(Constant.pxf_font, $"{player.get_dash_charge()}", Constant.dash_charge_ui_screen_position + new Vector2(32, 0), Color.White);
            //display attack charges
            _spriteBatch.Draw(Constant.sword_icon, Constant.dash_charge_ui_screen_position + new Vector2(0, 32), null, Color.White);
            _spriteBatch.DrawString(Constant.pxf_font, $"{player.get_attack_charge()}", Constant.dash_charge_ui_screen_position + new Vector2(32, 32), Color.White);
            //display health
            _spriteBatch.Draw(Constant.sword_icon, Constant.dash_charge_ui_screen_position + new Vector2(0, 32*2), null, Color.Green);
            _spriteBatch.DrawString(Constant.pxf_font, $"{player.get_health()}", Constant.dash_charge_ui_screen_position + new Vector2(32, 32*2), Color.Green);
            //display arrow charges
            _spriteBatch.Draw(Constant.bow_icon, Constant.dash_charge_ui_screen_position + new Vector2(0, 32*3), null, Color.White);
            _spriteBatch.DrawString(Constant.pxf_font, $"{player.get_arrow_charges()}", Constant.dash_charge_ui_screen_position + new Vector2(32, 32*3), Color.White);
            //display grenade charges
            _spriteBatch.Draw(Constant.bow_icon, Constant.dash_charge_ui_screen_position + new Vector2(0, 32*4), null, Color.Red);
            _spriteBatch.DrawString(Constant.pxf_font, $"{player.get_grenade_charge()}", Constant.dash_charge_ui_screen_position + new Vector2(32, 32*4), Color.Red);
            //display money
            _spriteBatch.DrawString(Constant.pxf_font, $"${player.get_money()}", Constant.dash_charge_ui_screen_position + new Vector2(-64, 32*5), Color.White);

            //draw text overlays
            _spriteBatch.DrawString(Constant.arial_small, "fps:" + fps, new Vector2(0, 17), Color.Black);
            _spriteBatch.DrawString(Constant.arial_small, "camera_rotation: " + camera.Rotation, new Vector2(0, 17*2), Color.Black);
            _spriteBatch.DrawString(Constant.arial_small, "camera_zoom:" + camera.Zoom, new Vector2(0, 17*3), Color.Black);
            _spriteBatch.DrawString(Constant.arial_small, "editor:" + editor_active, new Vector2(0, 17*4), Color.Black);
            //_spriteBatch.DrawString(Constant.arial_small, "selected_object:" + selected_object, new Vector2(0, 17*5), Color.Black);
            _spriteBatch.DrawString(Constant.arial_small, $"selected_object:{selected_object},obj_id:{obj_map[selected_object].get_id()}", new Vector2(0, 17*5), Color.Black);
            _spriteBatch.DrawString(Constant.arial_small, $"editor_tool:{editor_tool_idx}-{editor_tool_name_map[editor_tool_idx]}", new Vector2(0, 17*6), Color.Black);
            _spriteBatch.DrawString(Constant.arial_small, $"editor_layer:{editor_layer}", new Vector2(0, 17*7), Color.Black);
            _spriteBatch.DrawString(Constant.arial_small, $"editor_selected_object_rotation:{editor_object_rotation}", new Vector2(0, 17*8), Color.Black);
            _spriteBatch.DrawString(Constant.arial_small, $"editor_tile_idx:{editor_tile_idx}", new Vector2(0, 17*9), Color.Black);
            _spriteBatch.End();
            Constant.profiler.end("world_text_overlays_draw");
        }

        public void draw_textbox(SpriteBatch spriteBatch) {
            //draw textboxes without camera matrix (screen positioning)
            //draw also without point filtering applied
            spriteBatch.Begin(blendState: BlendState.AlphaBlend, samplerState: SamplerState.PointClamp);
            //draw textboxes on screen
            if ((current_sign != null || current_npc != null) && current_textbox != null) {
                current_textbox.Draw(spriteBatch);
            }
            spriteBatch.End();
        }
        
        #region lights
        //TODO: add exclusionary entities to lights
        //lights should not affect the entity they are being cast by
        //right now if we put a light on a lamppost it will factor in the lamp post into the algorithm to block that light
        //need to add an exclusionary list so if an edge or endpoint of a segment is found in that list or as a part of an entity in that list, then it is ignored from the algorithm
        //also need to add the option to pass in npcs and signs and other non collision objects to affect light and cast shadows
        //they have hurtboxes so we should be able to leverage that geometry to cast shadows as well
        public void draw_lights(List<Light> lights, List<IEntity> geometry_shadow_casters, List<IEntity> collision_entity_shadow_casters, List<IEntity> light_excluded_entities, SpriteBatch spriteBatch) {
            //only render if we have lights
            if (lights.Count > 0) {
                //lights
                spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp);
                spriteBatch.Draw(light_map_target, Vector2.Zero, Color.White);
                spriteBatch.End();

                _graphics.GraphicsDevice.SetRenderTarget(light_map_target);
                _graphics.GraphicsDevice.Clear(Color.Black * 0.25f);

                spriteBatch.Begin(SpriteSortMode.Immediate, Constant.subtract_blend, SamplerState.PointClamp);
                foreach (Light light in lights) {
                    //calculate in range geometry for light
                    light.calculate_in_range_geometry(geometry_shadow_casters, collision_entity_shadow_casters, light_excluded_entities, player.get_base_position(), render_distance);
                    //calculate geometry of this light
                    List<(float, Vector2)> light_geometry = calculate_light_geometry(light, light.get_geometry_edges());
                    Vector2 light_screen_space = Vector2.Transform(light.get_center_position(), camera.Transform);
                    //convert vector into screen space
                    for (int i = 0; i < light_geometry.Count - 1; i++) {
                        Vector2 ray1 = light_geometry[i].Item2;
                        Vector2 ray2 = light_geometry[i+1].Item2;
                        Vector2 ray1_vector_screen_space = Vector2.Transform(ray1, camera.Transform);
                        Vector2 ray2_vector_screen_space = Vector2.Transform(ray2, camera.Transform);
                        Renderer.DrawTri(
                            spriteBatch,
                            light_screen_space,
                            ray1_vector_screen_space,
                            ray2_vector_screen_space,
                            light.get_color() * light.get_intensity(),
                            1f
                        );
                    }
                    //draw last triangle between first and last point (also screen space)
                    Vector2 ray_first_screen_space = Vector2.Transform(light_geometry[0].Item2, camera.Transform);
                    Vector2 ray_last_screen_space = Vector2.Transform(light_geometry[light_geometry.Count-1].Item2, camera.Transform);
                    Renderer.DrawTri(
                        spriteBatch,
                        light_screen_space,
                        ray_first_screen_space,
                        ray_last_screen_space,
                        light.get_color() * light.get_intensity(),
                        1f
                    );
                }
                spriteBatch.End();

                // Set back to the default render target (screen)
                _graphics.GraphicsDevice.SetRenderTarget(null);
            }
        }
        
        public void draw_lights_world_space(List<Light> lights, List<IEntity> geometry_shadow_casters, List<IEntity> collision_entity_shadow_casters, List<IEntity> light_excluded_entities, SpriteBatch spriteBatch) {
            //loop over lights and calculate geometry per light with shadow casters
            foreach (Light light in lights) {
                //calculate in range geometry for light
                light.calculate_in_range_geometry(geometry_shadow_casters, collision_entity_shadow_casters, light_excluded_entities, player.get_base_position(), render_distance);
                //calculate geometry of this light
                List<(float, Vector2)> light_rays = calculate_light_geometry(light, light.get_geometry_edges());
                //draw rays themselves
                foreach ((float, Vector2) ray in light_rays) {
                    Renderer.DrawALine(
                        spriteBatch, 
                        Constant.pixel,
                        2f,
                        Color.Red,
                        1f,
                        light.get_center_position(),
                        ray.Item2
                    );
                }

                //iterate through all rays and draw triangles between rays
                for (int i = 0; i < light_rays.Count - 1; i++) {
                    Vector2 ray1 = light_rays[i].Item2;
                    Vector2 ray2 = light_rays[i+1].Item2;
                    //fill triangles
                    Renderer.DrawTri(
                        spriteBatch, 
                        light.get_center_position(),
                        ray1,
                        ray2,
                        Color.White,
                        0.1f
                    );
                }
                //draw last triangle between first and last point
                // Renderer.DrawTri(
                //     spriteBatch,
                //     light.get_center_position(),
                //     light_rays[0].Item2,
                //     light_rays[light_rays.Count-1].Item2,
                //     Color.White,
                //     0.1f
                // );
            }
        }
        
        //function to calculate light map geometry
        public List<(float, Vector2)> calculate_light_geometry(Light light, List<(Vector2, Vector2)> edges) {
            List<(float, Vector2)> rays = new List<(float, Vector2)>();
            HashSet<float> unique_angles = new HashSet<float>();

            float max_distance = 250f;
            float offset_angle = 0.01f;
            // min rays to cast out for when there is not enough geometry in the scene, but we still have lights
            int min_rays = 100;
            
            //loop over all the edges we are calculating light against
            foreach ((Vector2, Vector2) edge in edges) {
                Vector2 p1 = edge.Item1;
                Vector2 p2 = edge.Item2;

                float angle1 = (float)Math.Atan2(p1.Y - light.get_center_position().Y, p1.X - light.get_center_position().X);
                float angle2 = (float)Math.Atan2(p2.Y - light.get_center_position().Y, p2.X - light.get_center_position().X);
                
                //add to unique angles
                unique_angles.Add(angle1);
                unique_angles.Add(angle1 + offset_angle);
                unique_angles.Add(angle1 - offset_angle);

                unique_angles.Add(angle2);
                unique_angles.Add(angle2 + offset_angle);
                unique_angles.Add(angle2 - offset_angle);
            }

            if (unique_angles.Count < min_rays) {
                int remaining_rays = min_rays - unique_angles.Count;
                //evenly distribute remaining rays
                float angle_increment = MathHelper.TwoPi / remaining_rays;

                for (int i = 0; i < remaining_rays; i++) {
                    float additional_angle = i * angle_increment;
                    unique_angles.Add(additional_angle);
                }
            }
            
            List<float> sorted_angles = unique_angles.OrderBy(a => a).ToList(); //sort clockwise order
            
            //ensure there are no large gaps between consecutive angles
            //this works to keep the light circular as we exit places with higher entity density to cast light off of
            //otherwise the light ends up looking fairly polygonal which kind of destroys the effect
            for (int i = 0; i < sorted_angles.Count - 1; i++) {
                float angle1 = sorted_angles[i];
                float angle2 = sorted_angles[i+1];

                if (angle2 - angle1 > (MathHelper.TwoPi / min_rays)) {
                    float new_angle = (angle1 + angle2) / 2;
                    //add if we don't already have an angle for this value
                    if (!unique_angles.Contains(new_angle)) {
                        unique_angles.Add(new_angle);
                    }
                }
            }
            
            sorted_angles = unique_angles.OrderBy(a => a).ToList();
            //cast rays at each unique angle
            foreach (float angle in sorted_angles) {
                Vector2 ray_direction = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                Vector2 closest_intersection = light.get_center_position() + ray_direction * max_distance;
                float closest_distance = max_distance;

                //check for intersection with each edge
                foreach ((Vector2, Vector2) edge in edges) {
                    Vector2 intersection;
                    if (RRect.ray_intersects_edge(light.get_center_position(), ray_direction, edge.Item1, edge.Item2, out intersection)) {
                        float distance = Vector2.Distance(light.get_center_position(), intersection);
                        if (distance < closest_distance) {
                            closest_distance = distance;
                            closest_intersection = intersection;
                        }
                    }
                }

                rays.Add((angle, closest_intersection));
            }
            
            //return the rays (they are in clockwise order at this point)
            return rays;
        }
        #endregion
        #endregion
    }
}