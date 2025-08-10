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

        public void Draw(SpriteBatch spriteBatch) {
            //draw line to each neighbor
            foreach (NavNode n in neighbors) {
                Renderer.DrawALine(spriteBatch, Constant.pixel, 1f, Color.Pink, 1f, position, n.position);
            }
            //draw rect for current position
            Renderer.FillRectangle(spriteBatch, position, 3, 3, Color.Purple, 1f);
        }
    }
}