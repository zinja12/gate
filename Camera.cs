using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using gate.Collision;

namespace gate
{
    public class Camera
    {
        private Matrix transform;
        public Matrix Transform
        {
            get { return transform; }
        }

        private Vector2 center;
        private Vector2 offset;
        private Viewport viewport;

        private float zoom = 1.0f;
        private float rotation = 0.0f;
        private float pitch = 0.0f;

        private bool rotating = false;
        private float target_rotation = 0f;

        public float X
        {
            get { return center.X; }
            set { center.X = value; }
        }

        public float Y
        {
            get { return center.Y; }
            set { center.Y = value; }
        }

        public float Zoom
        {
            get { return zoom; }
            set
            {
                zoom = value;
                if (zoom < 0.1f)
                    zoom = 0.1f;
            }
        }

        public float Rotation
        {
            get { return rotation; }
            set { rotation = value; }
        }

        public float Pitch
        {
            get { return pitch; }
            set
            {
                pitch = value;
                if (pitch < 0.0f)
                    pitch = 0.0f;
            }
        }

        public List<Vector2> get_viewport_boundary_points() {
            //calculate the corners of the viewport in world space
            return new List<Vector2>() {
                //top-left - 0
                Vector2.Transform(Vector2.Zero, Matrix.Invert(Transform)),
                //top-right - 1
                Vector2.Transform(new Vector2(viewport.Width-10, 0), Matrix.Invert(Transform)),
                //bottom-left - 2
                Vector2.Transform(new Vector2(0, viewport.Height-10), Matrix.Invert(Transform)),
                //bottom-right - 3
                Vector2.Transform(new Vector2(viewport.Width-10, viewport.Height-10), Matrix.Invert(Transform))
            };
        }
        
        //returns the indexes of the points that are outside of the rect
        //indexes correspond to the ordering from the get_viewport_boundary_points above
        public List<int> get_points_outside_rect(RRect rect) {
            List<Vector2> viewport_boundary_points = get_viewport_boundary_points();
            List<int> point_idxs = new List<int>();
            for (int i = 0; i < viewport_boundary_points.Count; i++) {
                if (rect.is_point_outside(viewport_boundary_points[i])) {
                    point_idxs.Add(i);
                }
            }
            return point_idxs;
        }

        public void rotate_to_target(float degrees) {
            //convert degrees to radians
            float radians = MathHelper.ToRadians(degrees);
            //set target and rotating
            this.target_rotation = radians;
            this.rotating = true;
        }

        private void rotate_camera_to_target() {
            // if we are currently rotating and are still not at the target rotation
            if (rotating){
                if (rotation == target_rotation) {
                    //reached the target so turn off rotation
                    rotating = false;
                    target_rotation = 0f;
                } else {
                    rotation += 0.01f;
                }
            }
        }

        public void set_camera_offset(Vector2 offset) {
            this.offset = offset;
        }

        public Camera(Viewport viewport)
        {
            this.viewport = viewport;
        }

        public Camera(Viewport viewport, Vector2 init_position){
            this.viewport = viewport;
            this.center = init_position;
        }

        public void update_viewport(Viewport viewport, Vector2 position) {
            Console.WriteLine("Viewport Width/Height:" + viewport.Width + "," + viewport.Height);
            this.viewport = viewport;
            this.center = position;
        }

        public void Update(Vector2 position)
        {
            center = position;
            transform = Matrix.CreateTranslation(new Vector3(-center.X + offset.X, -center.Y + offset.Y, 0)) *
                                                 Matrix.CreateRotationZ(Rotation) *
                                                 Matrix.CreateScale(Zoom) *
                                                 Matrix.CreateTranslation(new Vector3((viewport.Width) / 2, (viewport.Height) / 2, 0));
        }
    }
}