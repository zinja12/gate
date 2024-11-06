using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using gate.Collision;

namespace gate.Interface
{
    public interface ICondition {
        string condition_name();
        int condition_id();
        bool condition(GameTime gameTime, float rotation, RRect mouse_hitbox);
        void set_triggered(bool trigger_value);
        bool get_triggered();
        void trigger_behavior();
        Vector2 get_position();
        RRect get_rect();
        List<RRect> get_sub_boxes();
        string get_tag();
    }

    public class ObjectCondition {
        //TODO: generic obj condition? pass in function?
    }
}