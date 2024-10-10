using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using gate.Interface;
using gate.Core;

namespace gate.Sounds
{
    public class SoundManager
    {
        private float max_volume_distance = 350f;
        private List<string> loaded_sfx;
        private Dictionary<SoundEffectInstance, Vector2> sfx_instance_positions;
        private List<SoundEffectInstance> dead_sfx_instances;

        private World world;
        private ContentManager Content;

        public SoundManager(World world, ContentManager Content) {
            //set for world access
            this.world = world;
            //pull content for loading sound effects
            this.Content = Content;

            //initialize loaded sfx
            this.loaded_sfx = new List<string>();
            //initialize dictionary for sfx instance positions so we can track sound positions
            this.sfx_instance_positions = new Dictionary<SoundEffectInstance, Vector2>();
            this.dead_sfx_instances = new List<SoundEffectInstance>();
        }

        public void load_sfx(ref SoundEffect sfx, string path) {
            check_and_load_sfx(ref sfx, path);
        }

        private void check_and_load_sfx(ref SoundEffect sfx, string sfx_path) {
            if (!loaded_sfx.Contains(sfx_path)) {
                //add to loaded sfx
                loaded_sfx.Add(sfx_path);
                sfx = this.Content.Load<SoundEffect>(sfx_path);
                Console.WriteLine($"Loaded sfx from path: {sfx_path}");
            }
        }

        public List<string> get_loaded_sfx() {
            return loaded_sfx;
        }

        public void play_spatial_sfx(SoundEffect sfx, Vector2 sound_position, Vector2 player_position, float pitch, float player_hearing_distance, float volume_offset = 0f) {
            //sound should already be loaded at this point so we should just play it where it needs to be played
            //create instance
            SoundEffectInstance sfx_instance = sfx.CreateInstance();

            //calculate vector between sound position and player
            Vector2 direction = player_position - sound_position;

            //adjust pan based on position
            float pan = MathHelper.Clamp(-direction.X / player_hearing_distance, -1f, 1f);
            sfx_instance.Pan = pan;
            //adjust volume based on distance (further away, the quieter the sound should be played)
            float volume = MathHelper.Clamp(1f - (direction.Length() / max_volume_distance), 0f, 1f);
            float final_volume = MathHelper.Clamp(volume + volume_offset, 0f, 1f);
            sfx_instance.Volume = final_volume;

            //set pitch (clamped)
            sfx_instance.Pitch = MathHelper.Clamp(pitch, -1f, 1f);

            //add to dictionary for tracking played sounds
            if (!sfx_instance_positions.ContainsKey(sfx_instance)) {
                sfx_instance_positions.Add(sfx_instance, sound_position);
            }
            
            //play sound effect with pan and volume
            sfx_instance.Play();
        }

        public void Update(GameTime gameTime, float rotation) {
            update_sfx_instance_positions();
        }

        private void update_sfx_instance_positions() {
            foreach (KeyValuePair<SoundEffectInstance, Vector2> kv in sfx_instance_positions) {
                SoundEffectInstance sfx = kv.Key;
                if (sfx.State == SoundState.Stopped) {
                    dead_sfx_instances.Add(sfx);
                }
            }
            //remove dead sfx instances from dictionary and clear dead_sfx_instances
            foreach (SoundEffectInstance sfx in dead_sfx_instances) {
                sfx_instance_positions.Remove(sfx);
            }
            dead_sfx_instances.Clear();
        }

        public void Draw(SpriteBatch spriteBatch) {
            foreach (KeyValuePair<SoundEffectInstance, Vector2> kv in sfx_instance_positions) {
                Renderer.FillRectangle(spriteBatch, kv.Value, 10, 10, Color.Gray);
            }
        }
    }
}