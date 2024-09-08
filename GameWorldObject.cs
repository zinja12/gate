using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace gate
{
    public class GameWorldObject
    {
        //game world object fields
        public string object_identifier { get; set; }
        public int object_id_num { get; set; }
        public float x_position { get; set; }
        public float y_position { get; set; }
        public float scale {get; set; }
        public List<string> sign_messages { get; set; }
        public bool canopy { get; set; }
        public float width { get; set; }
        public float height { get; set; }
        public string level_id { get; set; }
        public float rotation { get; set; }
        public float goal_rotation { get; set; }
        public List<int> npc_path_entity_ids { get; set; }
        public string npc_conversation_file_id { get; set; }
    }
}