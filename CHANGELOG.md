# Changelog

## [2.2.2] - 11-06-23

- Fixed remote clients infinitely loading new video in some cases if they were already loading a video

## [2.2.1] - 10-10-23

- Added _SetTextureOverride, _GetTextureOverride, _GetResolvedTextureOverride to ScreenManager
- Set minimum CommonTXL version to 1.0.3 (hopefully get around VCC version resolution issues)

## [2.2.0] - 10-04-23

- Added support for multiple CRT definitions
- Changed how CRTs are listed, surfacing more important details
- Added warning if any texture overrides reference non-default textures in the Packages directory
- When a CRT definition is first added, a new CRT and material will be copied into the Assets folder
- If an active placeholder texture is a RenderTexture, CRTs will remain in realtime update mode
- Hid the Aspect Ratio override field on the txl shaders
- Fixed editor texture not being applied to surfaces in editor
- Added Resize to Video option for CRTs, causing them to change their resolution to match the underlying video when it loads
- Added Expand to Fit option for CRTs, modifying the resize option to expand the CRT to account for letterboxing at the target aspect ratio
- Video Source script will display errors in inspector if unity video player component properties are modified (excluding max resolution)
- Added support for AudioLink 1.0 media state API
- Added event to Audio Manager when AudioLink binding changes
- Added Fade Delay option to Audio Fade Zone to smooth out changes in volume

## [2.1.8] - 10-01-23

- Fixed duplicate Screen Manager component on "other" video player prefabs
- Fixed unlinked Audio Manager reference on "other" video player prefabs
- Video player throws explicit error when trying to play AVPro in simulator
- Fixed playlist trying to start video twice when auto-advancing

## [2.1.7] - 09-27-23

- Fixed upgrade bug from pre-2.1.0 that could prevent correct binding to video and audio managers
- Fixed editor error when sync audio profiles with no unity source defined in profile template
- OptionsUI component can automatically detect parent video player / audio manager
- Remove hidden button from lower-right corner of trackbar to cycle video mode

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
