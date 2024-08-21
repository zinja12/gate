using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace gate
{
    public interface ICollisionEntity {
        void take_hit(IEntity entity, int damage);
        RRect get_hurtbox();
        bool is_hurtbox_active();
    }
}