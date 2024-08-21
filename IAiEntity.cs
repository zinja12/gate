using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace gate
{
    public interface IAiEntity
    {
        float[] assign_weights(Vector2[] movement_directions, IEntity goal_entity, bool ignore_circling);
        int select_best_weight(float[] weights);
        void update_movement(float rotation);
        void set_ai_entities(List<IAiEntity> enemies);
        int get_ID_num();
        void set_behavior_enabled(bool value);
        Emotion get_emotion_trait();
    }
}