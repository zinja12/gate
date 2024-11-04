using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using gate.Serialize;
using gate.Collision;

namespace gate.Interface
{
    public enum TriggerType {
        Level = 1,
        Rotation = 2,
        Script = 3
    }

    public interface ITrigger {
        RRect get_trigger_collision_box();
        bool is_triggered();
        TriggerType get_trigger_type();
        void set_triggered(bool value);
        GameWorldTrigger to_world_level_trigger();
        void Update(GameTime gameTime, float rotation);
        void Draw(SpriteBatch spriteBatch);
    }
}