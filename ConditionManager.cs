using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace gate
{
    public class ConditionManager
    {
        private Dictionary<int, ICondition> conditions;
        private World world;

        public ConditionManager(World world) {
            this.conditions = new Dictionary<int, ICondition>();
            this.world = world;
        }

        public ConditionManager(List<ICondition> conditions) {
            this.conditions = new Dictionary<int, ICondition>();
            //load all conditions from list into map
            foreach(ICondition c in conditions) {
                conditions[c.condition_id()] = c;
            }
        }

        public void Update(GameTime gameTime, float rotation) {
            //update all conditions
            foreach (KeyValuePair<int, ICondition> kv in conditions) {
                int id = kv.Key;
                ICondition c = kv.Value;
                bool condition_value = c.condition();
                if (condition_value) {
                    c.trigger_behavior();
                    c.set_triggered(condition_value);
                }
                //update rect
                if (world.is_editor_active()) {
                    //update rect only if the editor is active
                    c.get_rect().update(rotation, c.get_position());
                }
            }
        }

        public bool check_collision(ICondition c, RRect r) {
            return c.get_rect().collision(r);
        }

        //returns the first condition object that the rectangle collides with
        public ICondition find_condition_colliding(RRect r) {
            foreach (KeyValuePair<int, ICondition> kv in conditions) {
                int id = kv.Key;
                ICondition c = kv.Value;
                if (check_collision(c, r)) {
                    return c;
                }
            }
            return null;
        }

        public ICondition get_condition_by_id(int condition_id) {
            //if we don't have the id return null
            if (!conditions.ContainsKey(condition_id)) {
                return null;
            }
            //get the condition
            return conditions[condition_id];
        }

        public void add_condition(ICondition condition) {
            //don't add null conditions
            if (condition == null) { return; }
            //only add conditions we don't already have (repeats)
            if (!conditions.ContainsKey(condition.condition_id())) {
                conditions[condition.condition_id()] = condition;
            }
        }

        public void clear_conditions() {
            conditions.Clear();
        }

        public List<GameWorldCondition> get_world_level_list() {
            List<GameWorldCondition> world_conditions = new List<GameWorldCondition>();
            foreach (KeyValuePair<int, ICondition> kv in conditions) {
                int id = kv.Key;
                ICondition c = kv.Value;
                if (c is EnemiesDeadRemoveObjCondition) {
                    EnemiesDeadRemoveObjCondition edroc = (EnemiesDeadRemoveObjCondition)c;
                    world_conditions.Add(
                        new GameWorldCondition {
                            object_identifier = c.condition_name(),
                            object_id_num = c.condition_id(),
                            enemy_ids = edroc.get_enemy_ids(),
                            obj_ids_to_remove = edroc.get_obj_ids_to_remove(),
                            x_position = edroc.get_position().X,
                            y_position = edroc.get_position().Y
                        }
                    );
                }
            }
            return world_conditions;
        }

        public void Draw(SpriteBatch spriteBatch) {
            //draw rect with arrows to all linked objects
            foreach (KeyValuePair<int, ICondition> kv in conditions) {
                int id = kv.Key;
                ICondition c = kv.Value;
                Vector2 position = c.get_position();
                c.get_rect().set_color(Color.Purple);
                c.get_rect().draw(spriteBatch);
                if (c is EnemiesDeadRemoveObjCondition) {
                    EnemiesDeadRemoveObjCondition edroc = (EnemiesDeadRemoveObjCondition)c;
                    //draw connections to each enemy
                    foreach (int i in edroc.get_obj_ids_to_remove()) {
                        IEntity e = edroc.get_world_obj().find_entity_by_id(i);
                        Renderer.DrawALine(spriteBatch, Constant.pixel, 1, Color.Red, 1f, position, e.get_base_position());
                    }
                    if (edroc.get_enemy_ids().Count > 0) {
                        foreach (int j in edroc.get_enemy_ids()) {
                            IEntity e = edroc.get_world_obj().find_entity_by_id(j);
                            Renderer.DrawALine(spriteBatch, Constant.pixel, 1, Color.Blue, 1f, position, e.get_base_position());
                        }
                    } else {
                        spriteBatch.DrawString(Constant.arial_small, "all_enemies", position, Color.White);
                    }
                    if (edroc.get_selected()) {
                        //display selection options
                        Renderer.FillRectangle(spriteBatch, c.get_position() + new Vector2(0, -35), 10, 10, Color.Red);
                        Renderer.FillRectangle(spriteBatch, c.get_position() + new Vector2(15, -35), 10, 10, Color.Blue);
                    }
                }
            }
        }
    }
}