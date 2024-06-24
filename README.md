# README

Untitled Narrative Driven Top Down Game
Codename: The Gate

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

Note: Mouse right clicking on condition objects activates their debug tool buttons to connect and disconnect entities to and from the condition

## Screen Rendering
The game world is rendered to a texture and then drawn to the screen. This is achieved via the canvas class capturing the `_spriteBatch` calls by rendering them to a render target and then passing that to be drawn to the screen. Every time the screen is updated in size the dimensions of the screen are reset and the action is captured so that we can calculate the best fit for the render target in the Canvas.cs class. The viewport for the world does not need to be updated, only the dimensions and fit + position of the render target being drawn to the screen.

## Sprite Stacking
The generalized concept for certain world (geometry) objects in the game is sprite stacking. Meaning that the game is fully 2D, however the impression of certain 3D elements is given due to how the objects are being drawn. The high-level overview of the concept is that of a 3D-printer where the objects are drawn layer by layer in order to create a 3D shape. Another analogy for this is a CT-scan where the slices are imaged and then drawn together in order to show the full 3D object. The sprites are all saved in a spritesheet and then the code, with given parameters for each individual sheet is able to slice the sheet and then display each slice as a layer in the final object. When rotated together with the world/camera the impression of a 3D object is given. When this technique is combined with a rotated collision rectangle at the first (base) layer of the sprite stack it gives the impression of an isometric 3D game, but with a stylized approach.