# ShadowFix
## Megabonk mod which enables Unity's dynamic shadows in-game
### Why?
Megabonk has dynamic shadow quality settings in the menu:

<img width="1000" height="126" alt="imagen" src="https://github.com/user-attachments/assets/a238c622-f243-4455-8643-50f8e9f98e40" />

Those settings work just fine for the shadows in the main menu:

<img width="345" height="465" alt="imagen" src="https://github.com/user-attachments/assets/83e7b993-9339-4963-a8ed-77b598234dd6" />

However, there are no shadows in-game!

<img width="704" height="511" alt="imagen" src="https://github.com/user-attachments/assets/3dfa1911-10cc-451a-92c5-ca93db346e17" />

This could have been an artistic decision or maybe something done for better performance, but with this mod at least you have a choice.

<img width="1093" height="598" alt="imagen" src="https://github.com/user-attachments/assets/f32a5687-f652-4a74-93de-92800b37ec2e" />

## Mod settings
The mod has 5 settings in its config file (located at (GameDirectory)/BepInEx/config/ShadowFix.cfg)

#### DisableBlobShadow (DEFAULT: True)
Disable the player circle (blob) shadow.

#### EnablePlayerShadow (DEFAULT: True)
Enable dynamic shadows for the player.

Note: You can have both of them on or off at the same time without issues.

#### TwoSidedShadows (DEFAULT: True)
Makes it so scene objects have two-sided shadow rendering. Fixes some shadow artifacts mostly present on Desert houses.

#### EnableNPCShadows (DEFAULT: False)
Enable shadows for enemies. Disabled by default because it has a few small visual artifacts when enemies die and it's not recommended for low end devices.

#### ShadowDarkness (DEFAULT: 0.8)
Darkness of the dynamic shadows. 


