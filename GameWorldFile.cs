using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace gate
{
    public class GameWorldFile
    {
        //game world object fields
        public GameWorldObject camera_bounds { get; set; }
        public List<GameWorldPlayerAttribute> player_attributes { get; set; }
        public List<GameWorldObject> world_objects { get; set; }
        public List<GameWorldCondition> conditions { get; set; }
        public List<GameWorldParticleSystem> particle_systems { get; set; }
        public List<string> world_script { get; set; }
    }
}