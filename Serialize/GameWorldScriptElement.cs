using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace gate.Serialize
{
    public class GameWorldScriptElement 
    {
        public string trigger { get; set; }
        public string action { get; set; }
        public GameWorldScriptElementParam parameters { get; set; }
        public int level_load_count_required { get; set; }
    }
}