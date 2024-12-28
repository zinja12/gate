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

namespace gate
{
    public class ScriptParser
    {
        private World world;
        private List<GameWorldScriptElement> game_world_script;

        private int level_loaded_count = 0;

        private List<GameWorldScriptElement> on_load_scripts;
        private List<GameWorldScriptElement> on_unload_scripts;
        private List<GameWorldScriptElement> on_gameplay_scripts;
        
        private int current_command_idx;
        private int on_load_processed;

        private float action_timer;

        //command specific variables
        private bool camera_move1 = false;
        private float camera_pause_timer, camera_pause_threshold;

        public ScriptParser(World world, List<GameWorldScriptElement> game_world_script, int level_loaded_count) {
            this.world = world;
            this.game_world_script = game_world_script;
            
            this.level_loaded_count = level_loaded_count;

            this.on_load_scripts = new List<GameWorldScriptElement>();
            this.on_unload_scripts = new List<GameWorldScriptElement>();
            this.on_gameplay_scripts = new List<GameWorldScriptElement>();

            this.current_command_idx = -1;
            this.on_load_processed = 0;

            this.action_timer = 0f;
        }

        public List<GameWorldScriptElement> get_game_world_script() {
            return game_world_script;
        }

        public List<GameWorldScriptElement> get_on_load_script() {
            return on_load_scripts;
        }

        public List<GameWorldScriptElement> get_on_unload_script() {
            return on_unload_scripts;
        }

        public List<GameWorldScriptElement> get_on_gameplay_script() {
            return on_gameplay_scripts;
        }

        public List<GameWorldScriptElement> get_on_gameplay_script(int trigger_id) {
            //filter gameplay scripts to specific trigger so we are able to run partial scripts
            return on_gameplay_scripts.Where(command => command.parameters.trigger_id == trigger_id).ToList();
        }

        public bool finished_execution() {
            return current_command_idx == -1;
        }

        public bool parse_script(List<GameWorldScriptElement> world_script, out int parse_count) {
            parse_count = 0;
            //iterate through script commands
            foreach (GameWorldScriptElement script_element in world_script) {
                switch (script_element.trigger) {
                    case "ON_LOAD":
                        on_load_scripts.Add(script_element);
                        parse_count++;
                        break;
                    case "ON_UNLOAD":
                        on_unload_scripts.Add(script_element);
                        parse_count++;
                        break;
                    case "ON_TRIGGER":
                        on_gameplay_scripts.Add(script_element);
                        parse_count++;
                        break;
                    default:
                        //nothing
                        break;
                }
            }
            Console.WriteLine($"world script: parsed {on_load_scripts.Count} on_load script command(s), {on_unload_scripts.Count} on_unload script command(s), {on_gameplay_scripts.Count} on_gameplay script command(s)");
            return true;
        }
        
        //separate script actions into function to call on load, unload and gameplay
        public void execute_script_actions(GameTime gameTime, List<GameWorldScriptElement> script, GameWorldScriptElement command) {
            switch (command.action) {
                case "camera_move":
                    //call camera move function
                    camera_move_command(gameTime, command, script);
                    break;
                case "player_movement":
                    Console.WriteLine($"setting player movement disabled: {command.parameters.disable}");
                    //set player movement based on what the command specifies
                    world.get_player().set_movement_disabled(command.parameters.disable);
                    //increase command idx as we have finished this command
                    safe_increase_command_idx(script);
                    break;
                case "ai_behavior":
                    Console.WriteLine($"setting ai behavior disabled: {command.parameters.disable}");
                    world.set_ai_behavior_disabled(command.parameters.disable);
                    //increase command idx as we have finished this command
                    safe_increase_command_idx(script);
                    break;
                case "spawn_nightmare":
                    Console.WriteLine($"spawning nightmare at position: ({command.parameters.x_position},{command.parameters.y_position})");
                    //spawn object using place object function
                    world.place_object(
                        gameTime, //gameTime
                        0f, //current rotation
                        15, //editor object id corresponding to nightmare entity
                        new Vector2(command.parameters.x_position, command.parameters.y_position), //position
                        0f, //editor rotation
                        (int)ObjectPlacement.Script //placement source
                    );
                    //increase command idx as we have finished this command
                    safe_increase_command_idx(script);
                    break;
                case "spawn_ghost":
                    Console.WriteLine($"spawning ghost at position: ({command.parameters.x_position},{command.parameters.y_position})");
                    //spawn object using place object function
                    world.place_object(
                        gameTime, //gameTime
                        0f, //current rotation
                        30, //editor object id corresponding to ghost entity
                        new Vector2(command.parameters.x_position, command.parameters.y_position), //position
                        0f, //editor rotation
                        (int)ObjectPlacement.Script //placement source
                    );
                    //increase command idx as we have finished this command
                    safe_increase_command_idx(script);
                    break;
                case "intro_text":
                    //call intro text function
                    if (world.run_intro_text) {
                        intro_text_command(gameTime, command, script);
                    } else {
                        safe_increase_command_idx(script);
                    }
                    break;
                default:
                    Console.WriteLine($"command action:{command.action} not supported. ignoring and continuing...");
                    //increase command idx as we have finished this command
                    safe_increase_command_idx(script);
                    break;
            }
        }

