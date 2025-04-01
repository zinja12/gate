using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using gate.Interface;

namespace gate.Core
{
    public class RenderList
    {
        /*
        * The general philosophy behind this is to hold all the entities that need depth sorting.
        * Entities being drawn and sorted are stored in the entities list
        * and then moved in and out of the list based on distance to reduce the number of objects
        * that we need to sort on a regular basis. (Hopefully this improves frame rate a little bit
        * as I think we are starting to become CPU bound)
        */
        List<IEntity> entities;
        Dictionary<IEntity, int> all_entities; //values: 0 -> don't render, 1 -> render
        List<IEntity> keys;

        public RenderList()
        {
            entities = new List<IEntity>();
            keys = new List<IEntity>();
            all_entities = new Dictionary<IEntity, int>();
        }

        public List<IEntity> get_entities() {
            return entities;
        }

        public Dictionary<IEntity, int> get_all_entities() {
            return all_entities;
        }

        public void Add(IEntity e)
        {
            entities.Add(e);
            all_entities[e] = 1;
            //in theory this keys check should never get triggered during gameplay becuase all the keys should already be in the map, but if not add it for some reason
            //however it might get triggered in the editor though
            if (!keys.Contains(e)) {
                keys.Add(e);
            }
        }

        public IEntity get_entity_by_id(int id) {
            //iterate over all entities in order to find the requested entity by id
            foreach (KeyValuePair<IEntity, int> kv in all_entities) {
                IEntity e = kv.Key;
                if (e.get_obj_ID_num() == id) {
                    return e;
                }
            }
            //return null if not found
            return null;
        }

        public void Delete(IEntity e)
        {
            entities.Remove(e);
        }

        public void Delete_Hard(IEntity e) {
            keys.Remove(e);
            entities.Remove(e);
            all_entities.Remove(e);
        }

        public void Clear() {
            keys.Clear();
            entities.Clear();
            all_entities.Clear();
        }

        public void AddRange(RenderList new_render_list) {
            //pull all entities from new render list (save to variable to save calling the function every time the loop runs)
            Dictionary<IEntity, int> new_all_entities = new_render_list.get_all_entities();
            foreach (KeyValuePair<IEntity, int> kv in new_all_entities) {
                IEntity e = kv.Key;
                //add all entities to this render list
                Add(e);
            }
        }
        
        public void sort_list()
        {
            //Sort the list off of the Y position of the objects within the list
            entities.Sort((p, q) => p.get_base_position().Y.CompareTo(q.get_base_position().Y));
        }

        public void sort_list_by_depth(float rotation, Vector2 center_position, float render_distance)
        {
            //manage moving things in and out of the sorted list to increase performance
            //iterate over keys and modify dictionary
            foreach (IEntity e in keys) {
                if (Vector2.Distance(center_position, e.get_base_position()) <= render_distance) {
                    if (!entities.Contains(e)) {
                        Add(e);
                    }
                } else {
                    all_entities[e] = 0;
                    if (entities.Contains(e)) {
                        Delete(e);
                    }
                }
            }


            //Sort the list off of the Y position of the objects within the list
            entities.Sort((p, q) => p.get_depth(rotation).CompareTo(q.get_depth(rotation)));
        }

        public void print_entities()
        {
            //sort_list();
            for (int i = 0; i < entities.Count; i++)
            {
                Console.WriteLine("Entity: " + i + ", position: " + entities[i].get_base_position().ToString());
            }
            Console.WriteLine("\n");
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 focus_position, float render_distance)
        {
            Constant.profiler.start("renderlist_draw");
            for (int i = 0; i < entities.Count; i++)
            {
                if (Vector2.Distance(entities[i].get_base_position(), focus_position) < render_distance){
                    entities[i].Draw(spriteBatch);
                }
            }
            Constant.profiler.end("renderlist_draw");
        }
    }
}