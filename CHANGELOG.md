# Changelog

## [2.5.0] - 02-25-25

### [2.5.0-beta.18]
- Added AddSource and RemoveSource methods to SourceManager
- Added EVENT_SOURCE_ADDED and EVENT_SOURCE_REMOVED methods to SourceManager

### [2.5.0-beta.17]
- Requires CommonTXL 1.8.0 or later
- Added exclusion zones property to SyncVideoPlayer
- Added LocalPlaybackEnabled property to SyncVideoPlayer to control local playback
- Added LocalPlaybackValid property to SyncVideoPlayer to check local playback

### [2.5.0-beta.16]
- Changed playback behavior so video keeps advancing if/while owner resyncs
- Fixed various edge cases around sync playback, pause, resync, and playback zones
- Fixed playback zones not handling pause case
- Fixed unneeded ownership transfer of video player from playlists and queues
- Fixed edge cases in how local offset ways applied
- Updated AudioLink media integration to support one-loop state
- Added Fake Mute option to audio channel

### [2.5.0-beta.15]
- Added youtube fallback options to Video Manager
- Added LocalOffset property to Sync Video Player API

### [2.5.0-beta.14]
- Fixed lock mute option in Audio Channel script
- Added 'Use Reserved Audio Source' option for AudioLink to audio profiles, overriding selected channel
- Added overall allow setting for queue add, priority and delete
- Added allow self delete option for queue entries
- Added optional ACL for queue add permission
- Added add from proxy option to queue
- Fixed resolved title data in queue not propagating to remote clients
- Fixed video mode setting not updating on player controls UI on remote clients
- Added world persist option for volume/mute settings (CompuGeniusCode)

### [2.5.0-beta.13]
- Added helper controls to several inspectors to create and link new components
- Added additional safety checks in sync player controls (CompuGeniusCode)
- Added public VideoPlayer getter to ScreenManager
- Added VRC Logging option to Playlist
- Improved efficiency of debug logging within main sync player, video manager, playlist
- Improved efficiency of URL Remapper for large number of rules
- Added retry count to loading bar after first load fail

### [2.5.0-beta.12]
- Removed invalid rootNamespace entry from asmdefs
- Added quest URL support to queues
- Added methods to get current title and player from queues
- Added retry support/properties to base URL source

### [2.5.0-beta.11]
- Fixed trace crash in VideoManager from beta 10
- Hid TXLVideoPlayer field from SourceManager inspector
- Obsoleted Zone Membership for SyncPlayer playback zones, replaced with Tracked Zone Triggers
- Fixed array bounds error in Screen Manager Inspector that shows up in certain edge cases
- Added proper undo/persistence to GameObject->TXL menu actions
- Fixed crash in SyncPlayer if state debugging is used and URL is changed with a null Quest URL
- Added error retry options to URL sources (Playlist, Queue)

### [2.5.0-beta.10]
- Added "trace" debug option to Video Manager

### [2.5.0-beta.9]
- Fixed oopsie with class name

### [2.5.0-beta.8]
- Added Input Proxy component to enable smooth integration with prefabs like Youtube Search
- Input Proxy has one-button integration with Youtube Search prefab if it exists in scene
- Fixed Screen Manager events not being public
- Added several events to Screen Manager related to texture or resolution change
- Added direct access to validated, raw captured texture in Screen Manager
- Added CurrentTextureIsError property to Screen Manager
- Added optional Image Download Manager field to logo download on Screen Manager
- Added simple "Load URL" button prefab that loads a static URL
- Fixed playlist "add to queue" function not respecting shuffle order

### [2.5.0-beta.7]
- Fixed local player not recognizing quest platform in URL remapper

### [2.5.0-beta.6]
- Fixed local player not working with all URL remapper rules
- Updated VRSL blit material to perform point filtering
- Included transparent screen shader

### [2.5.0-beta.5]

- Fixed queue input not accepting non-HTTP URLs
- Default enter video text indicates queue if queue entry is active
- Added explicit VRSL mode select to screen manager
- Added toggle and interval for checking capture source to screen manager
- Force-enable audiolink when audiolink is bound to video player
- AVPro reserved audio source is muted by default
- Stream source and UI progress, but not ready for full use

### [2.5.0-beta.4]

- Fixed video source UI auomatically selecting active playing source (again)
- Added highlight for button of selected source
- Screen Manager performance improvements

### [2.5.0-beta.3]

- Audio Manager now uses ReservedAudioSource for AudioLink as last resort
- Audio profiles updated to select explicit AudioLink source for AVPro
- Screen manager will temporarily unregister its listeners when disabled
- Playlist exposes ListChangeSerial and TrackChangeSerial properties
- Added additional URLs list to Stream Source
- Added synced custom URL to Stream Source
- Player Controls (for SyncPlayer) will temporarily unregister its listeners when disabled
- Playlist UI and Playlist Queue UI will temporarily unregister its listeners when disabled
- Video Source UI will automatically select the active playing source when opened

