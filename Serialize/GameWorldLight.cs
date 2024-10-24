using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace gate.Serialize
{
    public class GameWorldLight
    {
        //game world object fields
        public float light_center_x { get; set; }
        public float light_center_y { get; set; }
        public float radius { get; set; }
    }
}