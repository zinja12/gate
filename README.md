# README

Untitled Narrative Driven Top Down Game
Codename: The Gate

## GIFS
![](https://github.com/zinja12/gate/blob/main/gifs/gategif1_f.gif)

## Controls
### Keyboard
- Y - open text / interact
- WASD - move
- SPACE - Dash, advance text
- V - Attack
- N - Heavy Attack / Charged Heavy Attack
- J - Aim Bow
- K - Charge/Fire Arrow
- Right/Left Arrow Keys - Rotate Camera
- Up/Down Arrow Keys - Zoom

### GamePad
- Y - open text / interact
- Left Joystick - Move
- Right Joystick - Rotate Camera
- A - Dash, advance text
- X - Attack
- Right Trigger - Heavy Attack / Charged Heavy Attack
- Left Trigger - Aim Bow
- B - Charge/Fire Arrow

### UI Notes
- Black - Dash Charges
- White - Attack Charges
- Red - Health
- Blue - Arrow Charges

### Editor
- E - enter and exit editor
- I - switch item left
- O - switch item right
- P - print all objects in level
- { (open-bracket) - rotate selected object left
- } (close-bracked) - rotated selected object right
- Ctrl+S - save current level file
    - NOTE: this will save the player where they are in the editor mode
- WASD - move player to move camera
- Left Mouse Click - place object
- 1 - rotate selected editor tool left
- 2 - rotate selected editor tool right
- 3 - rotate selected editor layer left
- 4 - rotate selected editor layer right

Note: Mouse right clicking on condition objects activates their debug tool buttons to connect and disconnect entities to and from the condition

Note: Right click when in delete mode to delete objects

## Editor Tools
- 0 - Placement Tool
- 1 - Gameplay Condition Edit Tool
- 2 - Delete Tool

## Screen Rendering
The game world is rendered to a texture and then drawn to the screen. This is achieved via the canvas class capturing the `_spriteBatch` calls by rendering them to a render target and then passing that to be drawn to the screen. Every time the screen is updated in size the dimensions of the screen are reset and the action is captured so that we can calculate the best fit for the render target in the Canvas.cs class. The viewport for the world does not need to be updated, only the dimensions and fit + position of the render target being drawn to the screen.

## Sprite Stacking
The generalized concept for certain world (geometry) objects in the game is sprite stacking. Meaning that the game is fully 2D, however the impression of certain 3D elements is given due to how the objects are being drawn. The high-level overview of the concept is that of a 3D-printer where the objects are drawn layer by layer in order to create a 3D shape. Another analogy for this is a CT-scan where the slices are imaged and then drawn together in order to show the full 3D object. The sprites are all saved in a spritesheet and then the code, with given parameters for each individual sheet is able to slice the sheet and then display each slice as a layer in the final object. When rotated together with the world/camera the impression of a 3D object is given. When this technique is combined with a rotated collision rectangle at the first (base) layer of the sprite stack it gives the impression of an isometric 3D game, but with a stylized approach.

## Level Transitions
Level transitions were a tricky one to handle. The first implementation tried to use rendertarget2d in order to capture what was happening on either side of a level load. However, there was nuance with how the render targets would capture the sprties that were being drawn at any one time. The easiest and probably most efficient way to handle the transition is to simply fill the whole screen with a black texture. This is procedurally done in the draw function depending on the level transition variables that are set and modified when the level transitions. Essentially the black texture just fades in over a certain period of time, then fades back out over the same period of time. When the transition is fully faded to black the loading of the next level happens to avoid/hide any hiccups in frame rate. The current transition time is 4 seconds(2000 milliseconds per fade), though this could be modified in the future.

## Sway (Wind) for Stacked Objects
Stacked Objects are just representations for sprite stacks. They are a generic version with most of the functionality that is necessary and all the parameters that are needed to distinguish one object from another. Wind and sway is an effect that happens on top of that where the stack is divided into three segments and each of the segments, based on a timer, have their draw_positions offset by a pre-set amount. This is also done in a random nature by starting the sway timers at different values in order to make the effect look more natural in engine with objects that do not sway the same as the wind travels through them. This can be activated by using the set_sway() function in the stacked object class after initialization.