### [2.5.0-beta.2]

- Fixed errors in several TXL GameObject menu add entries
- Added option to player controls to set default URL entry mode (queue vs. normal)
- Added option to player controls to remember URL entry mode (queue vs. normal)
- Added option for URL sources to override video display with the logo image
- Updated logic on how different sources take precedence or interrupt
- Added EVENT_INTERRUPT event to VideoUrlSource when a source wants to interrupt current playback
- Added stream URL source with extra rules for error handling or fallback behavior, still missing UI

### [2.5.0-beta.1]

- BREAKING: Removed URL Source (Playlist, Queue, Custom) from SyncPlayer
- BREAKING: "PlaylistUI" UI prefab has been removed and deleted from "PlayerControls" prefab
- BREAKING: "Video Source UI" prefab has been added to PlayerControls as replacement for old PlaylistUI
- Added Source Manager component, which can manage multiple sources (playlists, queues, custom)
- Added Playlist Queue URL source
- Added URL Info Resolver component, which can resolve title and author info when loading a YouTube URL 
- SyncPlayer prefab is updated with a Source Manager added by default with a Queue source
- Removed legacy single-track queue from SyncPlayer, which has not worked since the VRChat keyboard update
- Player Controls UI includes an "add to queue" toggle when pressing the [+] change URL button, if a queue is present on an attached source manager
- Player Controls UI includes a "title" field on the upper part of the display area
- Player Controls UI will show "QUEUE" or "PLAYLIST" in the lower left if a URL is from those sources, replacing the legacy "QUEUED" behavior
- Playlist UI rows updated to show track number
- Playlist UI rows show an "add to queue" button if the backing playlist is associated with a target queue
- Default volume of AVPro "Reserved Audio Source" components changed from 0.01 to 0.001

## [2.4.15] - 01-07-25

- Added "YouTube Prefer Unity In Editor" option to Video Manager, enabled by default
- Partial fix for CRT showing wrong placeholder frame when double buffered and tri-filtered
- Added 8k upper bound on setting CRT resolution in screen manager inspector
- Added EVENT_POSTINIT_DONE event to Sync and Local video players

## [2.4.14] - 11-30-24

- Fixed regression in VRSL integration
- Fixed audio fade zone throwing udon error when using non-sphere colliders

## [2.4.13] - 11-06-24

- Fixed VRSL integration not positioning correctly in editor preview when scaled less than 1
- Fixed VRSL integration when RAW RTs are not at default dimensions

## [2.4.12] - 07-25-24

- Added VRSL version check and warning message in screen manager
- YouTube URLs now default to loading on AVPro/Stream video sources when video source is set to Auto

  NOTE: YouTube videos can no longer be played easily in the editor.  Consider finding other compatible sources
  for testing like direct MP4 files, or look into steps to incorporate an AVPro demo into your environment
  to be able to play back content on AVPro.

## [2.4.11] - 07-10-24

- Fixed Audio Fade Zone sometimes dropping audio when entering inner collider
- Detect simple spherical audio zones and convert to more efficient distance interpolation
- Fixed livestreams sometimes freezing at start for master/owner in SyncPlayer and BasicSyncPlayer
- Fixed VRSL integration initializing with 0 scale
- Fixed Screen Manager using first CRT's property map on all CRTs
- CRT double buffering now exposed as separate Unity and AVPro settings
- Added _GetVRSLDoubleBuffered, _SetVRSLDoubleBuffered, _GetCRTDoubleBuffered, _SetCRTDoubleBuffered API to Screen Manager

## [2.4.10] - 07-02-24

- Added VRSLEnabled property to screen manager to control at runtime

## [2.4.9] - 07-01-24

- Added direct VRSL integration in Screen Manager
- Fixed lock control message when on access control
- Fixed sustain zone continuing to play if leaving while loading

## [2.4.8] - 06-14-24

- Included YTDL editor resolver logs with "VideoTXL" instead of "USharpVideo"
- Reversed previous "ghost windows" fix as it is not a VideoTXL issue
- Added "Handle Stream End Event" option to Video Manager, allowing event to be ignored
- Added more safety checks to the options UI in case some object references are missing
- Added init checks to several ScreenManager public methods
- Added Target Aspect Ratio and Double Buffered properties to property map for CRT material shaders
- Exposed Double Buffered checkbox on CRT list
- Hid several CRT properties behind an Advanced Options checkbox by default
- Enabled double buffering by default on newly created CRTs
- Added single-button conversion from default screen setup to CRT setup

