Bunny Blitz is a Unity sample demonstrating the 3D as 2D workflow. It shows the new 3D capabilities in the 2D renderer first released in Unity 6.3 and how 3D objects can blend with 2D sprites, sorted, lit, and masked with the 2D tools and URP 2D Renderer.

Bunny Blitz is a classic 2D platformer featuring a lively 3D character moving at fast speed through a 2D environment, all the 3D and 2D elements are cohesively illuminated with 2D lights and use out-of-the box components to make all play out together, it’s made in Unity 6.5

**The techniques and features that you’ll see in the sample include using:**

* 3D objects using the Sorting Group component and 3D as 2D to be sorted among sprites through sorting layer instead of depth (typical in 3D projects)  
* A perspective camera and elements arranged on the Z-axis to give the environment the illusion of depth  
* A 2D custom injection pass (introduced in Unity 6.5) to blur the game play layers that are out of focus  
* Shader Graph sprite based shaders, with 3D as 2D compatibility enabled for 3D objects  
* The 2D toolset in action, 2D Lights, 2D Tilemaps, 2D Sprite Shape, 2D Animation, 2D mask  
* Advanced graphic techniques, like the shadow camera renderer for the rabbit’s projected shadow, matcap effect material or tileable tilemap materials  
* Advanced visual effects using VFX Graph and Shader Graph, that you will find throughout the level, watch for character spawning, cloud transportation, end of level portal or bouncing on spring effects, for example.  
* A big collection of reusable assets, 3D and 2D, featuring a fully animated main character, enemies, and a final boss “the megasnail”  
* Pure 2D physics driven gameplay 

You can expand the universe of Bunny Blitz by creating your own levels out of the scene template *(File \> New scene \> Template level scene)*, and adding them to the main title screen selection screen (LevelSelect scene and select LevelSelect GameObject). 

We are working on great companion content to the project, but for devs looking for upskilling in 2D, you can get the free e-book [2D game art, animation, and lighting for artists (Unity 6.3 LTS edition)](https://unity.com/resources/2d-game-art-animation-lighting-unity-6-3-lts)

**How to play Bunny Blitz** 

Open the LevelSelect scene in the BunnyBlitz/Game/Scenes folder, select level and press Play.

* Left/Right or A/D will move the Rabbit left and right  
* Space to jump, doble press to doble jump  
  * Land on enemies and blocks and avoid hazards like spikes or falls  
* Shoot “pomegranades” with Enter or Left click on the mouse

Collect golden carrots, every 100 you get a life.  
Collect “pomegranades” and shoot them at enemies.  
Watch for the hearts, if you lose 3 hearts you will respawn from a save point or restart from the level selection screen if you don’t have lives left.  
For the completionist:  
Collect the hidden glass ducks and the letters U,N,I,T,Y hidden throughout the demo level.  
Defeat the “megasnail” at the end of the level, he doesn’t like “pomegranades” but even less bombs.  
Return home, cross the portal in the carrot house.

**Note:** shaders compile in the first play-through, expect hiccups as they get ready the first time.

**Got questions? Write to us in the Discussions thread**  
[https://discussions.unity.com/t/bunny-blitz-2d-and-3d-sample-project-available-now/1731205](https://discussions.unity.com/t/bunny-blitz-2d-and-3d-sample-project-available-now/1731205)   
All the assets can be repurposed, added to your own projects.  