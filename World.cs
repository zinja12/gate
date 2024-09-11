using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;

namespace gate
{
    public class World
    {
        //Game object
        Game1 game;

        //debug variables
        float fps;

        //Graphics and game objects
        GraphicsDeviceManager _graphics;
        string content_root_directory;
        ContentManager Content;

        //loaded textures
        List<string> loaded_textures;
        //bool loading = false;
        bool debug_triggers = true;

        public string load_file_name = "trails_v1.json", current_level_id;
        //string load_file_name = "sandbox.json";
        string save_file_name = "untitled_sandbox.json";

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
        List<ITrigger> triggers;
        List<IEntity> collision_geometry;
        List<IAiEntity> enemies;
        List<IAiEntity> npcs;
        List<IEntity> projectiles;
        ConditionManager condition_manager;
        Dictionary<IEntity, bool> collision_geometry_map;

        //clear variables
        List<IEntity> entities_to_clear;

        TextBox current_textbox = null;
        Sign current_sign = null;
        NPC current_npc = null;

        float rotation = 0f;
        
        //world variables
        private float render_distance = 600;
        private int freeze_frames = 0;
        //level transition variables
        private bool transition_active = false;
        private string next_level_id = null;
        private float transition_elapsed, transition_threshold = 1000f;
        private float transition_percentage = 0f;
        private bool transition1_complete = false;
        //level rotation variables
        private bool rotation_active = false;
        private float camera_goal_rotation;

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

        //editor variables
        private bool editor_active = false, editor_enabled = true;
        private int selected_object = 1;
        private float selection_cooldown = 200f, selection_elapsed;
        private float editor_object_rotation = 0f, editor_object_scale = 1f;
        //private Dictionary<int, Texture2D> object_map;
        private Dictionary<int, IEntity> obj_map;
        private Vector2 mouse_world_position, create_position;
        private RRect mouse_hitbox;
        private int previous_scroll_value;
        private Keys previous_key;
        private ICondition selected_condition;
        //editor tools: (object_placement, object/linkage editor)
        private int editor_tool_idx, editor_object_idx, editor_tool_count, editor_layer, editor_layer_count;

        //Random variable
        private Random random = new Random();

        /*PARTICLE SYSTEM*/
        private List<ParticleSystem> particle_systems;
        private List<ParticleSystem> dead_particle_systems;

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

            //weapons
            entities_to_clear = new List<IEntity>();

            //create entities list and add player
            entities_list = new RenderList();

