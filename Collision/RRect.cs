using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using gate.Core;

namespace gate.Collision
{
    //NOTE: vertices/edges for rrect are clockwise wound (clockwise winding)
    public class RRect
    {
        public Vector2 position; //position to denote center of rectangle
        public Vector2 top_left_position;
        public Vector2 top_right_position;
        public Vector2 bottom_right_position;
        public Vector2 bottom_left_position;
        public float width;
        public float height;
        public Dictionary<Vector2, Vector2> edges;
        public List<Vector2> normals;
        public List<Vector2> verts;
        public List<KeyValuePair<Vector2, Vector2>> ordered_edges;

        public float rotation, rotation_offset;

        //directions to rotate points
        private Vector2 dir_top_left = new Vector2(-1, -1);
        private Vector2 dir_top_right = new Vector2(1, -1);
        private Vector2 dir_bottom_left = new Vector2(-1, 1);
        private Vector2 dir_bottom_right = new Vector2(1, 1);

        private bool draw_rect = true;
        private bool draw_positions = false;

        private Color draw_color = Color.Green;

        public RRect(Vector2 center, float width, float height) {
            //set position and width/height
            this.position = center;
            this.width = width;
            this.height = height;
            //calculate other points
            top_left_position = new Vector2(center.X - width/2, center.Y - height/2);
            top_right_position = new Vector2();
            bottom_left_position = new Vector2(center.X, center.Y - height/2);
            bottom_right_position = new Vector2();
            //create edges map
            edges = new Dictionary<Vector2, Vector2>();
            ordered_edges = new List<KeyValuePair<Vector2, Vector2>>();

            verts = new List<Vector2>();

            normals = new List<Vector2>();
        }

        public void update(float rotation, Vector2 center_position) {
            this.rotation = rotation + rotation_offset;
            this.position = center_position;
            //rotate all points around center
            //width/2 denotes distance away from rotation point
            top_left_position = position + (width/2) * new Vector2(dir_top_left.X * (float)Math.Cos(-this.rotation) - dir_top_left.Y * (float)Math.Sin(-this.rotation), dir_top_left.Y * (float)Math.Cos(-this.rotation) + dir_top_left.X * (float)Math.Sin(-this.rotation));
            top_right_position = position + (width/2) * new Vector2(dir_top_right.X * (float)Math.Cos(-this.rotation) - dir_top_right.Y * (float)Math.Sin(-this.rotation), dir_top_right.Y * (float)Math.Cos(-this.rotation) + dir_top_right.X * (float)Math.Sin(-this.rotation));
            bottom_left_position = position + (width/2) * new Vector2(dir_bottom_left.X * (float)Math.Cos(-this.rotation) - dir_bottom_left.Y * (float)Math.Sin(-this.rotation), dir_bottom_left.Y * (float)Math.Cos(-this.rotation) + dir_bottom_left.X * (float)Math.Sin(-this.rotation));
            bottom_right_position = position + (width/2) * new Vector2(dir_bottom_right.X * (float)Math.Cos(-this.rotation) - dir_bottom_right.Y * (float)Math.Sin(-this.rotation), dir_bottom_right.Y * (float)Math.Cos(-this.rotation) + dir_bottom_right.X * (float)Math.Sin(-this.rotation));

            //right hand rule around rect
            edges.Clear();
            edges.Add(top_left_position, top_right_position);
            edges.Add(top_right_position, bottom_right_position);
            edges.Add(bottom_right_position, bottom_left_position);
            edges.Add(bottom_left_position, top_left_position);

            ordered_edges.Clear();
            foreach(KeyValuePair<Vector2, Vector2> entry in edges)
            {
                ordered_edges.Add(entry);
            }

            verts.Clear();
            verts.Add(top_left_position);
            verts.Add(top_right_position);
            verts.Add(bottom_right_position);
            verts.Add(bottom_left_position);
        }

        public bool collision(RRect r) {
            /*Separating Axis Theorem Collision System*/
            //loop through all edges in both polygons
            //project all points onto normals(predefined axis) of every edge and check for any separation
            //if no separation -> collision
            //if separation -> no collision

            //calculate normals for two polygons
            normals = new List<Vector2>();
            //iterate over all edges in current rect
            foreach(KeyValuePair<Vector2, Vector2> points_edge in edges) {
                Vector2 a = points_edge.Key;
                Vector2 b = points_edge.Value;
                //calculate edge direction
                Vector2 edge = b - a;
                //calculate normal
                Vector2 normal = new Vector2(-edge.Y, edge.X);
                //set up min and max
                float minA = float.MaxValue;
                float maxA = float.MinValue;
                foreach (Vector2 v in verts) { //iterate over vertices
                    float proj = Vector2.Dot(v, normal); //calculate projection onto normal
                    //set min max
                    if (proj < minA) { minA = proj; }
                    if (proj > maxA) { maxA = proj; }
                }
                float minB = float.MaxValue;
                float maxB = float.MinValue;
                foreach (Vector2 v in r.verts) {
                    float proj = Vector2.Dot(v, normal);
                    if (proj < minB) { minB = proj; }
                    if (proj > maxB) { maxB = proj; }
                }
                //check for gaps
                if (minA >= maxB || minB >= maxA) {
                    draw_color = Color.Green;
                    return false;
                }
            }
            //iterate over all edges in other rect
            foreach(KeyValuePair<Vector2, Vector2> points_edge in r.edges) {
                Vector2 a = points_edge.Key;
                Vector2 b = points_edge.Value;
                Vector2 edge = b - a;
                Vector2 normal = new Vector2(-edge.Y, edge.X);
                float minA = float.MaxValue;
                float maxA = float.MinValue;
                foreach (Vector2 v in verts) {
                    float proj = Vector2.Dot(v, normal);
                    if (proj < minA) { minA = proj; }
                    if (proj > maxA) { maxA = proj; }
                }
                float minB = float.MaxValue;
                float maxB = float.MinValue;
                foreach (Vector2 v in r.verts) {
                    float proj = Vector2.Dot(v, normal);
                    if (proj < minB) { minB = proj; }
                    if (proj > maxB) { maxB = proj; }
                }
                if (minA >= maxB || minB >= maxA) {
                    draw_color = Color.Green;
                    return false;
                }
            }

            draw_color = Color.Red;
            return true;
        }

