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
    public class ScriptTrigger : ITrigger 
    {
        public Vector2 position;

        public RRect trigger_box;

        protected bool triggered;
        protected bool previously_activated, retrigger;
        protected Player player;

        protected static bool debug = true;

        protected int ID;
        
        public ScriptTrigger(Vector2 position, float width, float height, bool previously_activated, bool retrigger, Player player, int ID) {
            this.position = position;
            trigger_box = new RRect(position, width, height);
            triggered = false;
            this.player = player;

            this.previously_activated = previously_activated;
            this.retrigger = retrigger;

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

        public bool get_retrigger() {
            return retrigger;
        }

        public void set_previously_activated(bool value) {
            this.previously_activated = value;
        }

        public bool get_previously_activated() {
            return previously_activated;
        }

        public virtual void set_triggered(bool value) {
            this.triggered = value;
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
            return TriggerType.Script;
        }

        public virtual GameWorldTrigger to_world_level_trigger() {
            Console.WriteLine($"saving script trigger previously activated value:{previously_activated}");
            return new GameWorldTrigger {
                object_identifier = "script_trigger",
                object_id_num = get_obj_ID_num(),
                x_position = position.X,
                y_position = position.Y,
                width = trigger_box.width,
                height = trigger_box.height,
                previously_activated = previously_activated,
                retrigger = retrigger
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