            //initialize particle systems for world effects
            this.particle_systems = new List<ParticleSystem>();
            this.dead_particle_systems = new List<ParticleSystem>();

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
            editor_tool_count = 3;
            editor_layer = 0;
            editor_layer_count = 2;
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
            obj_map.Add(15, new Nightmare(Constant.nightmare_tex, Vector2.Zero, 1f, Constant.hit_confirm_spritesheet, player, -1, "nightmare"));
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
            obj_map.Add(22, new StackedObject("grass2", Constant.stacked_grass, Vector2.Zero, 1f, 32, 32, 17, Constant.stack_distance1, 0f, -1));
            obj_map.Add(23, new Tile(Vector2.Zero, 2f, Constant.trail_tex, "trail_tile", (int)DrawWeight.Medium, -1));
            obj_map.Add(24, new Tile(Vector2.Zero, 2f, Constant.sand_tex, "sand_tile", (int)DrawWeight.Medium, -1));
            obj_map.Add(25, new StackedObject("sword", Constant.sword_tex, Vector2.Zero, 1f, 16, 16, 22, Constant.stack_distance1, 0f, -1));
            obj_map.Add(26, new StackedObject("box", Constant.box_spritesheet, Vector2.Zero, 1f, 32, 32, 18, Constant.stack_distance1, 0f, -1));
            obj_map.Add(27, new StackedObject("house", Constant.house_spritesheet, Vector2.Zero, 1f, 128, 128, 54, Constant.stack_distance1, 0f, -1));
            obj_map.Add(28, new PlaceHolderEntity(Vector2.Zero, "ParticleSystem", -1));
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
        public void load_level(string root_directory, GraphicsDeviceManager _graphics, string level_id) {
            //set current level id
            current_level_id = level_id;
            //load context for fx
            Constant.pixelate_effect = Content.Load<Effect>("fx/pixelate");
            Constant.pixelate_effect.Parameters["pixels"].SetValue(Constant.pixels);
            Constant.pixelate_effect.Parameters["pixelation"].SetValue(Constant.pixelation);
            //load content not specific to an object
            Constant.tile_tex = Content.Load<Texture2D>("sprites/tile3");
            Constant.pixel = Content.Load<Texture2D>("sprites/white_pixel");
            Constant.footprint_tex = Content.Load<Texture2D>("sprites/footprint");
            Constant.attack_sprites_tex = Content.Load<Texture2D>("sprites/attack_sprites2");
            Constant.hit_confirm_spritesheet = Content.Load<Texture2D>("sprites/hit_confirm_spritesheet");
            Constant.slash_confirm_spritesheet = Content.Load<Texture2D>("sprites/slash_confirm_spritesheet");
            Constant.shadow_tex = Content.Load<Texture2D>("sprites/shadow0");
            Constant.green_tree = Content.Load<Texture2D>("sprites/green_tree1_1_26");
            //load fonts
            Constant.arial = Content.Load<SpriteFont>("fonts/arial");
            Constant.arial_small = Content.Load<SpriteFont>("fonts/arial_small");
            Constant.arial_mid_reg = Content.Load<SpriteFont>("fonts/arial_mid_reg");
            //load emotions
            Constant.fear_tex = Content.Load<Texture2D>("sprites/fear_anger");
            Constant.anxiety_tex = Content.Load<Texture2D>("sprites/anxiety");
            //set up texture map once textures are loaded
            Constant.emotion_texture_map = Constant.generate_emotion_texture_map();

            //set pixel shader to active once it has been loaded
            game.set_pixel_shader_active(true);

            //debug fps initialization
            game.fps = new FpsCounter(game, Constant.arial_small, Vector2.Zero);
            game.fps.Initialize();

            List<string> loaded_obj_identifiers = new List<string>();

            //set up and read level file
            string level_file_path = "levels/";
            string dialogue_file_path = "dialogue/";
            string file_contents = read_gameworld_file_contents(root_directory, level_file_path, level_id);
            /*Read object from contents as json*/
            GameWorldFile world_file_contents = JsonSerializer.Deserialize<GameWorldFile>(file_contents);
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
                        case "player":
                            if (player_count > 0) {
                                throw new Exception("World File Error: Too many player objects found in world file. There can only be one. :)");
                            }
                            //load textures for player
                            check_and_load_tex(ref Constant.player_tex, "sprites/test_player_spritesheet6");
                            check_and_load_tex(ref Constant.player_dash_tex, "sprites/test_player_dash_spritesheet1");
                            check_and_load_tex(ref Constant.player_attack_tex, "sprites/test_player_attacks_spritesheet5");
                            check_and_load_tex(ref Constant.player_heavy_attack_tex, "sprites/test_player_heavy_attack_spritesheet1");
                            check_and_load_tex(ref Constant.player_charging_tex, "sprites/test_player_charging_spritesheet1");
                            check_and_load_tex(ref Constant.player_aim_tex, "sprites/test_player_bow_aim_spritesheet1");
                            check_and_load_tex(ref Constant.arrow_tex, "sprites/arrow1");
                            //create player object
                            Player p = new Player(obj_position, w_obj.scale, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight, w_obj.object_id_num, this);
                            //read world file player attributes to help initialize player
                            if (world_file_contents.player_attributes != null) {
                                //iterate over player attributes and set active variable for attribute
                                foreach (GameWorldPlayerAttribute pa in world_file_contents.player_attributes) {
                                    p.set_attribute(pa.identifier, pa.active);
                                }
                            }
                            entities_list.Add(p);
                            collision_entities.Add(p);
                            //make sure to set player reference and increment count
                            player = p;
                            player_count++;
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
                            check_and_load_tex(ref Constant.grass_tex, "sprites/grass_0");
                            Grass g = new Grass(obj_position, w_obj.scale, w_obj.object_id_num);
                            plants.Add(g);
                            entities_list.Add(g);
                            break;
                        case "sign":
                            //load texture
                            check_and_load_tex(ref Constant.sign_tex, "sprites/sign1");
                            check_and_load_tex(ref Constant.Y_tex, "sprites/Y");
                            Sign s = new Sign(Constant.sign_tex, obj_position, w_obj.scale, w_obj.sign_messages, w_obj.object_id_num);
                            entities_list.Add(s);
                            collision_entities.Add(s);
                            break;
                        case "ghastly":
                            //load texture
                            check_and_load_tex(ref Constant.ghastly_tex, "sprites/void_essence1");
                            Ghastly ghast = new Ghastly(Constant.ghastly_tex, obj_position, w_obj.scale, Constant.hit_confirm_spritesheet, w_obj.object_id_num);
                            entities_list.Add(ghast);
                            collision_entities.Add(ghast);
                            break;
                        case "marker":
                            //load texture
                            check_and_load_tex(ref Constant.marker_spritesheet, "sprites/marker_spritesheet5");
                            StackedObject m = new StackedObject(w_obj.object_identifier, Constant.marker_spritesheet, obj_position, w_obj.scale, 32, 32, 15, Constant.stack_distance, w_obj.rotation, w_obj.object_id_num);
                            entities_list.Add(m);
                            collision_geometry.Add(m);
                            collision_geometry_map[m] = false;
                            break;
                        case "lamp":
                            //load texture
                            check_and_load_tex(ref Constant.lamppost_spritesheet, "sprites/lamppost_spritesheet3");
                            Lamppost l = new Lamppost(obj_position, w_obj.scale, w_obj.object_id_num);
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
                        case "level_trigger":
                            LevelTrigger lt = new LevelTrigger(obj_position, w_obj.width, w_obj.height, w_obj.level_id, player, w_obj.object_id_num);
                            triggers.Add(lt);
                            break;
                        case "rotation_trigger":
                            RotationTrigger rt = new RotationTrigger(obj_position, w_obj.width, w_obj.height, w_obj.goal_rotation, player, w_obj.object_id_num);
                            triggers.Add(rt);
                            break;
                        case "nightmare":
                            check_and_load_tex(ref Constant.nightmare_tex, "sprites/test_nightmare_spritesheet2");
                            check_and_load_tex(ref Constant.nightmare_attack_tex, "sprites/test_nightmare_attacks_spritesheet1");
                            Nightmare nightmare = new Nightmare(Constant.nightmare_tex, obj_position, w_obj.scale, Constant.hit_confirm_spritesheet, player, w_obj.object_id_num, w_obj.object_identifier);
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
                            NPC npc = new NPC(Constant.player_tex, obj_position, w_obj.scale, 32, (int)AIBehavior.Stationary, dialogue_file, w_obj.npc_conversation_file_id, Constant.hit_confirm_spritesheet, player, w_obj.object_id_num, w_obj.object_identifier);
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
                            add_floor_entity(gt_tile);
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
                            check_and_load_tex(ref Constant.stacked_grass, "sprites/grass1_2_17");
                            StackedObject g2 = new StackedObject(w_obj.object_identifier, Constant.stacked_grass, obj_position, w_obj.scale, 32, 32, 17, Constant.stack_distance1, w_obj.rotation, w_obj.object_id_num);
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
                        default:
                            break;
                    }
                }
                //set editor object_idx to whatever i is for editor current object idx later on
                editor_object_idx = world_file_contents.world_objects.Count - 1;

