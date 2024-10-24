# README

Untitled Narrative Driven Top Down Game
Codename: The Gate

## GIFS
![](https://github.com/zinja12/gate/blob/main/gifs/gategif1_f.gif)
![](https://github.com/zinja12/gate/blob/main/gifs/gategif2_f.gif)

## Build + Info
This project is built using Monogame 3.8.0. The build SDK is .NET 6.0.302, however the target is .NET Core 3.1. In order to run the build .NET Core 3.1 is required, however Monogame itself is packaged alongside the .exe in the bin/Debug/netcoreapp3.1 directory so that Monogame itself is not necessary to install before being able to run the build. In order to run the project, just run the gate.exe in the bin/Debug/netcoreapp3.1 directory. This should not be necessary for the Release folder however as that version will have the .NET Core 3.1 target binaries necessary for dependencies packaged alongside the .exe so no installs will be needed aside from the Release folder itself.

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

#### Editor Tools
- 0 - Placement Tool
- 1 - Gameplay Condition Edit Tool
- 2 - Delete Tool

## Screen Rendering
The game world is rendered to a texture and then drawn to the screen. This is achieved via the canvas class capturing the `_spriteBatch` calls by rendering them to a render target and then passing that to be drawn to the screen. Every time the screen is updated in size the dimensions of the screen are reset and the action is captured so that we can calculate the best fit for the render target in the Canvas.cs class. The viewport for the world does not need to be updated, only the dimensions and fit + position of the render target being drawn to the screen.

## Collision
Collision is quite a complex topic. Firstly, collision needs to work with rotated rectangles, meaning from every potential angle between 0 and 360 we need to be able to detect collisions. The solve to this is the Separating Axis Theorem of collision detection. Essentially given all the edges of the two polygons we loop through all edges and project the points onto normals (predefined axis) of every edge and check for any separation. If there is not separation, that means there is a collision. If there is separation, that means no collision on this axis (still need to check the rest). There are papers written that explain it better than this, so please refer to the internet for the full explanation and implementation. In terms of collision resolution, the handling of the cases boils down to basically detecting the collision one frame earlier than usual by calculating a future hurtbox for the character and if that intersects with any of the collision geometry then we essentially just do not move the character on that frame. Games like Celeste use this approach and if it's good enough for them it is probably fine here lol.

## Sprite Stacking
The generalized concept for certain world (geometry) objects in the game is sprite stacking. Meaning that the game is fully 2D, however the impression of certain 3D elements is given due to how the objects are being drawn. The high-level overview of the concept is that of a 3D-printer where the objects are drawn layer by layer in order to create a 3D shape. Another analogy for this is a CT-scan where the slices are imaged and then drawn together in order to show the full 3D object. The sprites are all saved in a spritesheet and then the code, with given parameters for each individual sheet is able to slice the sheet and then display each slice as a layer in the final object. When rotated together with the world/camera the impression of a 3D object is given. When this technique is combined with a rotated collision rectangle at the first (base) layer of the sprite stack it gives the impression of an isometric 3D game, but with a stylized approach.

## Level Transitions
Level transitions were a tricky one to handle. The first implementation tried to use rendertarget2d in order to capture what was happening on either side of a level load. However, there was nuance with how the render targets would capture the sprties that were being drawn at any one time. The easiest and probably most efficient way to handle the transition is to simply fill the whole screen with a black texture. This is procedurally done in the draw function depending on the level transition variables that are set and modified when the level transitions. Essentially the black texture just fades in over a certain period of time, then fades back out over the same period of time. When the transition is fully faded to black the loading of the next level happens to avoid/hide any hiccups in frame rate. The current transition time is 4 seconds(2000 milliseconds per fade), though this could be modified in the future.

## Sway (Wind) for Stacked Objects
Stacked Objects are just representations for sprite stacks. They are a generic version with most of the functionality that is necessary and all the parameters that are needed to distinguish one object from another. Wind and sway is an effect that happens on top of that where the stack is divided into three segments and each of the segments, based on a timer, have their draw_positions offset by a pre-set amount. This is also done in a random nature by starting the sway timers at different values in order to make the effect look more natural in engine with objects that do not sway the same as the wind travels through them. This can be activated by using the set_sway() function in the stacked object class after initialization.

## NPCs
NPC systems are built off the original nightmare class. It has been updated in certain ways in order to be the foundation for the NPC system. The bones of the system are the same where weights are calculated and assigned based on how incentivized different directions are. There is also some nuanced logic in the animation update function that controls the rubber-banding of animations when npcs are moving so that it just sticks to one animation rather than swapping back and forth jankily. Otherwise the system is fairly straight-forward in that it takes in the parameters given, has optionality of swapping behavior modes externally in order to control behavior and simply executes the logic for those states. Definitely some room for improvement in terms of the logic handling the different AI states and such, but for a first pass it is probably decent enough.

## Dialogue
Dialogue is very similar to how text works for signs, however one difference is that the text is read out of a different file that is included on the game world object for NPCs. This way we can control NPC speaking through including a reference to that file in the GameWorldObject that is being saved. This file for dialogue will also probably change in order to reflect the different actions that the character has taken throughtout the game. Can also probably include some light scripting at some point in order to process and decide what dialogue to display at what times during gameplay. It's a start for now though and the system is flexible enough (through them being conversation files rather than just a characters speaking lines). They should be able to be modified to have back and forth conversation with the player.