        public bool collision(Vector2 point) {
            float left = top_left_position.X;
            float right = top_right_position.X;
            float top = top_left_position.Y;
            float bottom = bottom_left_position.Y;
            
            return point.X >= left && point.X <= right &&
                point.Y >= top && point.Y <= bottom;
        }

        public bool collision(Vector2 start, Vector2 end) {
            float step_distance = 8f;
            Vector2 direction = end - start;
            if (direction != Vector2.Zero) direction.Normalize();
            float distance = Vector2.Distance(start, end);

            for (float d = 0; d < distance; d += step_distance) {
                Vector2 line_point_sample = start + direction * d;
                if (collision(line_point_sample)) {
                    return true;
                }
            }
            
            return false;
        }

        public void set_rotation_offset(float rotation_offset_degrees) {
            float radians_offset = MathHelper.ToRadians(rotation_offset_degrees);
            rotation_offset = radians_offset;
        }

        public float distance_from_edge_to_point(Vector2 edge_point1, Vector2 edge_point2, Vector2 point) {
            // Calculate the equation of the line in slope-intercept form (y = mx + b)
            float m = (edge_point2.Y - edge_point1.Y) / (edge_point2.X - edge_point1.X);
            float b = edge_point1.Y - m * edge_point1.X;

            // Calculate A, B, and C coefficients of the line equation Ax + By + C = 0
            float A = -m;
            float B = 1;
            float C = -b;

            // Calculate the perpendicular distance from the point to the line
            float distance = Math.Abs(A * point.X + B * point.Y + C) / (float)Math.Sqrt(A * A + B * B);
            //Console.WriteLine("distance:" + distance);
            return distance;
        }

        public int closest_edge(Vector2 point) {
            ordered_edges = new List<KeyValuePair<Vector2, Vector2>>();
            //insert dictionary entries into ordered list
            foreach(KeyValuePair<Vector2, Vector2> entry in edges)
            {
                ordered_edges.Add(entry);
            }
            //loop through edges and calculate distances
            float distance = 10000000f;
            int idx = 0;
            for (int i = 0; i < ordered_edges.Count; i++) {
                //Console.WriteLine("edge:" + ordered_edges[i].Key + ", " + ordered_edges[i].Value);
                float edge_distance_to_point = distance_from_edge_to_point(ordered_edges[i].Key, ordered_edges[i].Value, point);
                //Console.WriteLine("distance:" + edge_distance_to_point);
                if (edge_distance_to_point < distance) {
                    distance = edge_distance_to_point;
                    idx = i;
                }
            }
            return idx;
        }

        public Vector2 get_edge_normal(Vector2 start_point, Vector2 end_point) {
            //calculate normal based on edge given
            Vector2 direction = end_point - start_point;
            Vector2 normal = new Vector2(direction.Y, -direction.X);
            normal.Normalize();
            return normal;
        }

        public List<Vector2> get_edge_normals(List<KeyValuePair<Vector2, Vector2>> edges) {
            List<Vector2> edge_normals = new List<Vector2>();
            for (int i = 0; i < edges.Count(); i++) {
                edge_normals.Add(get_edge_normal(edges[i].Key, edges[i].Value));
            }
            return edge_normals;
        }

        public List<KeyValuePair<Vector2, Vector2>> get_edges() {
            return ordered_edges;
        }

        public bool is_point_outside(Vector2 point) {
            //transform the point to the local coordinate system of the rectangle
            Vector2 local_point = Vector2.Transform(point, Matrix.Invert(Matrix.CreateTranslation(new Vector3(position.X, position.Y, 0)) * Matrix.CreateRotationZ(-rotation)));
            //Vector2 local_point = Vector2.Transform(point, Matrix.Invert(Matrix.CreateTranslation(position) * Matrix.CreateRotationZ(-rotation)));

            //check if the transformed point is outside the rectangle
            if (local_point.X < -width / 2 || local_point.X > width / 2 || local_point.Y < -height / 2 || local_point.Y > height / 2) {
                return true; //point is outside
            } else {
                return false; //point is inside
            }
        }
        
