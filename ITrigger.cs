using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace gate
{
    public interface ITrigger {
        RRect get_trigger_collision_box();
        bool is_triggered();
        GameWorldObject to_world_level_object();
        void Update(GameTime gameTime, float rotation);
        void Draw(SpriteBatch spriteBatch);
    }
}