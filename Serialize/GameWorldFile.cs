using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace gate.Serialize
{
    public class GameWorldFile
    {
        //game world object fields
        public GameWorldObject camera_bounds { get; set; }
        public int level_loaded_count { get; set; }
        public string checkpoint_level_id { get; set; }
        public List<GameWorldObject> world_objects { get; set; }
        public List<GameWorldTrigger> world_triggers { get; set; }
        public List<GameWorldCondition> conditions { get; set; }
        public List<GameWorldParticleSystem> particle_systems { get; set; }
        public GameWorldObject shade { get; set; }
        public List<GameWorldScriptElement> world_script { get; set; }
    }
}