                //gather all ai entities
                List<IAiEntity> all_ai_entities = new List<IAiEntity>();
                foreach (IAiEntity ai in enemies) { all_ai_entities.Add(ai); }
                foreach (IAiEntity ai in npcs) { all_ai_entities.Add(ai); }
                //set fellow enemies for all ais (this is so each entity knows where the others are to avoid overlaps)
                foreach (IAiEntity ai in enemies) { ai.set_ai_entities(all_ai_entities); }
                foreach (IAiEntity ai in npcs) { ai.set_ai_entities(all_ai_entities); }
                //set up conditions
                for (int i = 0; i < world_file_contents.conditions.Count; i++) {
                    GameWorldCondition w_condition = world_file_contents.conditions[i];
                    editor_object_idx++;
                    ICondition c = null;
                    switch (w_condition.object_identifier) {
                        case "all_enemies_dead_remove_objs":
                            if (w_condition.enemy_ids == null || w_condition.enemy_ids.Count == 0) {
                                c = new EnemiesDeadRemoveObjCondition(editor_object_idx, this, enemies, w_condition.obj_ids_to_remove, new Vector2(w_condition.x_position, w_condition.y_position));
                            } else {
                                c = new EnemiesDeadRemoveObjCondition(editor_object_idx, this, enemies, w_condition.enemy_ids, w_condition.obj_ids_to_remove, new Vector2(w_condition.x_position, w_condition.y_position));
                            }
                            break;
                        default:
                            break;
                    }
                    //add created condition to condition manager
                    condition_manager.add_condition(c);
                }
                //increment editor object idx
                editor_object_idx++;

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