        public void execute_on_load_script(GameTime gameTime, int level_loaded_count, List<GameWorldScriptElement> script) {
            //current command is not set
            if (current_command_idx == -1) {
                //set the current command
                if (on_load_processed < script.Count) {
                    //set current command equal to the next processed
                    current_command_idx = on_load_processed;
                }
            } else {
                /*process current command*/

                //continue adding to action timer
                action_timer += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
                //pull current command and execute based on timer
                GameWorldScriptElement command = script[current_command_idx];
                //first check whether or not the current commands required level load count matches
                if (command.level_load_count_required < level_loaded_count) {
                    //level load count required does not match current, so we can skip this command and continue
                    safe_increase_command_idx(script);
                }
                
                //execute script commands
                execute_script_actions(gameTime, script, command);
            }
        }

        public void execute_gameplay_trigger_script(GameTime gameTime, ScriptTrigger trigger, List<GameWorldScriptElement> script) {
            //NOTE: for each command that executes make sure to check and deactivate the trigger so it does not keep running
            if (script.Count == 0) {
                Console.WriteLine($"Length of script commands passed in is {script.Count}");
            }

            //check current command set
            if (current_command_idx == -1) {
                //set the current command
                if (on_load_processed < script.Count) {
                    //set current command equal to the next processed
                    current_command_idx = on_load_processed;
                }
            } else {
                /*process current command*/

                //continue adding to action timer
                action_timer += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
                //pull current command and execute based on timer
                GameWorldScriptElement command = script[current_command_idx];
                //check for whether the command entity referenced as already been activated on a previous level load
                //this can be checked by finding the entity/trigger object and then checking for whether or not it has been previously activated
                if (trigger.get_previously_activated() && !trigger.get_retrigger()) {
                    //skip this command since it has been previously activated and we do not want to retrigger it
                    safe_increase_command_idx(script);
                } else {
                    //execute command idx
                    execute_script_actions(gameTime, script, command);
                }
            }
        }

        private void safe_increase_command_idx(List<GameWorldScriptElement> script) {
            if (current_command_idx == script.Count - 1) {
                current_command_idx = -1;
                Console.WriteLine("end of script commands run");
            } else {
                current_command_idx++;
            }
            //set action timer to 0
            action_timer = 0f;
        }

        //returns true when executing, false when finished
        public bool execute_on_unload_script(List<GameWorldScriptElement> script) {
            return false;
        }

        public int get_current_command_idx() {
            return current_command_idx;
        }

        public void intro_text_command(GameTime gameTime, GameWorldScriptElement command, List<GameWorldScriptElement> script) {
            //turn player movement off
            world.get_player().set_movement_disabled(true);
            //set intro text playing
            world.intro_text_playing = true;
            world.intro_text_opacity = 1f;
            world.intro_text_threshold = 12000f;
            world.intro_text_finished_threshold = 6000f;
            world.intro_text = command.parameters.text_items;
            
            if (world.intro_text_elapsed >= world.intro_text_threshold) {
                world.intro_text_finish_elapsed += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
            }

            if (world.intro_text_finish_elapsed >= world.intro_text_finished_threshold) {
                //finish
                world.intro_text_elapsed = 0f;
                world.intro_text_finish_elapsed = 0f;
                world.intro_text_playing = false;
                world.get_player().set_movement_disabled(false);
                safe_increase_command_idx(script);
            }
        }

        public void camera_move_command(GameTime gameTime, GameWorldScriptElement command, List<GameWorldScriptElement> script) {
            world.get_player().set_movement_disabled(true);
            //set camera_move_timer threshold
            camera_pause_threshold = command.parameters.pause_time;
            //move camera
            world.player_camera_tethered = false;
            //update position of the camera
            Vector2 camera_center_position = new Vector2(world.get_camera().X, world.get_camera().Y);
            Vector2 goal_position = new Vector2(command.parameters.x_position, command.parameters.y_position);
            float distance_diff = Vector2.Distance(goal_position, camera_center_position);
            //move towards goal position if camera_move1 has not been marked as completed
            if (distance_diff > 5f && !camera_move1) {
                //lerp camera position to desired point (goal position)
                world.get_camera().Update(
                    Vector2.Lerp(
                        camera_center_position,
                        goal_position,
                        0.0005f * (float)gameTime.ElapsedGameTime.TotalMilliseconds
                    )
                );
            } else if (distance_diff <= 5f && !camera_move1) {
                //mark camera_move1 complete
                camera_move1 = true;
            } else if (camera_move1 && camera_pause_timer < camera_pause_threshold) { //camera_move1 is complete
                //wait with timer
                camera_pause_timer += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
            } else if (camera_pause_timer >= camera_pause_threshold && camera_move1) {
                //we have moved, waited and paused
                //now move back
                //lerp camera position to player position
                world.get_camera().Update(
                    Vector2.Lerp(
                        camera_center_position,
                        world.get_player().get_camera_track_position(),
                        0.0005f * (float)gameTime.ElapsedGameTime.TotalMilliseconds
                    )
                );
                //check distance to finish and clean up variables
                //leave things as we found them
                if (Vector2.Distance(camera_center_position, world.get_player().get_camera_track_position()) < 5f) {
                    //ACTION DONE
                    //camera has reached player
                    //re-tether the camera to the player
                    world.player_camera_tethered = true;
                    //set camera pause timer back to 0
                    camera_pause_timer = 0f;
                    //set camera_move1 back to false for potential next camera move
                    camera_move1 = false;
                    //increase on_load_processed count
                    //on_load_processed++;
                    //increase command idx as we have finished this command
                    safe_increase_command_idx(script);
                }
            }
        }
    }
}