## [2.4.7] - 05-18-24

- Potential fix for "ghost" frozen editor windows sometimes appearing
- Write out remapped URL in log if remapping happens

## [2.4.6] - 05-02-24

- Fixed editor URL resolver to resolve correctly with latest yt-dlp update

## [2.4.5] - 04-22-24

- Added custom rule support to URL Remapper
- Fixed screen manager not updating double-buffered CRTs between certain state transitions
- Fixed stop button not appearing available when rery on error was enabled
- Fixed some sync player state not being initialized ahead of first deserialization
- Fixed playlist not network syncing if it wasn't shuffled

## [2.4.4] - 03-30-24

- Fixed VideoTex not holding onto editor texture in editor window
- Changed deault lower bound of audio fade zone from 0.01 to 0.1

## [2.4.3] - 03-28-24

- Min CommonTXL version 1.5.0
- Added DependentSource prefab and GameObject menu option
- Added event logging option to Video Manager
- Changed event logging behavior in SyncPlayer, AudioManager, ScreenManager

## [2.4.2] - 03-27-24

- Added DependentSource component for linking a Local Video Player to another video player
- Added optional Dependent Source field to Local Vidoe Player
- Added button to create new property map to global shader entries in screen manager inspector

## [2.4.1] - 03-25-24

- Fixed shared material-based screens flipped and incorrect gamma on Quest
- Fixed video source change not recognized by screen manager in some cases
- Added EVENT_BIND_VIDEOMANAGER, EVENT_UNBIND_VIDEOMANAGER, EVENT_VIDEO_SOURCE_CHANGE events to TXLVideoPlayer

## [2.4.0] - 03-24-24

- Min CommonTXL version 1.4.0
- Changed repeat into a tri-state option including "repeat one" to loop current track
- Added RepeatMode property (get/set) to TXLVideoPlayer, obsoletes repeatPlaylist field
- Added DebugState field to VideoManager and ScreenManager
- Added _SetDebugState method to SyncPlayer, VideoManager, AudioManager, ScreenManager
- Added _TriggerInternalAVSync method to SyncPlayer to toggle internal AV sync
- Fixes to pass through internal AV sync and loop settings to video sources
- Added logo screen texture to list of recognized packages textures
- Menu options to add video player prefabs now place them under selected object in hierarchy
- Added "Fill" screen fit option, which clips the smaller of horizontal or vertical overflow

## [2.3.2] - 02-17-24

- Fixed potential race in ScreenManager that could throw an error and stop the manager

## [2.3.1] - 02-12-24

- Fixed cast error in ScreenManager inspector window
- Changed looping behavior of unity sources in SyncPlayer to be seamless

## [2.3.0] - 02-04-24

- Overhaul of Screen Manager inspector
- Removed legacy Screen Manager options "Use Material Overrides", "Separate Playback Materials", "Editor Material"
- Added Screen Manager option "Latch Error State"
- Added Global Property Updates section to Screen Manager
- Updated VideoTXL/Unlit shader to match support of VideoTXL/RealtimeEmissiveGamma
- Cleaned up API interface of Screen Manager
- Fixed build hooks canceling if a video source was found without a video player being reachable
- Added URL Input repair to build hooks

## [2.2.7] - 01-29-24

- Fixed script upgrade warnings for Unity 2022
- Fixed ScreenManager editor texture to be applied on scene load
- Added isQuest state to DebugState output of Sync Video Player
- Added Global Video Tex option to CRT configurations in Screen Manager
- Added Logo Image download option to Screen Manager

## [2.2.6] - 01-16-24

- Added _BindVideoPlayer method to Screen Manager to link manager to a different video player at runtime

## [2.2.5] - 01-03-24

- Added Enable AVPro In Editor option to Video Manager
- Added double buffer support to CRT shader

## [2.2.4] - 12-29-23

- Changed stream end timeout ignore of local player from 1s to 10s
- Updated implementation of VideoLockAclHandler to extend from AccessControlHandler base class

## [2.2.3] - 11-25-23

- Fixed Local Video Player not playing if it loses race with Video Manager
- Added continuous loop support to Local Video Player when using Unity sources
- Added experimental multi-point support to Audio Fade Zone

## [2.2.2] - 11-06-23

- Fixed remote clients infinitely loading new video in some cases if they were already loading a video
- Improve error description when property map is required but not set
- Added empty Screen Property Map prefab
- Added button to create an empty property map at field where it's missing
- Added default audio profile field to Audio Manager inspector
- Changed presentation of URL Source to select between Playlists / Custom
- Fixed resources not auto-initializing when first selecting sync player object

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
