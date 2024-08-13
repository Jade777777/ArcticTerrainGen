# ArcticTerrainGen

This program generates a mountainous arctic landscape. An arctic landscape seemed like it would be an effective choice for my method of procedural generation due to the distinct layers of various surfaces. Water, various types of ice, Vegetation,  stone, and snow cover textures are all layered together based off of the height of the perlin noise along with the slopes of each individual quad. The actual height of the terrain is broken up into separate regions from the textures. These regions map the perlin noise onto customizable animation curves to allow the generation and fine tuning of various terrain features. Using this technique I created flat regions for water, raised regions around the water for ice build up, flat rolling regions for a forested area, along with tall mountains sticking out of the landscape. The animation curves being exposed in the editor allowed me to iterate over ideas quickly, allowing me to fine tune my terrain until it felt just right.

I sourced all of the models from this project from Kenney. The first objects that I added to the terrain were boats and shipwrecks. I felt that the water being static and opaque needed something else to sell the idea that it was water and adding the boats helps clarify that. Along with the boats I also found a shipwreck from the same source. Shipwrecks were placed specifically on the ice to accentuate the difference and provide a little history. In the center of large bodies of water I placed some rock formations as well.  I used densely placed trees to create a forested region as well. Trees covered with snow and rocks are scattered across the mountains to give them some more depth. Light blue fog at long distances  along with a bright and cloudy skybox is used to give the terrain a frosty look.

I created a UV map with 16 textures from the Dokucraft Dwarven texture pack for minecraft, and about half of them are used across the terrain. The terrain itself is broken into a 15 by 15 grid of chunks each 100 by 100 meters in size. When in the center this covers the distance to the horizon.  A total of 8 UVs and 6 models were used for this environment.

The first step I would take to expand this environment into a game would be to add collision to the terrain. As it is now the terrain is only visible so if it was incorporated into the game it would need to be used as a sort of background. Changing how the bands of terrain are determined from regions to ordered sizes would also allow quicker iteration when generating new terrain. This would also allow for bands and terrain features to be mixed procedurally as well making it easier to create a large variety of terrains. I would also like to add some sort of masking feature. Right now textures can be determined based off of slope, but layering perlin noise, and potentially other types of noise could result in some cool effects.  It’s important for me to remember to keep the generation tools easy to work with. The strength of procedural generation is that it allows us to create a lot of content. If the iteration times become too high it will be difficult to create assets of acceptable quality.

Textures:
https://dokucraft.co.uk/resource-packs/dwarven

Assets:
https://www.kenney.nl/assets/survival-kit
https://www.kenney.nl/assets/holiday-kit
https://www.kenney.nl/assets/pirate-kit

Skybox:
https://assetstore.unity.com/packages/2d/textures-materials/sky/free-stylized-skybox-212257