        //specialized collision function to detect where a ray meets a set edge if at all
        public static bool ray_intersects_edge(Vector2 ray_origin, Vector2 ray_direction, Vector2 edge_start, Vector2 edge_end, out Vector2 intersection_point) {
            //intersection of line segments using parametrics
            Vector2 edge_direction = edge_end - edge_start;

            float denominator = (ray_direction.X * edge_direction.Y - ray_direction.Y * edge_direction.X);
            if (Math.Abs(denominator) < 0.0001f) {
                //parallel lines, no intersection
                intersection_point = Vector2.Zero;
                return false;
            }
            
            //t1 is the parameter for the ray
            //t2 is the parameter for the edge segment
            float t1 = ((edge_start.X - ray_origin.X) * edge_direction.Y - (edge_start.Y - ray_origin.Y) * edge_direction.X) / denominator;
            float t2 = ((edge_start.X - ray_origin.X) * ray_direction.Y - (edge_start.Y - ray_origin.Y) * ray_direction.X) / denominator;
            
            if (t1 >= 0 && t2 >= 0 && t2 <= 1) {
                //there is a valid intersection
                intersection_point = ray_origin + t1 * ray_direction;
                return true;
            }

            //base case
            intersection_point = Vector2.Zero;
            return false;
        }

        public Vector2 calculate_mtv(RRect r) {
            float min_overlap = 1000000000f;
            Vector2 mtv_axis = Vector2.Zero;

            //get all normals
            List<Vector2> axes = new List<Vector2>();
            axes.AddRange(get_edge_normals(get_edges()));
            axes.AddRange(r.get_edge_normals(r.get_edges()));

            foreach (Vector2 axis in axes) {
                // project both rectangles onto the axis
                (float min_a, float max_a) = project_onto_axis(axis, verts);
                (float min_b, float max_b) = project_onto_axis(axis, r.verts);

                //check overlap
                float overlap = Math.Min(max_a, max_b) - Math.Max(min_a, min_b);

                if (overlap <= 0) return Vector2.Zero; //no collision

                if (overlap < min_overlap) {
                    min_overlap = overlap;
                    mtv_axis = axis;
                }
            }

            //ensure mtv points in the right direction
            Vector2 direction = r.position - position;
            if (Vector2.Dot(direction, mtv_axis) < 0) {
                mtv_axis = -mtv_axis;
            }

            return mtv_axis * min_overlap; // mtv vector
        }

        private (float, float) project_onto_axis(Vector2 axis, List<Vector2> vertices) {
            float min = 1000000000f;
            float max = -1000000000f;

            foreach (Vector2 v in vertices) {
                float projection = Vector2.Dot(v, axis);
                min = Math.Min(min, projection);
                max = Math.Max(max, projection);
            }

            return (min, max);
        }

        public void set_color(Color color) {
            this.draw_color = color;
        }

        public void draw(SpriteBatch spriteBatch) {

            if (draw_positions) {
                //draw positions
                Renderer.DrawALine(spriteBatch, Constant.pixel, 2, Color.Red, 1f, position, position + new Vector2(0, -5));
                Renderer.DrawALine(spriteBatch, Constant.pixel, 2, Color.Blue, 1f, top_left_position, top_left_position + new Vector2(0, -5));
                Renderer.DrawALine(spriteBatch, Constant.pixel, 2, Color.Purple, 1f, top_right_position, top_right_position + new Vector2(0, -5));
                Renderer.DrawALine(spriteBatch, Constant.pixel, 2, Color.Green, 1f, bottom_left_position, bottom_left_position + new Vector2(0, -5));
                Renderer.DrawALine(spriteBatch, Constant.pixel, 2, Color.Yellow, 1f, bottom_right_position, bottom_right_position + new Vector2(0, -5));
            }
            if (draw_rect) {
                //draw rectangle
                Renderer.DrawALine(spriteBatch, Constant.pixel, 2, draw_color, 1f, top_left_position, top_right_position);
                Renderer.DrawALine(spriteBatch, Constant.pixel, 2, draw_color, 1f, top_left_position, bottom_left_position);
                Renderer.DrawALine(spriteBatch, Constant.pixel, 2, draw_color, 1f, bottom_left_position, bottom_right_position);
                Renderer.DrawALine(spriteBatch, Constant.pixel, 2, draw_color, 1f, top_right_position, bottom_right_position);
                Renderer.DrawALine(spriteBatch, Constant.pixel, 2, draw_color, 1f, top_left_position, bottom_right_position);
                Renderer.DrawALine(spriteBatch, Constant.pixel, 2, draw_color, 1f, top_right_position, bottom_left_position);
                //draw normals
                List<Vector2> edge_normals = get_edge_normals(get_edges());
                foreach (Vector2 v in edge_normals) {
                    Renderer.DrawALine(spriteBatch, Constant.pixel, 2, Color.Cyan, 1f, position, position+v*15f);
                }
            }
        }
    }
}