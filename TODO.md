# TODO

- [ ] Reduce ejection burn by the amount the plane change burn adds to it (i.e., fix Minmus overshoots)
- [ ] Allow a window of times for transfers
  - Calculate current absolute time
  - Use a fudge factor that scales with orbital period to choose a range
  - Translate outer ranges into sets of inner ranges
  - At the "leaf nodes", choose the center of a range as the burn time
  - Pick the soonest range when showing times or making maneuvers
  - Only rule out a range once the entire thing is overdue
- [ ] i18n / l10n (once SQUAD releases their version of it)

## More transfer types

- [X] To tracked asteroids
- [ ] Rendezvous with asteroids near Pe of their future encounter/escape trajectories
- [ ] Capture burns for inbound hyperbolic orbits
- [ ] Launch to orbit
- [ ] Return-to-LKO from Mun and Minmus

## Code style

- [ ] Split CalculateEjectionBurn into transfer versus ejection functions
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