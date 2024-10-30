using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace gate.Serialize
{
    public class GameWorldScriptElementParam 
    {
        public float x_position { get; set; }
        public float y_position { get; set; }
        public float pause_time { get; set; }
        public bool disable { get; set; }
    }
}