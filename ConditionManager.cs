using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using gate.Interface;
using gate.Core;
using gate.Collision;
using gate.Conditions;

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

        public void Update(GameTime gameTime, float rotation, RRect mouse_hitbox) {
            //update all conditions
            foreach (KeyValuePair<int, ICondition> kv in conditions) {
                int id = kv.Key;
                ICondition c = kv.Value;
                bool condition_value = c.condition(gameTime, rotation, mouse_hitbox);
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
        
        //checks all the subboxes that may be colliding with the rect
        public ICondition find_condition_sub_box_colliding(RRect r) {
            foreach (KeyValuePair<int, ICondition> kv in conditions) {
                int id = kv.Key;
                ICondition c = kv.Value;
                List<RRect> sub_boxes = c.get_sub_boxes();
                foreach (RRect sbr in sub_boxes) {
                    if (sbr.collision(r)) {
                        return c;
                    }
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

        public Dictionary<int, ICondition> get_conditions() {
            return conditions;
        }
        
        //retrieve condition by entity id that it listens to
        //return value is tuple of ICondition, int
        //where ICondition is the condition object and the int is either 0 or 1 depending on whether the entity is one being listened to or removed
        //0 - listened to
        //1 - removed
        public (ICondition, int) get_condition_by_entity_id(int entity_id) {
            foreach (KeyValuePair<int, ICondition> kv in conditions) {
                ICondition c = kv.Value;
                if (c is EnemiesDeadRemoveObjCondition) {
                    EnemiesDeadRemoveObjCondition edroc = (EnemiesDeadRemoveObjCondition)c;
                    (bool, int) contains = edroc.contains_entity(entity_id);
                    if (contains.Item1) {
                        return (edroc, contains.Item2);
                    }
                } else if (c is SwitchCondition) {
                    SwitchCondition sw_cond = (SwitchCondition)c;
                    (bool, int) contains = sw_cond.contains_entity(entity_id);
                    if (contains.Item1) {
                        return (sw_cond, contains.Item2);
                    }
                }
            }
            //return null if not found
            return (null, -1);
        }

        public void update_condition_listen_ids(int condition_id, List<int> listen_ids) {
            ICondition c = conditions[condition_id];
            if (c is EnemiesDeadRemoveObjCondition) {
                EnemiesDeadRemoveObjCondition edroc = (EnemiesDeadRemoveObjCondition)c;
                //clear lists
                edroc.clear_enemy_ids();
                //replace list values based on parameter
                foreach (int id in listen_ids) {
                    edroc.add_enemy_id_to_check(id);
                }
            } else if (c is SwitchCondition) {
                SwitchCondition sw_cond = (SwitchCondition)c;
                //clear lists
                sw_cond.clear_switch_ids();
                //replace values in list based on parameter
                foreach (int id in listen_ids) {
                    sw_cond.add_switch_id_to_check(id);
                }
            }
        }

        public void update_condition_remove_ids(int condition_id, List<int> remove_ids) {
            ICondition c = conditions[condition_id];
            if (c is EnemiesDeadRemoveObjCondition) {
                EnemiesDeadRemoveObjCondition edroc = (EnemiesDeadRemoveObjCondition)c;
                //clear list
                edroc.clear_remove_ids();
                //replace values
                foreach (int id in remove_ids) {
                    edroc.add_obj_to_remove(id);
                }
            } else if (c is SwitchCondition) {
                SwitchCondition sw_cond = (SwitchCondition)c;
                //clear list
                sw_cond.clear_remove_ids();
                //replace values
                foreach (int id in remove_ids) {
                    sw_cond.add_obj_to_remove(id);
                }
            }
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
                } else if (c is SwitchCondition) {
                    SwitchCondition sw_cond = (SwitchCondition)c;
                    world_conditions.Add(
                        new GameWorldCondition {
                            object_identifier = c.condition_name(),
                            object_id_num = c.condition_id(),
                            enemy_ids = sw_cond.get_switch_ids(),
                            obj_ids_to_remove = sw_cond.get_obj_ids_to_remove(),
                            x_position = sw_cond.get_position().X,
                            y_position = sw_cond.get_position().Y
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
                        foreach (UIButton b in edroc.get_buttons()) {
                            b.Draw(spriteBatch);
                        }
                    }
                } else if (c is SwitchCondition) {
                    SwitchCondition sw_cond = (SwitchCondition)c;
                    //draw connections to each object
                    foreach (int i in sw_cond.get_obj_ids_to_remove()) {
                        IEntity e = sw_cond.get_world_obj().find_entity_by_id(i);
                        Renderer.DrawALine(spriteBatch, Constant.pixel, 1, Color.Red, 1f, position, e.get_base_position());
                    }
                    if (sw_cond.get_switch_ids().Count > 0) {
                        foreach (int j in sw_cond.get_switch_ids()) {
                            IEntity e = sw_cond.get_world_obj().find_entity_by_id(j);
                            Renderer.DrawALine(spriteBatch, Constant.pixel, 1, Color.Blue, 1f, position, e.get_base_position());
                        }
                    }
                    if (sw_cond.get_selected()) {
                        //display selection options
                        foreach (UIButton b in sw_cond.get_buttons()) {
                            b.Draw(spriteBatch);
                        }
                    }
                }
            }
        }
    }
}