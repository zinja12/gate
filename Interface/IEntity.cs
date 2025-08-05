using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using gate.Serialize;
using gate.Core;

namespace gate.Interface
{
    public interface IEntity : IGameObject
    {
        Vector2 get_base_position();
        float get_depth(float rotation) { return Constant.get_object_depth(rotation, get_base_position()); }
        float get_scale();
        void set_scale(float scale_value);
        string get_flag();
        string get_id();
        void set_base_position(Vector2 base_position);
        float get_rotation_offset();
        void set_rotation_offset(float rotation_offset_degrees);
        GameWorldObject to_world_level_object();
        void Update(GameTime gameTime);
        void update_animation(GameTime gameTime);
        void Draw(SpriteBatch spriteBatch);
        //NOTE: get_obj_ID_num is inherited from one level above in IGameObject
    }
}