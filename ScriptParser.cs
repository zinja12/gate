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

        private List<GameWorldScriptElement> on_load_scripts;
        private List<GameWorldScriptElement> on_unload_scripts;
        
        private int current_command_idx;
        private int on_load_processed;
        private int on_unload_processed;

        private float action_timer;

        //command specific variables
        private bool camera_move1 = false;
        private float camera_pause_timer, camera_pause_threshold;

        public ScriptParser(World world, List<GameWorldScriptElement> game_world_script) {
            this.world = world;
            this.game_world_script = game_world_script;

            this.on_load_scripts = new List<GameWorldScriptElement>();
            this.on_unload_scripts = new List<GameWorldScriptElement>();

            this.current_command_idx = -1;
            this.on_load_processed = 0;
            this.on_unload_processed = 0;

            this.action_timer = 0f;
        }

        public List<GameWorldScriptElement> get_game_world_script() {
            return game_world_script;
        }

        public List<GameWorldScriptElement> get_on_load_scripts() {
            return on_load_scripts;
        }

        public bool finished_execution() {
            return current_command_idx == -1;
        }

        public List<GameWorldScriptElement> get_on_unload_scripts() {
            return on_unload_scripts;
        }

        public bool parse_script(List<GameWorldScriptElement> world_script, out int parse_count) {
            parse_count = 0;
            //iterate through script commands
            foreach (GameWorldScriptElement script_element in world_script) {
                Console.WriteLine($"script_element trigger:{script_element.trigger}");
                switch (script_element.trigger) {
                    case "ON_LOAD":
                        on_load_scripts.Add(script_element);
                        parse_count++;
                        break;
                    case "ON_UNLOAD":
                        on_unload_scripts.Add(script_element);
                        parse_count++;
                        break;
                    default:
                        //nothing
                        break;
                }
            }
            Console.WriteLine($"World Script: parsed {on_load_scripts.Count} on_load script command(s), {on_unload_scripts.Count} on_unload script command(s)");
            return true;
        }

        public void execute_on_load_script(GameTime gameTime, List<GameWorldScriptElement> script) {
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
                switch (command.action) {
                    case "camera_move":
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
                                //camera has reached player
                                //re-tether the camera to the player
                                world.player_camera_tethered = true;
                                //set camera pause timer back to 0
                                camera_pause_timer = 0f;
                                action_timer = 0f;
                                //set camera_move1 back to false for potential next camera move
                                camera_move1 = false;
                                //increase command idx as we have finished this command
                                safe_increase_command_idx(script);
                            }
                        }
                        break;
                    case "player_movement":
                        Console.WriteLine($"setting player movement: {command.parameters.disable}");
                        //set player movement based on what the command specifies
                        world.get_player().set_movement_disabled(command.parameters.disable);
                        //increase command idx as we have finished this command
                        safe_increase_command_idx(script);
                        break;
                    default:
                        Console.WriteLine($"command action:{command.action} not supported. ignoring...");
                        break;
                }
            }
        }

        private void safe_increase_command_idx(List<GameWorldScriptElement> script) {
            if (current_command_idx == script.Count - 1) {
                current_command_idx = -1;
            } else {
                current_command_idx++;
            }
        }

        //returns true when executing, false when finished
        public bool execute_on_unload_script(List<GameWorldScriptElement> script) {
            return false;
        }

        public int get_current_command_idx() {
            return current_command_idx;
        }
    }
}