                Console.WriteLine($"editor object idx:{editor_object_idx}");
            } else {
                throw new Exception("Cannot deserialize JSON world objects!");
            }

            if (editor_enabled) {
                //find objects that are not present in the level that we need to load textures for due to editor being active
                var unloaded_objects = Constant.get_object_identifiers().Where(i => loaded_obj_identifiers.All(i2 => i2 != i));
                //load textures not present in the level
                foreach (string i in unloaded_objects) {
                    switch (i) {
                        case "player":
                            check_and_load_tex(ref Constant.player_tex, "sprites/test_player_spritesheet6");
                            check_and_load_tex(ref Constant.player_dash_tex, "sprites/test_player_dash_spritesheet1");
                            check_and_load_tex(ref Constant.player_attack_tex, "sprites/test_player_attacks_spritesheet5");
                            check_and_load_tex(ref Constant.player_heavy_attack_tex, "sprites/test_player_heavy_attack_spritesheet1");
                            check_and_load_tex(ref Constant.player_charging_tex, "sprites/test_player_charging_spritesheet1");
                            check_and_load_tex(ref Constant.player_aim_tex, "sprites/test_player_bow_aim_spritesheet1");
                            check_and_load_tex(ref Constant.arrow_tex, "sprites/arrow1");
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
                            check_and_load_tex(ref Constant.grass_tex, "sprites/grass_0");
                            break;
                        case "sign":
                            check_and_load_tex(ref Constant.sign_tex, "sprites/sign1");
                            check_and_load_tex(ref Constant.Y_tex, "sprites/Y");
                            break;
                        case "ghastly":
                            check_and_load_tex(ref Constant.ghastly_tex, "sprites/void_essence1");
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
                        case "flower":
                            check_and_load_tex(ref Constant.flower_tex, "sprites/flower1_1_12");
                            break;
                        case "grass2":
                            check_and_load_tex(ref Constant.stacked_grass, "sprites/grass1_2_17");
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
                        default:
                            //don't load anything
                            Console.WriteLine($"WARNING: unknown object({i}) in Constant.get_object_identifiers()");
                            break;
                    }
                }
                
                editor_init();
                Console.WriteLine("initialized editor");
            }

            Console.WriteLine("Created all world objects");

            //reset camera on level load
            camera.Rotation = 0f;
            //sort the entities list once so things are not drawn out of order in terms of depth values
            entities_list.sort_list_by_depth(camera.Rotation, player.get_base_position(), render_distance);
        }
        #endregion

        private void check_and_load_tex(ref Texture2D tex, string texture_path) {
            if (!loaded_textures.Contains(texture_path)) {
                //add to loaded textures
                loaded_textures.Add(texture_path);
                tex = this.Content.Load<Texture2D>(texture_path);
                Console.WriteLine("Loaded texture from path:" + texture_path);
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
            collision_geometry.Clear();
            triggers.Clear();
            entities_list.Clear();
            enemies.Clear();
            projectiles.Clear();
            condition_manager.clear_conditions();
            collision_geometry_map.Clear();
            //clear entities
            clear_entities();
            //unload content
            Content.Unload();
            loaded_textures.Clear();

            //set world variables if needed on unload
            rotation_active = false;
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
                level_transition(gameTime, next_level_id);
            }
            
            //keep goal rotations updated
            update_goal_rotation(gameTime);

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
            for (int i = 0; i < entities_list.get_entities().Count; i++) {
                IEntity e = entities_list.get_entities()[i];
                if (e.get_flag().Equals(Constant.ENTITY_ACTIVE)) {
                    e.Update(gameTime, camera.Rotation);
                }
            }
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
                entities_list.sort_list_by_depth(camera.Rotation, player.get_base_position(), render_distance);

                //recalculate what objects need to be within render distance
            }

            #region player_death
            if (player.get_health() <= 0) {
                //player has died, transition and reload the current level
                if (!transition_active) {
                    set_transition(true, current_level_id);
                }
            }

            if (Keyboard.GetState().IsKeyDown(Keys.B)) {
                set_transition(true, current_level_id);
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
                selected_entity.set_base_position(mouse_world_position);
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
                if (editor_tool_idx >= editor_tool_count) {
                    Console.WriteLine("wrap selection down");
                    editor_tool_idx = 0;
                } else if (editor_tool_idx < 0) {
                    Console.WriteLine("wrap selection up");
                    editor_tool_idx = editor_tool_count - 1;
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

                if (Keyboard.GetState().IsKeyDown(Keys.P)) {
                    Console.WriteLine("mouse_world_position:" + mouse_world_position);
                    Console.WriteLine("create_position:" + create_position);
                }
                
                //only able to place objects if the editor_tool_idx == 0 (object placement tool)
                if (editor_tool_idx == 0 && (Keyboard.GetState().IsKeyDown(Keys.M) || Mouse.GetState().LeftButton == ButtonState.Pressed) && selection_elapsed >= selection_cooldown) {
                    selection_elapsed = 0f;
                    MouseState mouse_state = Mouse.GetState();
                    //mouse_world_position = Constant.world_position_transform(new Vector2(Mouse.GetState().X, Mouse.GetState().Y), camera);
                    previous_key = Keys.M;
                    //rotate point with rotation value
                    //create_position = Constant.rotate_point(mouse_world_position, -camera.Rotation, 0f, Constant.direction_down);
                    Console.WriteLine("mouse_world_position:" + mouse_world_position);
                    Console.WriteLine("create_position:" + create_position);
                    switch (selected_object) {
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
                            entities_list.Add(l);
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
                            Nightmare n = new Nightmare(Constant.nightmare_tex, create_position, 1f, Constant.hit_confirm_spritesheet, player, editor_object_idx, "nightmare");
                            n.set_behavior_enabled(false);
                            entities_list.Add(n);
                            collision_entities.Add(n);
                            enemies.Add(n);
                            Console.WriteLine($"nightmare,{create_position.X},{create_position.Y},1,{MathHelper.ToDegrees(editor_object_rotation)}");
                            break;
                        case 16:
                            ICondition cond = new EnemiesDeadRemoveObjCondition(editor_object_idx, this, enemies, new List<int>(), create_position);
                            condition_manager.add_condition(cond);
                            Console.WriteLine("condition created!");
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
                            StackedObject g2 = new StackedObject("grass2", Constant.stacked_grass, create_position, 1f, 32, 32, 17, Constant.stack_distance1, MathHelper.ToDegrees(editor_object_rotation), editor_object_idx);
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
                            collision_geometry.Add(house);
                            collision_geometry_map[house] = false;
                            Console.WriteLine("house," + create_position.X + "," + create_position.Y + ",1");
                            break;
                        case 28:
                            ParticleSystem ps = new ParticleSystem(true, create_position, 1, 800, 5, 2, 4, Constant.white_particles, new List<Texture2D>() { Constant.footprint_tex });
                            particle_systems.Add(ps);
                            Console.WriteLine($"particle_system,true,{create_position.X},{create_position.Y},1,800,5,2,4,white,footprint");
                            break;
                        default:
                            break;
                    }
                    Console.WriteLine($"Created object idx:{editor_object_idx}");
                    editor_object_idx++;
                } else if (Keyboard.GetState().IsKeyDown(Keys.S) && Keyboard.GetState().IsKeyDown(Keys.LeftControl) && selection_elapsed >= selection_cooldown) { //Ctrl+S
                    //pull all world entities from render list (whether or not they're currently being drawn)
                    List<IEntity> all_world_entities = entities_list.get_all_entities().Keys.ToList();
                    //save
                    save_world_level_to_file(all_world_entities, foreground_entities, background_entities);
                } else if (editor_tool_idx == 1) {
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
                }
            }
            #endregion

            //update particle systems
            update_world_particle_systems(gameTime, camera.Rotation);

            //check collisions
            check_entity_collision(gameTime);

            //clear entities
            clear_entities();

            //check trigger volumes
            check_triggers(gameTime, camera.Rotation);

            //shake camera update
            shake_camera(gameTime);

            //dashing camera shake
            check_player_dash_camera_shake();

            //update camera
            if (Keyboard.GetState().IsKeyDown(Keys.N)) {
                camera.Update(Vector2.Zero);
            } else {
                camera.Update(player.get_camera_track_position());
            }
            
            //update camera bounds
            //update_camera_bounds();
        }
        
        /*PARTICLE SYSTEMS CODE*/
        public void update_world_particle_systems(GameTime gameTime, float rotation) {
            //update and manage world particle systems
            foreach (ParticleSystem ps in particle_systems) {
                ps.Update(gameTime, rotation);
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
        
        #region save_world_file
        public void save_world_level_to_file(List<IEntity> entities,  List<ForegroundEntity> foreground_entities,  List<BackgroundEntity> background_entities) {
            //set up object lists to save to file
            List<GameWorldPlayerAttribute> player_attribute_list = player.to_world_player_attributes_object();
            List<GameWorldObject> world_objs = new List<GameWorldObject>();
            List<GameWorldCondition> conditions = condition_manager.get_world_level_list();
            List<GameWorldParticleSystem> world_particle_systems = new List<GameWorldParticleSystem>();
            //iterate over all entities
            foreach (IEntity e in entities) {
                world_objs.Add(e.to_world_level_object());
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
                    world_objs.Add(tile.to_world_level_object());
                }
            }
            //iterate over floor entities
            foreach (FloorEntity fe in floor_entities) {
                if (fe is Tile) {
                    Tile tile = (Tile)fe;
                    world_objs.Add(tile.to_world_level_object());
                }
            }
            //iterate over level triggers
            foreach (ITrigger t in triggers) {
                world_objs.Add(t.to_world_level_object());
            }

            //handle saving collision geometry that is not drawn
            //(invisible objects)
            foreach (IEntity g in collision_geometry) {
                if (g is InvisibleObject) {
                    InvisibleObject io = (InvisibleObject)g;
                    world_objs.Add(io.to_world_level_object());
                }
            }

            //handle saving particle systems
            foreach(ParticleSystem ps in particle_systems) {
                world_particle_systems.Add(ps.to_world_level_particle_system());
            }

            //handle sorting conditions
            List<GameWorldCondition> sorted_world_conditions = conditions.OrderBy(c => c.object_id_num).ToList();
            //sort world objects by numerical id
            List<GameWorldObject> sorted_world_objs = world_objs.OrderBy (o => o.object_id_num).ToList();

            //sanity check and re-issue ids to objects to prevent any gaps in object ids that could result in an issue on level loading
            //trying to collapse gaps in ids mostly because it throws off the level loading process
            //this is mostly because we introduced the concept of object deletion which then leads to gaps in ids
            for (int i = 0; i < sorted_world_objs.Count; i++) {
                sorted_world_objs[i].object_id_num = i;
            }
            int object_id_count = sorted_world_objs.Count;
            for (int i = object_id_count; i < sorted_world_conditions.Count; i++) {
                sorted_world_conditions[i].object_id_num = i;
            }
            object_id_count = sorted_world_objs.Count + sorted_world_conditions.Count;
            //set world particle systems object id (this number is irrelevant to particle systems, but it should still be in order)
            for (int i = object_id_count; i < world_particle_systems.Count; i++) {
                world_particle_systems[i].object_id_num = i;
            }
            
            //generate GameWorldFile object with all saved objects
            GameWorldFile world_file = new GameWorldFile {
                player_attributes = player_attribute_list,
                world_objects = sorted_world_objs,
                conditions = sorted_world_conditions,
                particle_systems = world_particle_systems,
                world_script = new List<string>()
            };
            //take world file and serialize to json then write to file
            string file_contents = JsonSerializer.Serialize(world_file);
            var file_path = Path.Combine(content_root_directory, "levels/" + save_file_name);
            Console.WriteLine("saving file:" + file_path);

            //write file
            File.WriteAllText(file_path, file_contents);
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
                foreach (IEntity e in collision_entities) {
                    //check player hitboxes collision with all entity hurtboxes
                    if ((e is Ghastly || e is Nightmare) && !(e is NPC)) {
                        ICollisionEntity ic = (ICollisionEntity)e;
                        if (ic.is_hurtbox_active()) {
                            bool collision = player.check_hitbox_collisions(ic.get_hurtbox());
                            if (collision) {
                                ic.take_hit(player, 1);
                                //add to arrows charges for player
                                player.add_arrow_charge(1);
                                //freeze frames to add weight to collision
                                freeze_frames = 2;
                                //shake the camera
                                set_camera_shake(Constant.camera_shake_milliseconds, Constant.camera_shake_angle, Constant.camera_shake_hit_radius);
                            }
                        }
                    }
                }
            }

            //check enemy collisions against player
            foreach (IEntity e in collision_entities) {
                //check enemy hitbox collision with player hurtboxes
                if (e is Nightmare) {
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
                }

                //handle projectile collisions
                foreach (IEntity proj in projectiles) {
                    if (!e.Equals(proj)) {
                        if (e is Nightmare) {
                            Nightmare nm = (Nightmare)e;
                            Arrow a = (Arrow)proj;
                            bool collision = nm.get_hurtbox().collision(a.get_hitbox());
                            if (collision) {
                                nm.take_hit(a, 1);
                                //if the shot is not a power shot then clear it on impact immediately
                                //it will be cleared when the speed runs out (dead)
                                if (!a.is_power_shot()) { entities_to_clear.Add(a); }
                            }
                        }
                    }
                }
            }

            //check player interactions with collision_entities
            foreach (IEntity e in collision_entities) {
                if (player.interacting()) {
                    if (e is Sign) {
                        Sign s = (Sign)e;
                        bool collision = player.check_hurtbox_collisions(s.get_interaction_box());
                        if (collision) {
                            //calculate screen textbox position
                            Vector2 textbox_screen_position = Constant.world_position_to_screen_position(s.get_overhead_position(), camera);
                            //set textbox screen position
                            s.get_textbox().set_position(textbox_screen_position);
                            //set sign to display
                            s.display_textbox();
                            //set current textbox to correct instance
                            current_textbox = s.get_textbox();
                            current_sign = s;
                        }
                    } else if (e is NPC) {
                        NPC npc = (NPC)e;
                        bool collision = player.check_hurtbox_collisions(npc.get_interaction_box());
                        if (collision) {
                            //orient npc to target for speaking
                            npc.orient_to_target(player.get_base_position(), camera.Rotation);
                            //calculate screen textbox position from overhead position of npc
                            Vector2 screen_position = Vector2.Transform(npc.get_overhead_position(), camera.Transform);
                            npc.get_textbox().set_position(screen_position);
                            //set npc to display
                            npc.display_textbox();
                            //set current textbox to correct instance
                            current_textbox = npc.get_textbox();
                            current_npc = npc;
                        }
                    }
                }

                if (e is Sign) { //check if interaction is available and display prompt for signs
                    Sign s = (Sign)e;
                    bool collision = player.check_hurtbox_collisions(s.get_interaction_box());
                    //set available interaction display
                    s.set_display_interaction(collision);
                } else if (e is NPC) {
                    NPC npc = (NPC)e;
                    bool collision = player.check_hurtbox_collisions(npc.get_interaction_box());
                    //set available interaction display
                    npc.set_display_interaction(collision);
                } else if (e is StackedObject) {
                    //interactions with picking up weapons
                    StackedObject stacked_object = (StackedObject)e;
                    bool collision = player.check_hurtbox_collisions(stacked_object.get_hurtbox());
                    if (collision && player.interacting()) {
                        if ((e.get_id().Equals("sword") || e.get_id().Equals("bow") || e.get_id().Equals("dash")) && player.get_attribute_active(e.get_id()) == false) {
                            player.set_attribute(e.get_id(), true);
                            clear_entity(e);
                        }
                    }
                }
            }

            //check player-geometry collisions
            foreach (IEntity e in collision_geometry) {
                if (e is StackedObject) {
                    StackedObject obj = (StackedObject)e;
                    bool collision = obj.check_hitbox_collisions(player.get_future_hurtbox());
                    collision_geometry_map[e] = collision;
                    // if (collision) {
                    //     player.resolve_collision_geometry_movement(player.get_direction(), obj);
                    // }

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
                            }
                            //add arrow charge because we hit something
                            player.add_arrow_charge(1);
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
                            set_transition(true, current_level_id);
                        }
                    }
                }
            }
            player.set_collision_geometry_map(collision_geometry_map);

            //check projectile collision against geometry
            //TODO: why isn't collision geometry a collection of icollisionentity?
            foreach (IEntity e in collision_geometry) {
                foreach (IEntity proj in projectiles) {
                    //make sure we are not checking the same entity
                    if (!e.Equals(proj)) {
                        if (e is StackedObject) {
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
                        }
                    }
                }
            }
        }

        private void check_triggers(GameTime gameTime, float rotation) {
            bool triggered = false;
            ITrigger trigger = null;

            foreach (ITrigger t in triggers) {
                t.Update(gameTime, rotation);

                //if trigger volume is triggered, set trigger to check, break and then handle trigger set
                if (t.is_triggered()) {
                    triggered = true;
                    trigger = t;
                    break;
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
                        //transition to next level
                        set_transition(true, level_id);
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
                }
                trigger = null;
            }
        }
        #endregion

        private void add_floor_entity(FloorEntity e) {
            //add entity to list
            floor_entities.Add(e);
            //sort by weight
            floor_entities = floor_entities.OrderByDescending(x => x.get_draw_weight()).ToList();
        }
        
        #region level_transitions
        //function to set up transition
        //NOTE: make sure that you check for !transition_active before invoking the function,
        //otherwise the update() will run again and the transition will continually trigger without ever completing, thereby just freezing the game (softlock)
        public void set_transition(bool value, string level_id) {
            //set transition to active and set elapsed and next level variables
            transition_active = value;
            next_level_id = level_id;
            transition_elapsed = 0f;
            player.set_movement_disabled(true);
        }

        public void level_transition(GameTime gameTime, string n_level_id) {
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
                    load_level(content_root_directory, _graphics, n_level_id);
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
                //if this is an ai, remove it from enemies
                if (e is IAiEntity) {
                    IAiEntity ai = (IAiEntity)e;
                    enemies.Remove(ai);
                }
                //if this is a projectile, remove it from projectiles
                if (e is Arrow && projectiles.Contains(e)) {
                    projectiles.Remove(e);
                }
                entities_list.Delete_Hard(e);
            }
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
                ICollisionEntity ce = (ICollisionEntity)e;
                if (ce.get_hurtbox().collision(r)) {
                    return e;
                }
            }
            foreach (IEntity e in collision_geometry) {
                //cast to collision entity
                ICollisionEntity ce = (ICollisionEntity)e;
                if (ce.get_hurtbox().collision(r)) {
                    return e;
                }
            }
            foreach (IEntity e in plants) {
                ICollisionEntity ce = (ICollisionEntity)e;
                if (ce.get_hurtbox().collision(r)) {
                    return e;
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

        public void Draw(SpriteBatch _spriteBatch){
            // TODO: Add drawing code here

            _spriteBatch.Begin(SpriteSortMode.Deferred,
                                BlendState.AlphaBlend,
                                SamplerState.PointClamp, null, null, null,
                                camera.Transform);
            //draw floor itself
            //note: this draws entities in order, meaning our sorting after 
            //insertion should yield the proper back to front ordering that needs to be drawn
            for (int i = 0; i < floor_entities.Count; i++) {
                if (Vector2.Distance(floor_entities[i].get_base_position(), player.get_base_position()) < render_distance) {
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
            entities_list.Draw(_spriteBatch, player.get_base_position(), render_distance);

            /*PARTICLE SYSTEMS*/
            foreach (ParticleSystem ps in particle_systems) {
                ps.Draw(_spriteBatch);
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
                    _spriteBatch.DrawString(Constant.arial_small, $"place", mouse_hitbox.position, Color.Black);
                } else if (editor_tool_idx == 1) {
                    _spriteBatch.DrawString(Constant.arial_small, $"cond.", mouse_hitbox.position, Color.Black);
                } else if (editor_tool_idx == 2) {
                    _spriteBatch.DrawString(Constant.arial_small, $"delete", mouse_hitbox.position, Color.Black);
                }
                
                Renderer.FillRectangle(_spriteBatch, create_position, 10, 10, Color.Purple);
                mouse_hitbox.draw(_spriteBatch);

                condition_manager.Draw(_spriteBatch);
            }
            _spriteBatch.End();

            //draw textboxes without camera matrix (screen positioning)
            //however, this is still drawing to the render target under point filtering so it will come out stylized
            _spriteBatch.Begin();
            //draw textboxes on screen
            if ((current_sign != null || current_npc != null) && current_textbox != null) {
                current_textbox.Draw(_spriteBatch);
            }
            _spriteBatch.End();

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
        }

        public void DrawTextOverlays(SpriteBatch _spriteBatch) {
            //draw UI
            _spriteBatch.Begin();
            //display dash charges
            for (int i = 0; i < player.get_dash_charge(); i++) {
                Renderer.FillRectangle(_spriteBatch, Constant.dash_charge_ui_screen_position + new Vector2(i*Constant.dash_charge_ui_size + 10f, 0), Constant.dash_charge_ui_size - 5, Constant.dash_charge_ui_size - 5, Color.Black);
            }
            //display attack charges
            for (int i = 0; i < player.get_attack_charge(); i++) {
                Renderer.FillRectangle(_spriteBatch, Constant.dash_charge_ui_screen_position + new Vector2(i*Constant.dash_charge_ui_size + 10f, Constant.dash_charge_ui_size + 5), Constant.dash_charge_ui_size - 5, Constant.dash_charge_ui_size - 5, Color.White);
            }
            //display health
            for (int i = 0; i < player.get_health(); i++) {
                Renderer.FillRectangle(_spriteBatch, Constant.dash_charge_ui_screen_position + new Vector2(i*Constant.dash_charge_ui_size + 10f, Constant.dash_charge_ui_size*2 + 5), Constant.dash_charge_ui_size - 5, Constant.dash_charge_ui_size - 5, Color.Red);
            }
            //display arrow charges
            for (int i = 0; i < player.get_arrow_charges(); i++) {
                Renderer.FillRectangle(_spriteBatch, Constant.dash_charge_ui_screen_position + new Vector2(i*Constant.dash_charge_ui_size + 10f, Constant.dash_charge_ui_size*3 + 5), Constant.dash_charge_ui_size - 5, Constant.dash_charge_ui_size - 5, Color.Blue);
            }

            //draw text overlays
            _spriteBatch.DrawString(Constant.arial_small, "fps:" + fps, new Vector2(0, 17), Color.Black);
            _spriteBatch.DrawString(Constant.arial_small, "camera_rotation: " + camera.Rotation, new Vector2(0, 17*2), Color.Black);
            _spriteBatch.DrawString(Constant.arial_small, "camera_zoom:" + camera.Zoom, new Vector2(0, 17*3), Color.Black);
            _spriteBatch.DrawString(Constant.arial_small, "editor:" + editor_active, new Vector2(0, 17*4), Color.Black);
            //_spriteBatch.DrawString(Constant.arial_small, "selected_object:" + selected_object, new Vector2(0, 17*5), Color.Black);
            _spriteBatch.DrawString(Constant.arial_small, $"selected_object:{selected_object},obj_id:{obj_map[selected_object].get_id()}", new Vector2(0, 17*5), Color.Black);
            _spriteBatch.DrawString(Constant.arial_small, $"editor_tool:{editor_tool_idx}", new Vector2(0, 17*6), Color.Black);
            _spriteBatch.DrawString(Constant.arial_small, $"editor_layer:{editor_layer}", new Vector2(0, 17*7), Color.Black);
            _spriteBatch.DrawString(Constant.arial_small, $"editor_selected_object_rotation:{editor_object_rotation}", new Vector2(0, 17*8), Color.Black);
            _spriteBatch.End();
        }
    }
}