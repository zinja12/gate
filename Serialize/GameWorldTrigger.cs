using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace gate.Serialize
{
    public class GameWorldTrigger
    {
        //trigger object fields
        public string object_identifier { get; set; }
        public int object_id_num { get; set; }
        public float x_position { get; set; }
        public float y_position { get; set; }
        public float scale {get; set; }
        public float width { get; set; }
        public float height { get; set; }
        public string level_id { get; set; }
        public float entry_x_position { get; set; }
        public float entry_y_position { get; set; }
        public float rotation { get; set; }
        public float goal_rotation { get; set; }
        public bool previously_activated { get; set; }
        public bool retrigger { get; set; }
    }
}