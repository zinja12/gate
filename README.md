# README

Untitled Narrative Driven Top Down Game

## Controls
### Keyboard
- Y - open text / interact
- WASD - move
- SPACE - Dash, advance text
- H - Attack
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
- Left Trigger - Aim Bow
- Right Trigger - Charge/Fire Arrow

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
Game world is rendered to a texture and then drawn to a render target which is then drawn to the screen via `_spriteBatch`. Every time the screen is updated in size the render target is recreated and the world viewport is updated. On each draw call the render target is set on the graphics device, the texture for the world is drawn, and the render target is then dropped from the graphics device. Then during the regular draw calls that render target is drawn to the screen via spriteBatch.