Video Prefabs (what should I use??)

SyncPlayer
-----------------------------------
The main video player asset with all the features and lets players enter URLs
to play for everyone in the room.  Use this when you need a general-purpose
video player.

BasicSyncPlayer
-----------------------------------
A heavily simplified version of the sync player, only two scripts with no other
dependencies.  This is the same script as the AudioLink Mini Player.  Use this
if you specifically want a minimal player or want to keep a minimum amount
of scripts.

LocalPlayer Unity
-----------------------------------
A local-only video player based on the Unity Video Player asset.  Good for
playing youtube but won't play streams.  No UI by default, but can be hooked
into some of the assets that use the data proxy.  Use this when you want
to let players trigger videos on demand not synced to other players, such as
in a gallery.

LocalPlayer AVPro
-----------------------------------
A local-only video player based on the AVPro asset.  Can play any video,
particularly good for streams.  No UI by default, but can be hooked into some
of the assets that use the data proxy.  Use this for event worlds when you
plan on streaming a single burned-in URL.  Streams naturally sync on their
own and this asset has no additional network sync that could be a central
point of failure during an event.

Additional prefabs under Prefabs/
-----------------------------------
* SyncPlayer Base 
	The base prefab that other SyncPlayer variants are derived from.
	Has no video sources or audio groups included by default.
* SyncPlayer Full
	A variation of SyncPlayer that has more video sources included to
	support multiple resolutions and latency modes.  Also includes multiple
	audio profiles.
* LocalPlayer Base
	The base prefab that other LocalPlayer variants are derived from.
	Has no video sources or audio groups included by default.