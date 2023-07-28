Playlist Component
-----------------------------------------------------------
Playlist is an optional component for SyncPlayer that allows a static list of
tracks to be defined in advance and played in sequence.  Adding a playlist to
SyncPlayer will enable the playlist controls on the associated Player Controls
UI object.

SyncPlayer's Player Controls UI already has a playlist UI embedded that can be
toggled by the dedicated list button between repeat and info.  More playlist
UIs can be added to the world, located under Texel/Video/UI/Playlist.  All UI
objects assigned to the same SyncPlayer will stay in sync with each other.

==================
/!\ IMPORTANT /!\
==================

Playlist UI uses an internal prefab under Texel/Video/UI/Playlist/Internal
called Playlist Entry, which it instantiates at runtime.  Due to
shortcomings mixing U# and prefabs, this prefab may be corrupt on import.
You'll know there's a problem if the list is not populated correctly and
you see messages about 'Unable to load program' in your console.

Try the following to resolve the issue:

1. From the menu, run VRChat SDK > Utilites > Re-compile all program sources

2. If the above does not resolve the problem:
  a. Open the Playlist Entry prefab
  b. Create a new Udon Behavior component with the PlaylistUIEntry asset
  c. Copy all the values from the old udon component to the new one
  d. Delete the old udon component
  e. Reassign the script in the Button object in the prefab's hierarchy
    i. Drag the root Playlist Entry node to the missing On Click reference
    ii. Set the action to SendCustomEvent

==================

Playlist Data Component
-----------------------------------------------------------
Playlist Data is the actual list of tracks and titles that make up a playlist.
A single data object is included in the Playlist prefab already.

Playlist Data can be programatically swapped out from the Playlist at runtime
by calling the _LoadData method on Playlist.  This must be run on all clients.

Playlist Catalogue Component
-----------------------------------------------------------
Playlist Catalogue is a way to manage multiple playlist data and synchronize
the active selection across all players.  Assign a reference of each playlist
data that should be part of the catalogue to the component.

Call the functions _LoadFromCatalogueData or _LoadFromCatalogueIndex on the
Playlist object instead of the _LoadData function.  The correct playlist data
will be synced and loaded for all players.
