# Astrogator

<div style="margin:0.25em;padding:2px;text-align:center;">![mainScreenshot](screenshots/mainScreenshot.png)</div>

A space-navigational aide for [Kerbal Space Program](http://www.kerbalspaceprogram.com/).

See all the transfers that you could choose from your current location at a glance, including the time till the burn and delta V, and turn them into maneuvers with one click.

## Download

The [latest version]() is available on Github.

Unzip to your GameData folder.

## Features

### Transfers to more places

Many transfer calculators require the start and end bodies to be direct satellites of a common parent body.
For example, you could transfer from Mun to Minmus but not low Kerbin orbit to Minmus, or you can transfer from Laythe to Pol but not Laythe to Kerbin.

Astrogator also supports burns within the current SOI, as well as burns to bodies orbiting more distant "ancestors" of the current SOI.

### Automatic maneuver node creation

When you're piloting a vessel, maneuver node icons will appear next to the delta V numbers.
Click to create maneuver nodes for that transfer:

![Maneuver creation](screenshots/maneuverCreation.png)

These typically give immediate encounters for transfers to the Mun and Minmus from low Kerbin orbit, but some adjustment is usually needed for other destinations.

### Time warping

![Warping](screenshots/warping.png)

Click a warp icon to warp to 5 minutes before the corresponding transfer.
If it's already within 5 minutes, you'll be auto-warped to the exact time of the transfer.

### Settings

Click the wrench to open and close the settings panel:

![Settings panel](screenshots/settingsPanel.png)

| Setting | Description |
| --- | --- |
| Generate plane change burns | If you turn this off, then only (prograde) ejection maneuvers will be calculated. This may be needed if the plane change calculations become disruptive. |
| Include plane change burns in Δv display | If you enable this, then the delta V shown in the table will be the ejection node plus the plane change node. Otherwise only the ejection delta V is shown, to make ejection burns less confusing when flying without maneuver nodes. |
| Delete existing maneuvers | Our method for calculating plane changes doesn't work if other unrelated maneuvers are active. By default, we simply don't calculate them if that happens. Enabling this setting tells Astrogator to go ahead and delete your nodes if it needs to. **Use with caution!** |
| Automatically target destination | When this is enabled, clicking the maneuver node icon will set the destination as the active vessel's target. This can be helpful because it enables the close approach markers. |
| Automatically focus destination | When this is enabled, clicking the maneuver icon will change the map view focus. If the default maneuvers create an encounter with the desired body, then that body will be focused so you can fine tune your arrival; otherwise the destination's parent body will be focused so you can establish the encounter. |
| Automatically edit ejection node | When this is enabled, clicking the maneuver icon will leave the first node open for editing. |
| Automatically edit plane change node | If you enable this, then the second node will be opened for editing instead of the first. |

## Known limitations

- Blizzy's toolbar is not and will not be supported. 0.23.5 was a long time ago.
- It's not going to fly your ship for you. Other mods can do that already.
- Only the phase angle approximation is used, not full porkchop plots, for performance reasons.
- I don't know how to calculate plane change burns directly, so instead we create a succession of actual maneuver nodes at the AN/DN and ask KSP to tell us the resulting AN/DN, until it gets close to 0. This gets messy because we can't do that if the user has already set up some maneuver nodes that he wants to keep. Sometimes it raises exceptions inside core code. It also means you have to refrain from messing with maneuvers while this is going on.
- I can't get the maneuver / warp buttons to show their built-in `tooltipText` property, and they don't seem to have `onHover` events to set one up manually like we did for the app launcher, so it won't tell you what those icons do.

## Future plans

- Robustness
  - **Calculate plane change burns without instantiating ejection burns**
  - Delta V for burns among Jool's moons is way too high, maybe due to SOI size?
  - Create useful numbers for launches/landed and KSC
  - Return-to-LKO burns from Mun and Minmus
    - Lowest warp altitude limit + 5km
  - Include target Vessel as a destination
- Publish
  - Push to Github
    - Make a Release
  - New thread on forums
  - [DialogGUI* thread](http://forum.kerbalspaceprogram.com/index.php?/topic/149324-popupdialog-and-the-dialoggui-classes/)
  - Reply to [Kerbal Navigator post](http://forum.kerbalspaceprogram.com/index.php?/topic/138886-what-mod-should-i-make/&do=findComment&comment=2562076), @Nansuchao
  - CKAN

## Building

### Linux

```sh
git clone git@github.com:HebaruSan/Astrogator.git
cd Astrogator
ln -s /path/to/KSP/KSP_x64_Data src
make
```

## References

### Plug-in authoring
- http://forum.kerbalspaceprogram.com/index.php?/topic/153765-getting-started-the-basics-of-writing-a-plug-in/
- http://forum.kerbalspaceprogram.com/index.php?/topic/151354-unity-ui-creation-tutorial/
- http://forum.kerbalspaceprogram.com/index.php?/topic/78231-application-launcher-and-mods/
- http://forum.kerbalspaceprogram.com/index.php?/topic/154006-solved-texture-issues/&do=findComment&comment=2904233
- https://kerbalspaceprogram.com/api/index.html

### Physics and math
- https://en.wikipedia.org/wiki/Hohmann_transfer_orbit
- https://en.wikipedia.org/wiki/Orbital_speed#Precise_orbital_speed
- https://www.reddit.com/r/KerbalAcademy/comments/35wtv1/how_do_i_calculate_phase_and_ejection_angle/crf3kc4/
- http://www.bogan.ca/orbits/kepler/orbteqtn.html
- https://d2r5da613aq50s.cloudfront.net/wp-content/uploads/411616.image0.jpg
- https://en.wikipedia.org/wiki/Orbital_inclination_change#Calculation

### Performance
- http://www.somasim.com/blog/2015/04/csharp-memory-and-performance-tips-for-unity/
- http://forum.kerbalspaceprogram.com/index.php?/topic/142712-devnote-tuesday-smashing-buttons/&do=findComment&comment=2653161

## Credits

- Phase angle logic and some icons borrowed from Kerbal Alarm Clock
- `.gitignore` borrowed from Transfer Window Planner
- `csproj` file borrowed from Transfer Window Planner and Craft Import and adapted. (I develop with `xbuild` and so could not generate files with an IDE.)
- r4m0n for making it easy to find plane change nodes
- [TCShipInfo](http://forum.kerbalspaceprogram.com/index.php?/topic/59724-112-v04-resource-details-in-tracking-center/) for figuring out the tracking station API
- Main app icon modified from http://fontawesome.io/icon/map/
- ProjectGuid generated by https://www.guidgenerator.com/online-guid-generator.aspx
- Hohmann, Tsiolkovsky, and Oberth for giving us the math
