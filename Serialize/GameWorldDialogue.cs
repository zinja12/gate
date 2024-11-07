using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace gate.Serialize
{
    public class GameWorldDialogue
    {
        public string speaker { get; set; }
        public string dialogue_line { get; set; }
        public string tag { get; set; }
    }
}