using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace gate.Interface
{
    public interface IGameObject
    {
        int get_obj_ID_num();
        void set_obj_ID_num(int id_num);
    }
}