using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using gate.Core;

namespace gate.Navigation
{
    public class NavNode
    {
        public Vector2 position;
        public List<NavNode> neighbors;
        public float cost = 1f;

        public NavNode(Vector2 position, List<NavNode> neighbors, float cost = 1f) {
            this.position = position;
            this.neighbors = neighbors;
            this.cost = cost;
        }
    }
}