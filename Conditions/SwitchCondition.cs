using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using gate.Collision;
using gate.Interface;
using gate.Entities;
using gate.Particles;
using gate.Core;

namespace gate.Conditions
{
    //TODO: in order to be instantiated correctly, please insert a switch into enemy ids in the json
    //otherwise the game will not create it, need to fix this later
    public class SwitchCondition : ICondition
    {
        private string name = "switch_condition";
        private int id;
        private bool triggered = false;
        private Vector2 position;
        private RRect rect;
        private int size = 50;

        private float rotation;
        
        private List<IEntity> switches;
        private List<int> switch_ids;
        private List<int> obj_ids_to_remove;

        private bool selected = false;

        private UIButton connect, disconnect;
        private List<UIButton> buttons;
        private int function_mode = 0; //0 - connect, 1 - disconnect

        private string tag;

        private World world;

        public SwitchCondition(int id, World world, List<IEntity> switches, List<int> switch_ids, List<int> obj_ids_to_remove, Vector2 position, string tag) {
            this.id = id;
            this.world = world;
            this.switches = switches;
            this.switch_ids = switch_ids;
            this.obj_ids_to_remove = obj_ids_to_remove;

            this.position = position;
            this.rect = new RRect(this.position, size, size);

            this.tag = tag;

            connect = new UIButton(position + new Vector2(0, -30), 10, "c");
            disconnect = new UIButton(position + new Vector2(20, -30), 10, "d");
            buttons = new List<UIButton>();
            buttons.Add(connect);
            buttons.Add(disconnect);
        }

        public string get_tag() {
            return tag;
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

        public bool condition(GameTime gameTime, float rotation, RRect mouse_hitbox) {
            this.rotation = rotation;

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

            //general idea for this condition being met:
            //iterate through the switches and check if they are still active
            //we do this by iterating, pulling the swtich from the list, checking if it is still active and incrementing count
            //if the switch is inactive it must have been triggered by the player
            //if all the switches tracked by this condition in swtich_ids has been triggered then count should equal switch_ids.Count
            
            // if switch ids is not empty
            if (switch_ids.Count > 0) {
                int count = 0;
                foreach (int id in switch_ids) {
                    //pull switch by id from list
                    IEntity e = switches.FirstOrDefault(s => s.get_obj_ID_num() == id);
                    if (e != null) {
                        HitSwitch hs = (HitSwitch)e;
                        if (!hs.is_active()) {
                            count++;
                        }
                    }
                }
                return count == switch_ids.Count;
            }
            return false;
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
                    //clear entity by id
                    world.clear_entity_by_id(e_id);
                    world.add_world_particle_system(
                        new ParticleSystem(true,
                        Constant.rotate_point(
                            //TODO: this might get cumbersome with adding the particle systems because we need to find the entity in the list each time since we are requesting by entity id
                            //however for now this seems to work
                            world.find_entity_by_id(e_id).get_base_position(),
                            rotation,
                            1f,
                            Constant.direction_up),
                        2,
                        500f,
                        1,
                        5,
                        1,
                        3,
                        Constant.white_particles,
                        new List<Texture2D>() { Constant.footprint_tex })
                    );
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

        public void add_switch_id_to_check(int id) {
            if (!switch_ids.Contains(id)) {
                switch_ids.Add(id);
            }
        }

        public void remove_switch_id_from_check(int id) {
            if (switch_ids.Contains(id)) {
                switch_ids.Remove(id);
            }
        }

        public List<int> get_switch_ids() {
            return switch_ids;
        }

        public List<int> get_obj_ids_to_remove() {
            return obj_ids_to_remove;
        }

        public void clear_switch_ids() {
            switch_ids.Clear();
        }

        public void clear_remove_ids() {
            obj_ids_to_remove.Clear();
        }

        public void clear() {
            clear_switch_ids();
            clear_remove_ids();
        }
        
        //condition contains entity id
        public (bool, int) contains_entity(int entity_id) {
            if (switch_ids.Contains(entity_id)) {
                return (true, 0);
            } else if (obj_ids_to_remove.Contains(entity_id)) {
                return (true, 1);
            }
            return (false, -1);
        }
    }
}