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
        private float frame_mod = 10;
        private int max_speed;

        private float particle_life_duration;
        private bool constant_emission;
        private int total_particle_count, current_particle_count;
        private List<Color> particle_colors;

        private int particle_min_scale, particle_max_scale;
        
        //constant emission constructor
        public ParticleSystem(Vector2 base_position, int max_speed, float particle_life_duration, int particle_min_scale, int particle_max_scale, List<Color> particle_colors) {
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
        }
        
        //puff / explosion constructor
        public ParticleSystem(Vector2 base_position, int max_speed, float particle_life_duration, int total_particle_count, int particle_min_scale, int particle_max_scale, List<Color> particle_colors) {
            this.base_position = base_position;
            this.max_speed = max_speed;
            this.random = new Random();

            this.particles = new List<Particle>();
            this.dead_particles = new List<Particle>();

            this.particle_life_duration = particle_life_duration;
            this.constant_emission = false;

            this.total_particle_count = total_particle_count;
            this.current_particle_count = 0;
            this.particle_min_scale = particle_min_scale;
            this.particle_max_scale = particle_max_scale;
            this.particle_colors = particle_colors;
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
                particles.Add(new Particle(base_position, direction, speed, true, particle_life_duration, Constant.footprint_tex, (float)random.Next(particle_min_scale, particle_max_scale), particle_colors));
            } else {
                if (current_particle_count < total_particle_count) {
                    particles.Add(new Particle(base_position, direction, speed, true, particle_life_duration, Constant.footprint_tex, (float)random.Next(particle_min_scale, particle_max_scale), particle_colors));
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