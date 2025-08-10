using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using gate.Core;
using gate.Entities;
using gate.Interface;

namespace gate.Navigation
{
    public class NavGraph
    {
        public List<NavNode> nodes;

        public NavGraph(World world, List<FloorEntity> floor_entities, Dictionary<(int, int), List<IEntity>> chunked_collision_geometry) {
            this.nodes = build_navgraph(world, floor_entities, chunked_collision_geometry);
        }

        public List<NavNode> build_navgraph(World world, List<FloorEntity> floor_entities, Dictionary<(int, int), List<IEntity>> chunked_collision_geometry) {
            //loop over the floor entities to create 4 points within of equal distance from the corners and the center
            //make sure the points are chunked like the collision geometry
            //loop over all the chunks in both the collision geometry map and the chunked points and prune the ones that intersect with the collision geometry
            //loop over again and connect points by ray casting to test if the edge between is walkable based on the collision map

            List<NavNode> nodes = new List<NavNode>();

            foreach (FloorEntity f in floor_entities) {
                if (f is IEntity) {
                    //convert to tile for calculation
                    Tile t = (Tile)f;
                    List<Vector2> tile_points = new List<Vector2>();
                    
                    //generate points in rectangle (center, and in the center of the 4 quadrants)
                    Vector2 center = new Vector2(t.draw_position.X + (t.width/2), t.draw_position.Y + (t.height/2));
                    Vector2 nw = new Vector2(t.draw_position.X + (t.width/4), t.draw_position.Y + (t.height/4));
                    Vector2 ne = new Vector2(t.draw_position.X + (3*t.width/4), t.draw_position.Y + (t.height/4));
                    Vector2 sw = new Vector2(t.draw_position.X + (t.width/4), t.draw_position.Y + (3*t.height/4));
                    Vector2 se = new Vector2(t.draw_position.X + (3*t.width/4), t.draw_position.Y + (3*t.height/4));
                    
                    //add to list
                    tile_points.Add(center);
                    tile_points.Add(nw);
                    tile_points.Add(ne);
                    tile_points.Add(sw);
                    tile_points.Add(se);

                    List<Vector2> removal_list = new List<Vector2>();

                    foreach (Vector2 point in tile_points) {
                        int point_chunk_x = (int)Math.Floor(center.X / Constant.collision_map_chunk_size);
                        int point_chunk_y = (int)Math.Floor(center.Y / Constant.collision_map_chunk_size);

                        //pull nearby geometry from chunked data structure
                        List<IEntity> nearby_geometry = world.get_nearby_chunk_geometry_entities((point_chunk_x, point_chunk_y), 1);
                        
                        //iterate over nearby geometry
                        foreach (IEntity ng in nearby_geometry) {
                            if (ng is ICollisionEntity) {
                                ICollisionEntity ce = (ICollisionEntity)ng;
                                //if the points do collide with any nearby geometry, add them to removal list
                                if (ce.get_hurtbox().collision(point)) {
                                    if (!removal_list.Contains(point)) {
                                        removal_list.Add(point);
                                    }
                                }
                            }
                        }
                    }

                    Console.WriteLine($"Removal list count: {removal_list.Count}");
                    
                    //remove points from list
                    foreach (Vector2 remove_point in removal_list) {
                        if (tile_points.Contains(remove_point)) {
                            tile_points.Remove(remove_point);
                        }
                    }
                    
                    //create and add nodes
                    foreach (Vector2 point in tile_points) {
                        nodes.Add(new NavNode(point, new List<NavNode>()));
                    }
                }
            }

            //connect nodes
            float connection_radius = 35f;
            for (int i = 0; i < nodes.Count; i++) {
                NavNode nodeA = nodes[i];
                for (int j = 0; j < nodes.Count; j++) {
                    NavNode nodeB = nodes[j];
                    
                    //check to make sure distance is less than or equal to connection radius
                    if (Vector2.Distance(nodeA.position, nodeB.position) <= connection_radius) {
                        //calculate midpoint to find chunk to pull
                        Vector2 midpoint = new Vector2((nodeA.position.X + nodeB.position.X)/2, (nodeA.position.Y + nodeB.position.Y)/2);

                        int midpoint_chunk_x = (int)Math.Floor(midpoint.X / Constant.collision_map_chunk_size);
                        int midpoint_chunk_y = (int)Math.Floor(midpoint.Y / Constant.collision_map_chunk_size);

                        //pull nearby geometry from chunked data structure
                        List<IEntity> nearby_geometry = world.get_nearby_chunk_geometry_entities((midpoint_chunk_x, midpoint_chunk_y), 2);
                        
                        //if line between nodes does not intersect with any geometry, then add the two as neighbors
                        foreach (IEntity ng in nearby_geometry) {
                            if (ng is ICollisionEntity) {
                                ICollisionEntity ce = (ICollisionEntity)ng;
                                if (!ce.get_hurtbox().collision(nodeA.position, nodeB.position)) {
                                    //add neighbors
                                    if (!nodeA.neighbors.Contains(nodeB)) {
                                        nodeA.neighbors.Add(nodeB);
                                    }
                                    if (!nodeB.neighbors.Contains(nodeA)) {
                                        nodeB.neighbors.Add(nodeA);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            Console.WriteLine("BUILT NAV GRAPHHHHHHHHHHHHH");
            foreach (NavNode n in nodes) {
                Console.WriteLine($"n:{n.position}, {n.neighbors.Count}");
            }

            return nodes;
        }

        public void clear() {
            nodes.Clear();
        }

        public void Draw(SpriteBatch spriteBatch) {
            foreach (NavNode n in nodes) {
                n.Draw(spriteBatch);
            }
        }
    }
}