using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using gate.Serialize;
using gate.Interface;
using gate.Entities;
using gate.Triggers;
using gate.Collision;

namespace gate
{
    public class RotationTrigger : LevelTrigger 
    {
        private float rotation_value;

        public RotationTrigger(Vector2 position, float width, float height, float rotation_value, Player player, int ID) 
            : base(position, width, height, "", player, ID) {
            this.rotation_value = rotation_value;
        }

        public override void Update(GameTime gameTime, float rotation) {
            //update trigger volume
            trigger_box.update(rotation/1000, position);

            //check for player collision
            if (!triggered && player.check_hurtbox_collisions(trigger_box)) {
                triggered = true;
            }
        }

        public float get_rotation_value() {
            return rotation_value;
        }

        public override TriggerType get_trigger_type() {
            return TriggerType.Rotation;
        }

        public override GameWorldObject to_world_level_object() {
            return new GameWorldObject {
                object_identifier = "rotation_trigger",
                object_id_num = get_obj_ID_num(),
                x_position = position.X,
                y_position = position.Y,
                width = trigger_box.width,
                height = trigger_box.height,
                goal_rotation = this.rotation_value
            };
        }
    }
}