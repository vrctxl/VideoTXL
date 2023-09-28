Video Player Prefabs
--------------------

The default prefab is "Sync Video Player"

Additional prefabs are broken out in the following folders:

Audio Groups
  Preconfigured audio profiles

Component
  Optional components like playback zones, playlists, URL remappers, etc.

Other Video Players
  Other configurations of the Sync Video Player and Local Video Players

UI
  UI control objects

Video Sources
  Preconfigured video sources

Adding Prefabs to Scene
-----------------------

The recommended way to add prefabs to your scene is to use the 
GameObject->TXL menu.

Video player prefabs added through this menu save a couple extra steps. 
And if any part of a video player hierarchy is selected, most of the 
menu items to add components, UI, sources, etc. become context-aware 
and will automatically wire themselves up to the selected video player.