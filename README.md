# VideoTXL
Prefabs for sync and local video players built from common components.  Local video players can be useful for playing a burned in stream during a live event, or for populating screens in multiple booths in a showcase hall.

## Local Video Player Features
* Optional support for all shared components listed below
* Setup with either AVPro or Unity video player source
* Streams or videos
* Loop and resume last position options
* Supports burned-in URLs at multiple quality levels that can be user-selected at runtime

## Sync Video Player Features (WIP)
* Optional support for all shared components listed below
* Primarily AVPro-based
* Streams or videos
* Video seeking and current position / duration

## Shared Components
* Screen Manager
  * Can show alternate screens for stopped, loading, error, and audio-only states
  * Can update materials on multiple screen objects
  * Can update textures on multiple materials
  * Auto-detect audio-only sources
* Audio Manager
  * Manage volume on multiple audio sources
  * Supports separate base and overlay audio sources with different volume ratio when using an AVPro vidoe source
  * Supports artificially scaling volume between two collision boundaries
* Trigger Manager
  * Support playing video on world load
  * Support playing video when entering or leaving zones
  * Support playing video with play/stop buttons
* Local Controls
  * A separate UI providing local-only AV options that can be duplicated in the world any number of times
  * Quickly setup for any of these options:
    * Volume slider
    * 2D audio toggle
    * Resync button
    * stream quality selection (local player only)
  * All instances can be tied to a color profile for fast customization

## Installation
1. Install the latest VRCSDK and latest release of UdonSharp
2. Install the latest release or check out latest source
3. Select the most appropriate prefab from the prefabs folder and drag into scene.
4. Resize screen, move components like control panels around, and further customize components as necessary.

An example world is included with multiple local video player setups and a sync player setup to demonsrate the various ways players can be configured.
