# OneLevel (BETA)

> 🚧 **IMPORTANT:** This mod is usable but not finished yet and has many bugs 🚧

Hollow Knight mod that makes Hallownest one big level rather than a collection of separate rooms.

Features:

- All areas of Hallownest are visible in the world at once
- Zoom in/out using by scrolling with the mouse wheel

## Settings

- `ZoomSpeed` -- The speed at which scrolling the mouse zooms the level in and out.
- `Beta_DisableTransitions` -- You can disable transition points by changing this to `true`. There are currently gaps between chunks where you can fall out of the world and I don't plan on filling them in until the chunk placements are stable which is why this setting is disabled by default. Once the gaps are filled and chunk reloading is implemented (so that enemies can respawn) this mod will reach 1.0 and this setting will default to `true`.

## Known issues and caveats

This mod started out of curiosity after watching [this video](https://www.youtube.com/watch?v=24CbP6nP4Fc) so at first I wasn't trying to make it work well, just to see if it would work at all, but I guess I'll keep going while I still have motivation. 😄 Below is my to-do list of things to fix:

- **Not all the chunks have been mapped yet**
  - Currently I've only mapped Dirtmouth and part of Forgotten Crossroads
- **Dying spawns the chunk in the wrong location**
  - Just haven't gotten around to handling this case yet
- **Weird movement bugs like sliding when transitioning into a chunk**
  - Don't know what's happening there, haven't looked at it yet
- **Some scenes overlap visually**
  - Some scenes have visual elements that extend outside their bounds and overlap neighboring scenes
  - These will need to be manually fixed on a case-by-case basis
- **Parallax effect looks weird at certain angles**
  - The levels weren't designed to be looked at from below, so you can see things like floating shrubs in Dirtmouth when looking from a lower camera angle than normal
  - Not much I can do about this but might have good viewing angles more often when support for camera limits is added
- **Camera flickering when zooming out**
  - I haven't figured out why that happens yet
- **Particle effects are removed**
  - Like above their position is tied to the camera so the effect doesn't work when zoomed out
  - Also need to decouple this
- **Background items are overly/underly blurred**
  - Compared to the unmodded game the blurring/visibility of background items is inconsistent depending on the zoom level
  - I tried fixing this a bit but will require more work
- **Camera limits and locks are removed**
  - Normally the camera will stay still in certain places like boss battles, important moments or the edge of the map
  - There are lower limits harcoded in the camera methods of the game (since every scene has `(0, 0)` at the bottom-left corner) so I just moved the entire map to the top-right and removed the scene upper bounds and camera lock game objects to save time trying to make it work nice when the player could be zoomed in any amount but ideally it would still respect these
- **You can see in dark areas**
  - Normally the you can only see about a screen's width around you even in lit areas of the game, but that defeats the purpose of this mod if you can't see any areas but the one you're in!
- **Some things don't reset when turning the mod off mid-game**
  - I'm lazy so small things like camera limits are not restored until the game changes them next

## Future plans

In addition to fixing the issues above I plan to implement these things in roughly this order:

- **Click and drag positioning of chunks** to make arranging chunk positions easier during development
- **Click and drag camera** to see parts of the map you are not already in
- **Options menu** for configuring the mod
- **Only load visible chunks** for performance
- **Chunk reloading** so that chunks which the player has left will reload and respawn any enemies which were killed
- **Multiple chunk maps** so that custom maps and other areas can arrange their maps into a single level
- **Interior rooms** shown behind the main rooms (blurred) and camera zooms through the main rooms when entering an interior room

## Development

Update the `HollowKnightPath` variable in the `.csproj` to the path of your game then:

```ps
dotnet build
```
