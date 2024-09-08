using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace gate
{
    public enum TriggerType {
        Level = 1,
        Rotation = 2
    }

    public interface ITrigger {
        RRect get_trigger_collision_box();
        bool is_triggered();
        TriggerType get_trigger_type();
        void set_triggered(bool value);
        GameWorldObject to_world_level_object();
        void Update(GameTime gameTime, float rotation);
        void Draw(SpriteBatch spriteBatch);
    }
}