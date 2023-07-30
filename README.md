# VideoTXL
Prefabs for video players and A/V support.  Sync and local-only flavors of the video player are available, with multiple shared plugin components.

Support discord: https://discord.gg/cWKeenfsuq

![Main video player controls](Docs/Images/main_controls.png)

## Installation

VideoTXL is now distributed as a VPM package along with several other TXL packages that used to be included in the main VideoTXL unity package.

### Add the VPM repository

To download and install VideoTXL, add the following VPM repoistory to your creator companion: https://vrctxl.github.io/VPM/
To add the repository:

* Click the repository link to open it in your browser. 
* Press the first "Add to VCC" button to add the repo to your creator companion.

Or to add manually:

* Open the Packages tab on the Settings page of the creator companion.
* Press the "Add Repository" button in the upper-right corner.
* Paste in the repo URL: https://vrctxl.github.io/VPM/index.json

### Add the package to your project

* Add the "TXL - VideoTXL" package to your project from the packages list.
* The "TXL - CommonTXL" package will also be brought in automatically as a dependency.
* Consider adding any of the other TXL packages for things like stage microphones, advanced access control, etc.

## Upgrading from pre-VPM releases

To upgrade from the **VideoTXL 2.0 betas**, you can import the VPM packages directly into an existing project.  The related
assets in Assets/Texel will be automatically removed.  If you're on an early beta, it would be a good idea to upgrade to
beta 9 first, reaching the release notes of each beta along the way for a few important changes the happaned along the way.

To upgrade from the **VideoTXL 1.4.x** release or earlier, there is no direct upgrade path.  You should fully remove the
Assets/Texel folder, and recreate your video player setup.  It would be a good idea to check the project wiki for up to
date information on the video, audio, and screen managers, as much has changed going into 2.0.

## Adding a video player to your scene

The main video player prefab is **Sync Video Player** under `Packages/TXL - VideoTXL/Runtime/Prefabs`.  Drag the prefab into
your scene for a default setup that supports AVPro and Unity video backends with the default audio profile, similar to the
old 1.4.x prefab.

There are other video player prefabs located in `Packages/TXL - VideoTXL/Runtime/Prefabs/Other Video Players`.  These include:

* Basic Sync Video Player
** An ultra-stripped down AVPro video player with just two scripts.  This is essentially the same prefab as the AudioLinkMiniPlayer, distributed with AudioLink.
* Local Video Player AVPro
** A fully local (not network synced) video player based on AVPro.  Supports many of the same components as the main sync video player, and can be controlled by external scripting.
** A good choice for events that need to run a single streaming URL and want to minimize points of failure.
* Local Video Player Unity
** A fully local (not network synced) video player based on Unity Video.  Supports many of the same components as the main sync video player, and can be controlled by external scripting.
** A good choice for locally triggered video playback, like exhibit pieces that play an MP4 or YouTube video when you approach or interact with them.
* Sync Video Player Full
** A variant of the Sync Video Player that supports more video source configurations (different resolutions) and audio profiles.
** See customizing below for more information.

In some cases, it may be necessary to select the video player object in the scene and press the **Update Connected Components** button
in the inspector window.  This synchronized the audio profiles with the video sources and updates UI components that can't be controlled
at runtime.  In most cases, this should be done automatically for your as part of a pre-build hook.

## Customizing the video player

In addition to a few settings located in the main video player object, the video player is mainly configured and controlled by
three manager objects that are direct children of the video player prefab.

### Video Manager

The video manager is responsible for actual video playback.  The configuration lists one or more video sources, which are conifugrations
of either AVPro or Unity Video components.  Video source prefabs can be found in `Packages/TXL - VideoTXL/Runtime/Prefabs/Video Sources`
and they can be added as children to the Video Manager object.

See the [full documentation of the Video Manager](https://github.com/vrctxl/VideoTXL/wiki/Configuration:-Video-Manager) for more information on video sources
and why you might want to add or remove them.

### Audio Manager

The audio manager is responsible for defining audio profiles, managing the related audio components for video sources, and interacting with external
systems like AudioLink and VRSL.  Audio profiles reperesnt a set of configured audio channels.  Example profiles include the default profile based on a single
global audio sources.  Other profiles could represent stereo or 5.1 surround sound, or special setups like ARC DSP.

See the [full documentation of the Audio Manager](https://github.com/vrctxl/VideoTXL/wiki/Configuration:-Audio-Manager) for more information on audio sources
and why you might want to add, remove, or reconfigure them.

#### AudioLink

To support driving AudioLink with the video player, press the **Link AudioLink to this manager** button in the inspector window of the AudioManager.  It should
find AudioLink in your scene automatically, but you can set the reference yourself if you need to.  The manager will warn you if you've tried binding more than one
video player to the AudioLink object, which would cause a conflict.

#### VRSL

To support the Audio DMX capability of VRSL, press the **Link VRSL Audio DMX to this manager** button in the inspector window of the AudioManager.

### Screen Manager

The screen manager is responsible for updating materials and shaders throughout your world in response to videos being played.  It will automatically handle
the differences between AVPro and Unity video sources, and it supports a variety is custom textures that can be displayed in different states, such as idle or loading.

There are multiple ways the manager can update your scene.  By default, it is setup to override the material property block of the video screen quad that comes with the prefab.
This allows multiple video players to be placed in the world and not conflict with each other in their default setups.

The easiest way to apply video textures in your world is to enable the **Use Render Out** option, which will write out the video data to a custom render texture resource
in your project.  That CRT can be used anywhere else in your scene like a regular texture.  This is the preferred way to integrate with systems like VRSL or Pi's LTCGI.

See the [full documentation of the Screen Manager](https://github.com/vrctxl/VideoTXL/wiki/Configuration:-Screen-Manager) for more information on the different ways
the manager can update your scene.  While the CRT is the easiest way to work, other methods may give you more control.

## Support

If you have questions or need help, check out the project discord: https://discord.gg/cWKeenfsuq

