using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace gate.Serialize
{
    public class GameWorldPlayerAttributeFile
    {
        //player attribute fields
        public List<GameWorldPlayerAttribute> player_attributes { get; set; }
        //player money
        public int player_money { get; set; }
    }
}