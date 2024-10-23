using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using gate.Interface;
using gate.Core;
using gate.Entities;

namespace gate.Lighting
{
    public class Light
    {
        private Vector2 center_position;
        
        private float radius;
        private float intensity;
        private Color color;

        private List<(Vector2, Vector2)> geometry_edges;

        private IEntity parent_entity;

        public Light(Vector2 position, float radius, float intensity, Color color, IEntity parent_entity = null) {
            this.center_position = position;
            this.radius = radius;
            this.intensity = intensity;
            this.color = color;

            this.geometry_edges = new List<(Vector2, Vector2)>();

            if (parent_entity != null) {
                this.parent_entity = parent_entity;
            }
        }

        public void Update(GameTime gameTime, float rotation) {
            //if parent entity is not null, light position is set to that of the parent entity
            if (parent_entity != null) {
                center_position = parent_entity.get_base_position();
            }
        }

        public void calculate_in_range_geometry(List<IEntity> geometry, List<IEntity> entity_geometry, List<IEntity> excluded_geometry, Vector2 player_position, float render_distance) {
            //clear geometry edges
            geometry_edges.Clear();
            //calculate geometry to calculate against for this light
            //construct list of edges from shadow caster geometry
            foreach (IEntity g in geometry) {
                //skip any entities that are excluded
                //this will make is so they are not included in the geometry that the light needs to be built against
                //which should allow it to not be affected by lights when it is in range of a light
                if (excluded_geometry.Contains(g)) {
                    continue;
                }
                //if the light and player are within render_distance/2 AND if the light and the geometry are within the light distance
                if (Vector2.Distance(center_position, player_position) < render_distance/2 && Vector2.Distance(center_position, g.get_base_position()) < Constant.light_distance*1.5f) {
                    if (g is StackedObject) {
                        StackedObject so = (StackedObject)g;
                        foreach (KeyValuePair<Vector2, Vector2> kv in so.get_hurtbox().edges) {
                            //add to geometry map
                            geometry_edges.Add((kv.Key, kv.Value));
                        }
                    }
                }
            }

            //add ai hitbox geometry to geometry edges for light geometry calculation
            foreach (IEntity e in entity_geometry) {
                if (excluded_geometry.Contains(e)) {
                    continue;
                }
                if (Vector2.Distance(center_position, player_position) < render_distance/2 && Vector2.Distance(center_position, e.get_base_position()) < Constant.light_distance*1.5) {
                    if (e is ICollisionEntity) {
                        //convert to collision entity for access to geometry data
                        ICollisionEntity ce = (ICollisionEntity)e;
                        foreach (KeyValuePair<Vector2, Vector2> kv in ce.get_hurtbox().edges) {
                            //add to geometry map
                            geometry_edges.Add((kv.Key, kv.Value));
                        }
                    }
                }
            }
        }

        public List<(Vector2, Vector2)> get_geometry_edges() {
            return geometry_edges;
        }

        public Vector2 get_center_position() {
            return center_position;
        }

        public Vector2 get_draw_position() {
            return center_position - new Vector2(radius, radius);
        }

        public float get_radius() {
            return radius;
        }

        public Color get_color() {
            return color;
        }

        public float get_intensity() {
            return intensity;
        }
    }
}