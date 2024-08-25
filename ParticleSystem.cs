using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace gate
{

    public class ParticleSystem
    {
        public Vector2 base_position;

        public List<Particle> particles;
        public List<Particle> dead_particles;

        private Random random;
        private int max_speed;

        private float particle_life_duration;
        private bool constant_emission;
        private int total_particle_count, current_particle_count;
        private List<Color> particle_colors;
        private List<Texture2D> particle_textures;

        private int particle_min_scale, particle_max_scale;
        
        //constant emission constructor
        public ParticleSystem(Vector2 base_position, int max_speed, float particle_life_duration, int particle_min_scale, int particle_max_scale, List<Color> particle_colors, List<Texture2D> particle_textures) {
            this.base_position = base_position;
            this.max_speed = max_speed;
            this.random = new Random();

            this.particles = new List<Particle>();
            this.dead_particles = new List<Particle>();

            this.particle_life_duration = particle_life_duration;
            this.constant_emission = true;
            this.particle_min_scale = particle_min_scale;
            this.particle_max_scale = particle_max_scale;
            this.particle_colors = particle_colors;
            this.particle_textures = particle_textures;

            if (particle_textures.Count() == 0) {
                throw new Exception("Particle System requires particle textures. No particle textures provided in particle_textures list parameter. particle_textures parameter is empty.");
            }
        }
        
        //puff / explosion constructor
        public ParticleSystem(Vector2 base_position, int max_speed, float particle_life_duration, int total_particle_count, int particle_min_scale, int particle_max_scale, List<Color> particle_colors, List<Texture2D> particle_textures) 
            : this(base_position, max_speed, particle_life_duration, particle_min_scale, particle_max_scale, particle_colors, particle_textures) {
            this.constant_emission = false;
            this.total_particle_count = total_particle_count;
            this.current_particle_count = 0;
        }

        public void Update(GameTime gameTime, float rotation) {
            //generate new particle
            float speed = (float)random.Next(1, max_speed);
            speed /= 3;
            //TODO: need to continually rotate direction based on rotation of the world/camera
            Vector2 direction = new Vector2((float)random.Next(-100, 100), (float)random.Next(-100, 0));
            direction = Vector2.Normalize(direction);
            direction = Constant.rotate_point(direction, rotation, 1f, Constant.direction_up);
            //only add particles if we are constantly emitting particles
            if (constant_emission) {
                //generate new particle
                particles.Add(new Particle(base_position, direction, speed, true, particle_life_duration, get_random_texture(), (float)random.Next(particle_min_scale, particle_max_scale), particle_colors));
            } else {
                //generate particle up to certain amount specified
                if (current_particle_count < total_particle_count) {
                    particles.Add(new Particle(base_position, direction, speed, true, particle_life_duration, get_random_texture(), (float)random.Next(particle_min_scale, particle_max_scale), particle_colors));
                    current_particle_count++;
                }
            }

            //update particles and remove dead particles
            foreach (Particle p in particles) {
                p.update(gameTime, rotation);
                //check for dead particles
                if (p.is_dead()) {
                    dead_particles.Add(p);
                }
            }
            foreach (Particle p in dead_particles) {
                particles.Remove(p);
                Particle x = p;
                x = null;
            }
            dead_particles.Clear();
        }

        public void set_position(Vector2 position) {
            this.base_position = position;
        }

        public void add_particle_texture(Texture2D tex) {
            particle_textures.Add(tex);
        }

        public Texture2D get_random_texture() {
            //do not need to generate a random number if the list is just 1 texture
            if (particle_textures.Count() == 1){
                return particle_textures[0];
            }

            //index into list randomly to pull next texture
            return particle_textures[random.Next(0, particle_textures.Count())];
        }

        public bool is_finished() {
            if (constant_emission) {
                return false;
            } else {
                if (particles.Count == 0 && dead_particles.Count == 0) {
                    return true;
                }
            }
            return false;
        }

        public void Draw(SpriteBatch spriteBatch) {
            foreach (Particle p in particles) {
                p.draw(spriteBatch);
            }
        }
    }
}