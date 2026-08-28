# Creating Level

- Start with the template in New Scene popup
- The template have a basic ground and a spawner added
- Under LevelRoot there is 3 layer root : Background, Normal and Foreground.
- each Layer root have a bunch of tilemap as children you can paint for diverse functions (ground will be walkable, spike will hurt etc.)
- Place objects that are in a specific layer under one of the tilemap of that layer so collision works as expected (e.g. the start spawner is
placed under the Normal_ground tilemap)
- When in doubt, check how things are done in the Demo Level

# Collectible

In order to create a new custom collectible :

- Add a Trigger collider to the object
- Add a CollectibleItem Component to the object
- Set the Collectible 

_Note_: if you want to have a physic based object (e.g. like the coin collectible)
you will need have 2 collider on your object : a non trigger one that will allow the
object to be physically interacting with the scene *and** a second one that is trigger
slightly bigger to ensure the character bumping into the collectible will hit the trigger.
**Check the CoinPhysic prefab for an example**