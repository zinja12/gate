using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace gate
{
    public class GameWorldPlayerAttribute
    {
        //game world player attribute fields
        public string identifier { get; set; }
        public bool active { get; set; }
    }
}