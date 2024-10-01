using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using gate.Collision;
using gate.Interface;

namespace gate.Conditions
{
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

        private bool selected = false;

        private UIButton connect, disconnect;
        private List<UIButton> buttons;
        private int function_mode = 0; //0 - connect, 1 - disconnect

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
            
            connect = new UIButton(position + new Vector2(0, -30), 10, "c");
            disconnect = new UIButton(position + new Vector2(20, -30), 10, "d");
            buttons = new List<UIButton>();
            buttons.Add(connect);
            buttons.Add(disconnect);
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

            connect = new UIButton(position + new Vector2(0, -30), 10, "c");
            disconnect = new UIButton(position + new Vector2(20, -30), 10, "d");
            buttons = new List<UIButton>();
            buttons.Add(connect);
            buttons.Add(disconnect);
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

        public void set_selected(bool value) {
            selected = value;
        }

        public bool get_selected() {
            return selected;
        }

        public int get_function_mode() {
            return function_mode;
        }

        public List<UIButton> get_buttons() {
            return buttons;
        }

        public List<RRect> get_sub_boxes() {
            List<RRect> geometry = new List<RRect>();
            foreach (UIButton b in get_buttons()) {
                geometry.Add(b.get_collision_rect());
            }
            return geometry;
        }
        
        //function to check if all enemies (even certain enemies) are dead or not present in the given list
        public bool condition(GameTime gameTime, float rotation, RRect mouse_hitbox) {
            // if we are in edit mode (selected == true) we need to check and update the buttons
            if (selected) {
                //update buttons
                connect.Update(gameTime, rotation);
                disconnect.Update(gameTime, rotation);
                //specific logic to handle if they are clicked
                if (connect.check_clicked(mouse_hitbox, Mouse.GetState().RightButton == ButtonState.Pressed)) {
                    //set function mode to 0 so we can connect entities to this condition
                    function_mode = 0;
                    Console.WriteLine($"function_mode:{function_mode}-connect");
                } else if (disconnect.check_clicked(mouse_hitbox, Mouse.GetState().RightButton == ButtonState.Pressed)) {
                    //set function mode to 1 so we can disconnect entities to this condition
                    function_mode = 1;
                    Console.WriteLine($"function_mode:{function_mode}-disconnect");
                }
            }

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
            if (!obj_ids_to_remove.Contains(id)) {
                obj_ids_to_remove.Add(id);
            }
        }

        public void remove_obj_to_remove(int id) {
            if (obj_ids_to_remove.Contains(id)) {
                obj_ids_to_remove.Remove(id);
            }
        }

        public void add_enemy_id_to_check(int id) {
            if (!enemy_ids.Contains(id)) {
                enemy_ids.Add(id);
            }
        }

        public void remove_enemy_id_from_check(int id) {
            if (enemy_ids.Contains(id)) {
                enemy_ids.Remove(id);
            }
        }

        public List<int> get_enemy_ids() {
            return enemy_ids;
        }

        public List<int> get_obj_ids_to_remove() {
            return obj_ids_to_remove;
        }
    }
}