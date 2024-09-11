using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace gate
{
    public class GameWorldParticleSystem
    {
        //game world object fields
        public bool in_world { get; set; }
        public int object_id_num { get; set; }
        public float x_position { get; set; }
        public float y_position { get; set; }
        public int max_speed {get; set; }
        public float life_duration { get; set; }
        public float frequency { get; set; }
        public int min_scale { get; set; }
        public int max_scale { get; set; }
        public string particle_colors { get; set; }
        public string particle_textures { get; set; }
    }
}