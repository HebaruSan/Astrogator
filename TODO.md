# TODO

- [ ] Reduce ejection burn by the amount the plane change burn adds to it (i.e., fix Minmus overshoots)
- [ ] Allow a window of times for transfers
  - Calculate current absolute time
  - Use a fudge factor that scales with orbital period to choose a range
  - Translate outer ranges into sets of inner ranges
  - At the "leaf nodes", choose the center of a range as the burn time
  - Pick the soonest range when showing times or making maneuvers
  - Only rule out a range once the entire thing is overdue
- [ ] Merge the correction burn into the ejection burn
- [ ] Re-do launch approximation to increase accuracy

## Localization clean-up

- [ ] ["the Mun"](http://bugs.kerbalspaceprogram.com/issues/14314)
- [ ] [Ascending/descending sort markers](http://bugs.kerbalspaceprogram.com/issues/14317)
- [ ] Times not showing up in Chinese and Japanese

## Fixes

- [ ] Test vessel destruction: deorbit, deletion
- [ ] Crash on probe loss of radio contact
- [ ] Keys stop working when nested encounter established
- [ ] Freeze on set orbit cheat

## Code style

- [ ] `onVesselGoOffRails` / `onVesselGoOnRails` for the `OnOrbitChanged` checks
- [ ] Generalize retrograde orbit special cases
- [ ] Split ViewTools: Truly generic stuff versus this project's stuff
- [ ] Split MathTools off from PhysicsTools
- [ ] Factor out a SimpleMod base class
  - App launcher button
    - Tooltip
  - Main window
  - Settings
  - Resources
  - Event handlers
- [ ] Implement [TaxiService's method of updating the UI without close/reopen](http://forum.kerbalspaceprogram.com/index.php?/topic/149324-popupdialog-and-the-dialoggui-classes/&do=findComment&comment=2950891)
- [ ] Unit tests

## Deferred pending stock changes

- [ ] Rendezvous with asteroids near Pe of their future encounter/escape trajectories
