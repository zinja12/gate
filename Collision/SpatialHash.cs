using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using gate.Core;
using gate.Interface;

namespace gate.Collision
{
    public class SpatialHash
    {
        private int cell_size;
        private Dictionary<(int, int), List<ICollisionEntity>> grid;
        private float rotation;

        public SpatialHash(int cell_size, float initial_rotation) {
            this.cell_size = cell_size;
            this.grid = new Dictionary<(int, int), List<ICollisionEntity>>();
            set_rotation(initial_rotation);
        }

        public void set_rotation(float rotation) {
            this.rotation = rotation;
        }

        private Vector2 unrotate_position(Vector2 position) {
            float sin = (float)Math.Sin(-rotation);
            float cos = (float)Math.Cos(-rotation);
            return new Vector2(
                position.X * cos - position.Y * sin,
                position.X * sin + position.Y * cos
            );
        }

        public (int, int) get_cell_key(Vector2 position) {
            Vector2 unrotated_position = unrotate_position(position);
            int cx = (int)Math.Floor(unrotated_position.X / cell_size);
            int cy = (int)Math.Floor(unrotated_position.Y / cell_size);
            return (cx, cy);
        }

        public void add_entity(ICollisionEntity e, Vector2 position) {
            (int, int) cell_key = get_cell_key(position);
            
            //create
            if (!grid.ContainsKey(cell_key)) {
                grid.Add(cell_key, new List<ICollisionEntity>());
            }
            
            //add entity to list
            if (!grid[cell_key].Contains(e)) {
                grid[cell_key].Add(e);
            }
        }

        public void remove_entity(ICollisionEntity e, Vector2 position) {
            (int, int) cell_key = get_cell_key(position);
            
            //check to make sure we have the key
            if (grid.ContainsKey(cell_key)) {
                //remove
                bool removed = grid[cell_key].Remove(e);
                if (!removed) {
                    Console.WriteLine($"spatial hash entity either not found or not removed");
                }
            }
        }

        public void update_entity(ICollisionEntity e, Vector2 position) {
            remove_entity(e, position);
            add_entity(e, position);
        }
    }
}