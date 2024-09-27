using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace gate
{
    public class GameWorldCondition
    {
        //game world condition fields
        public string object_identifier { get; set; }
        public int object_id_num { get; set; }
        public List<int> enemy_ids { get; set; }
        public List<int> obj_ids_to_remove { get; set; }
        public float x_position { get; set; }
        public float y_position { get; set; }
    }
}