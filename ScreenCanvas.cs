using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using gate.Core;

namespace gate
{
    public class ScreenCanvas
    {
        private GraphicsDevice graphics_device;

        private RenderTarget2D world_render_target;
        private int base_width = 640;
        private int base_height = 360;
        private Color back_buffer_color;

        private List<Effect> postprocessing_effects;
        private RenderTarget2D render_target1, intermediate_target1, second_render_target;

        private World world;

        public ScreenCanvas(GraphicsDevice graphics_device, int base_width, int base_height, Color back_buffer_color) {
            this.graphics_device = graphics_device;

            this.base_width = base_width;
            this.base_height = base_height;
            this.back_buffer_color = back_buffer_color;

            this.world_render_target = new RenderTarget2D(this.graphics_device, this.base_width+1, this.base_height+1);
            this.render_target1 = new RenderTarget2D(this.graphics_device, this.base_width+1, this.base_height+1);
            this.intermediate_target1 = new RenderTarget2D(this.graphics_device, this.base_width+1, this.base_height+1);
            this.second_render_target = new RenderTarget2D(this.graphics_device, this.base_width+1, this.base_height+1);

            this.postprocessing_effects = new List<Effect>();
        }

        public void set_world(World world) {
            this.world = world;
        }

        public RenderTarget2D get_render_target() {
            return this.world_render_target;
        }

        public int get_base_width() {
            return this.base_width;
        }

        public int get_base_height() {
            return this.base_height;
        }

        public void add_postprocessing_effect(Effect effect) {
            postprocessing_effects.Add(effect);
        }
    
        public void clear_postprocessing_effects() {
            postprocessing_effects.Clear();
        }

        public void reset(int base_width, int base_height, Color back_buffer_color) {
            this.base_width = base_width;
            this.base_height = base_height;
            this.back_buffer_color = back_buffer_color;

            this.world_render_target = new RenderTarget2D(this.graphics_device, this.base_width+1, this.base_height+1);
            this.render_target1 = new RenderTarget2D(this.graphics_device, this.base_width+1, this.base_height+1);
            this.intermediate_target1 = new RenderTarget2D(this.graphics_device, this.base_width+1, this.base_height+1);
            this.second_render_target = new RenderTarget2D(this.graphics_device, this.base_width+1, this.base_height+1);
        }

        public void Draw(SpriteBatch spriteBatch) {
            //**************************************************************************
            //DRAW WORLD TO RENDER TARGET
            //**************************************************************************
            graphics_device.SetRenderTarget(world_render_target);
            graphics_device.Clear(back_buffer_color);
            
            //draw world
            world.draw_floor_background_entities(spriteBatch);

            // reset render target to finalize and draw to screen
            graphics_device.SetRenderTarget(null);

            //generate and pull light target from world
            world.draw_lights_to_render_target(spriteBatch);

            intermediate_target1 = new RenderTarget2D(this.graphics_device, this.base_width+1, this.base_height+1);
            graphics_device.SetRenderTarget(intermediate_target1);
            graphics_device.Clear(Color.Transparent);
            //draw floor render target
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp);
            spriteBatch.Draw(world_render_target, Vector2.Zero, Color.White);
            spriteBatch.End();
            Constant.c_light_effect.Parameters["SceneTexture"].SetValue(world_render_target);
            //draw lights render target
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, effect: Constant.c_light_effect);
            if (world.lights_enabled) {
                spriteBatch.Draw(world.light_map_target, Vector2.Zero, Color.White * 0.3f);
            }
            spriteBatch.End();
            world.draw_object_entities(spriteBatch);
            //draw world objects on top
            
            world.draw_transitions_and_intro_text(spriteBatch);

            graphics_device.SetRenderTarget(null);

            //**************************************************************************
            //PING PONG RENDER TARGET BUFFERS TO APPLY SHADERS
            //**************************************************************************
            RenderTarget2D first_render_target = intermediate_target1;

            foreach(Effect e in postprocessing_effects) {
                graphics_device.SetRenderTarget(second_render_target);
                graphics_device.Clear(Color.Transparent);

                spriteBatch.Begin(effect: e);
                spriteBatch.Draw(first_render_target, Vector2.Zero, Color.White);
                spriteBatch.End();

                (first_render_target, second_render_target) = (second_render_target, first_render_target);
            }

            graphics_device.SetRenderTarget(null);
            graphics_device.Clear(Color.Black);

            //**************************************************************************
            //DRAW RENDER TARGET TO SCREEN
            //**************************************************************************
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

            float scale = Constant.get_res_scale(graphics_device);

            Vector2 position = new Vector2(
                (graphics_device.Viewport.Width - (base_width * scale)) / 2, 
                (graphics_device.Viewport.Height - (base_height * scale)) / 2
            );

            // Draw the scaled render target
            spriteBatch.Draw(first_render_target, position, null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            spriteBatch.End();

            //**************************************************************************
            //DRAW TEXTBOXES TO SCREEN AFTER SHADERS
            //**************************************************************************

            //draw textboxes
            world.draw_textbox(spriteBatch, scale);
        }
    }
}