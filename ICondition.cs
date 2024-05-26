using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace gate
{
    public interface ICondition {
        string condition_name();
        int condition_id();
        bool condition();
        void set_triggered(bool trigger_value);
        bool get_triggered();
        void trigger_behavior();
        Vector2 get_position();
        RRect get_rect();
    }

    public class ObjectCondition {
        //TODO: generic obj condition? pass in function?
    }

    public class EnemiesDeadRemoveObjCondition : ICondition
    {
        private string name = "all_enemies_dead_remove_objs";
        private int id;
        private bool triggered = false;
        private Vector2 position;
        private RRect rect;
        private int size = 50;

        private List<IAiEntity> enemies;
        //enemy ids to only check for certain enemy deaths
        private List<int> enemy_ids;
        private List<int> obj_ids_to_remove;

        private bool selected = false, select_mode = true;

        private World world;

        //all enemies dead
        public EnemiesDeadRemoveObjCondition(int id, World world, List<IAiEntity> enemies, List<int> obj_ids_to_remove, Vector2 position) {
            this.id = id;
            this.world = world;
            this.enemies = enemies;
            this.enemy_ids = new List<int>();
            this.obj_ids_to_remove = obj_ids_to_remove;
            
            this.position = position;
            this.rect = new RRect(this.position, size, size);
        }
        
        //certain enemies dead
        public EnemiesDeadRemoveObjCondition(int id, World world, List<IAiEntity> enemies, List<int> enemy_ids, List<int> obj_ids_to_remove, Vector2 position) {
            this.id = id;
            this.world = world;
            this.enemies = enemies;
            this.enemy_ids = enemy_ids;
            this.obj_ids_to_remove = obj_ids_to_remove;

            this.position = position;
            this.rect = new RRect(this.position, size, size);
        }

        public Vector2 get_position() {
            return position;
        }

        public RRect get_rect() {
            return rect;
        }

        public string condition_name() {
            return name;
        }

        public int condition_id() {
            return id;
        }

        public World get_world_obj() {
            return world;
        }

        public void set_select_mode(bool value) {
            select_mode = value;
        }

        public void invert_select_mode() {
            select_mode = !select_mode;
        }

        public void set_selected(bool value) {
            selected = value;
        }

        public bool get_selected() {
            return selected;
        }
        
        //function to check if all enemies (even certain enemies) are dead or not present in the given list
        public bool condition() {
            // if we need to check for specific enemies then we iterate
            if (enemy_ids.Count > 0) {
                int count = enemy_ids.Count;
                foreach (int id in enemy_ids) {
                    if (enemies.Any(ai => ai.get_ID_num() == id)) {
                        //we found the id in the enemies array which means it is still active/alive so decrease the count
                        count--;
                    }
                }
                //if the count is the same as the count of enemies we did not see then we return true
                return count == enemy_ids.Count;
            }
            return enemies.Count == 0;
        }

        public void set_triggered(bool trigger_value) {
            this.triggered = trigger_value;
        }

        public bool get_triggered() {
            return triggered;
        }

        public void trigger_behavior() {
            if (!triggered) {
                triggered = true;
                //remove certain object(s) from the world
                foreach (int e_id in obj_ids_to_remove) {
                    world.clear_entity_by_id(e_id);
                }
            }
        }

        public void add_obj_to_remove(int id) {
            obj_ids_to_remove.Add(id);
        }

        public void add_enemy_id_to_check(int id) {
            enemy_ids.Add(id);
        }

        public List<int> get_enemy_ids() {
            return enemy_ids;
        }

        public List<int> get_obj_ids_to_remove() {
            return obj_ids_to_remove;
        }
    }
}