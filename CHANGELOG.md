# Changelog

## [2.1.7] - 09-27-23

- Fixed upgrade bug from pre-2.1.0 that could prevent correct binding to video and audio managers

## [2.1.6] - 09-23-23

- Fixed USharp upgrade / prefab data loss when first selecting the PlayerControls object on prefab

## [2.1.5] - 09-23-23

- Fixed regression from 2.1.3 causing video restart when using playlist with immediate off and player joins world
- Fixed shuffled playlists not syncing correctly across users
- Fixed render out not working if no material overrides set in screen manager
- Add remsume after load option to playlist to determine if playlist is resumed after manual URL is done playing

## [2.1.4] - 09-22-23

- Add trace logging option to SyncPlayer for future debugging support
- Removed UdonSharp dependency

## [2.1.3] - 09-20-23

- Fixed audio fade zone init race that could cause volume to not take effect on start
- Added immediate option to playlist to control interrupting existing track when loading new list

## [2.1.2] - 09-18-23

- Fixed currently active playlist entry not being selectable, even if video is stopped
- Fixed playlist UI not honoring video player lock
- Fixed audio fade zone not setting volume at start if lower bound set to 0

## [2.1.1] - 09-14-23

- Fixed local video player not picking up error event from video manager

## [2.1.0] - 08-04-23

- Added default-on option to sync video player to run build hooks
- Moved sync video player sync options under an "Advanced" sub-section.
- Removed reference to Video Manager and Audio Manager from Sync Video Player
- Video Manager and Audio Manager objects register themselves with a video player on startup
- Updated sync video player integrity check to look for none or duplicate managers referencing it
- Added menu items to GameObject menu to add different VideoTXL objects to scene
- Added repair prefab instance tool to fix some classes of problems
- Added URL Remapper prefab
- Added support for audio channels 7 and 8 to AVPro template
- Fixed playlist UI from 2.0.0 release
- Added playlist load button prefab

## [2.0.0] - 07-29-23

- First post-VPM release
