using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using gate.Serialize;
using gate.Interface;
using gate.Entities;
using gate.Collision;

namespace gate.Triggers
{
    public class LevelTrigger : ITrigger 
    {
        public Vector2 position;

        public RRect trigger_box;

        protected bool triggered;
        private string level_id;
        protected Player player;

        protected static bool debug = true;

        protected int ID;
        
        public LevelTrigger(Vector2 position, float width, float height, string level_id, Player player, int ID) {
            this.position = position;
            trigger_box = new RRect(position, width, height);
            triggered = false;
            this.level_id = level_id;
            this.player = player;

            this.ID = ID;
        }

        public virtual void Update(GameTime gameTime, float rotation) {
            //update trigger volume
            trigger_box.update(rotation / 1000, position);

            //check for player collision
            if (!triggered && player.check_hurtbox_collisions(trigger_box)) {
                triggered = true;
            }
        }

        public bool is_triggered() {
            return triggered;
        }

        public virtual void set_triggered(bool value) {
            this.triggered = value;
        }

        public string get_level_id() {
            return level_id;
        }

        public RRect get_trigger_collision_box() {
            return trigger_box;
        }

        public void set_obj_ID_num(int id_num) {
            this.ID = id_num;
        }

        public int get_obj_ID_num() {
            return this.ID;
        }

        public virtual TriggerType get_trigger_type() {
            return TriggerType.Level;
        }

        public virtual GameWorldObject to_world_level_object() {
            return new GameWorldObject {
                object_identifier = "level_trigger",
                object_id_num = get_obj_ID_num(),
                x_position = position.X,
                y_position = position.Y,
                width = trigger_box.width,
                height = trigger_box.height,
                level_id = this.level_id
            };
        }

        public void Draw(SpriteBatch spriteBatch) {
            //draw collision geometry in debug mode
            if (debug) {
                trigger_box.draw(spriteBatch);
            }
        }
    }
}