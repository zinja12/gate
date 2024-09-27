using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using gate;
using gate.Serialize;
using gate.Core;

namespace gate.Particles
{
    public class ParticleSystem
    {
        public Vector2 base_position, end_position;
        private Vector2? dir;

        public List<Particle> particles;
        public List<Particle> dead_particles;

        private Random random;
        private int max_speed;

        private float particle_life_duration, frequency;
        private bool constant_emission;
        private int total_particle_count, current_particle_count;
        private List<Color> particle_colors;
        private List<Texture2D> particle_textures;

        private int particle_min_scale, particle_max_scale;

        private bool in_world;
        private int frame_count = 0;

        private RRect hurtbox;

        //constant emission constructor
        public ParticleSystem(bool in_world, Vector2 base_position, int max_speed, float particle_life_duration, float frequency, int particle_min_scale, int particle_max_scale, List<Color> particle_colors, List<Texture2D> particle_textures, Vector2? direction = null) {
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

            this.dir = direction;

            if (particle_textures.Count() == 0) {
                throw new Exception("Particle System requires particle textures. No particle textures provided in particle_textures list parameter. particle_textures parameter is empty.");
            }

            this.in_world = in_world;
            this.frequency = frequency;

            this.hurtbox = new RRect(base_position, 10, 10);
        }
        
        //puff / explosion constructor
        public ParticleSystem(bool in_world, Vector2 base_position, int max_speed, float particle_life_duration, int frequency, int total_particle_count, int particle_min_scale, int particle_max_scale, List<Color> particle_colors, List<Texture2D> particle_textures, Vector2? direction = null) 
            : this(in_world, base_position, max_speed, particle_life_duration, frequency, particle_min_scale, particle_max_scale, particle_colors, particle_textures, direction) {
            this.constant_emission = false;
            this.total_particle_count = total_particle_count;
            this.current_particle_count = 0;
        }
        
        //constant emission line constructor
        public ParticleSystem(bool in_world, Vector2 start_position, Vector2 end_position, int max_speed, float particle_life_duration, int frequency, int particle_min_scale, int particle_max_scale, List<Color> particle_colors, List<Texture2D> particle_textures, Vector2? direction = null) 
            : this(in_world, start_position, max_speed, particle_life_duration, frequency, particle_min_scale, particle_max_scale, particle_colors, particle_textures, direction) {
            this.constant_emission = true;
            this.end_position = end_position;
        }

        public void Update(GameTime gameTime, float rotation) {
            //generate new particle
            float speed = (float)random.Next(1, max_speed);
            speed /= 3;
            //TODO: need to continually rotate direction based on rotation of the world/camera
            Vector2 direction = dir.HasValue ? dir.Value : new Vector2((float)random.Next(-100, 100), (float)random.Next(-100, 0));
            //normalize and rotate
            direction = Vector2.Normalize(direction);
            //rotate the direction vector as needed depending on whether this is an in world effect vs a screen effect
            if (in_world) {
                direction = Constant.rotate_point(direction, rotation, 1f, Constant.direction_up);
            }
            //only add particles if we are constantly emitting particles
            if (constant_emission) {
                //line emission
                if (dir.HasValue && frame_count % frequency == 0) {
                    //pick random point on the line between start and end point
                    //emit from that point
                    particles.Add(new Particle(random_point_on_line(base_position, end_position), direction, speed, true, particle_life_duration, get_random_texture(), (float)random.Next(particle_min_scale, particle_max_scale), particle_colors));
                    frame_count = 0;
                } else {
                    //generate new particle from single point
                    if (frame_count % frequency == 0) {
                        particles.Add(new Particle(base_position, direction, speed, true, particle_life_duration, get_random_texture(), (float)random.Next(particle_min_scale, particle_max_scale), particle_colors));
                        frame_count = 0;
                    }
                }
                frame_count++;
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

            //update hurtbox
            hurtbox.update(rotation, base_position);
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

        public Vector2 random_point_on_line(Vector2 start, Vector2 end) {
            //calculate the vector from one to the other
            Vector2 line = end - start;
            //normalize
            line.Normalize();
            //generate random magnitude between 1 and the distance between the two
            float distance = Vector2.Distance(start, end);
            int magnitude = random.Next(1, (int)distance);
            //return line * random magnitude to give a random point on the line
            return line * magnitude;
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

        public RRect get_hurtbox() {
            return hurtbox;
        }

        public GameWorldParticleSystem to_world_level_particle_system() {
            return new GameWorldParticleSystem {
                in_world = this.in_world,
                object_id_num = -1,
                x_position = base_position.X,
                y_position = base_position.Y,
                max_speed = this.max_speed,
                life_duration = particle_life_duration,
                frequency = this.frequency,
                min_scale = particle_min_scale,
                max_scale = particle_max_scale,
                particle_colors = "white",
                particle_textures = "footprint"
            };
        }

        public void Draw(SpriteBatch spriteBatch) {
            foreach (Particle p in particles) {
                p.draw(spriteBatch);
            }
        }
    }
}