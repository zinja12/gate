using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace gate.Serialize
{
    public class GameWorldDialogueFile
    {
        public string character_name { get; set; }
        public List<GameWorldDialogue> dialogue { get; set; }
    }
}