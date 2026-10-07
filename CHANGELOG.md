# Changelog

## [1.11.4.0] - 2026-10-04

### Changed
- The meter refreshes 10 times per second and re-renders only when a displayed value changes; bar movement is animated in CSS. This keeps its embedded browser from falling behind in large fights.

### Fixed
- The app slowed down over long sessions: every mob ever hit kept an entry that was scanned on each damage and buff event. Finished entries are now dropped.
- The meter and the player-details window got slower as a fight went on: each refresh copied and rescanned every hit of every player. Hit statistics are now kept as running counts, the party DPS no longer recomputes all player stats, and the details window refreshes 4 times per second (only when the player has new hits) and reads just the new combat-log entries.
- Packet intake and the current-target lookup cost less per packet and per hit, most noticeably with many mobs fought nearby.
- Hits could go missing after a network hiccup: the meter lost its place in the game's data stream and kept discarding new data until it happened to realign.
- Player DPS is now damage over the whole fight's duration, so rows add up to the total. It used each player's own first-to-last hit time, so a single hit showed ten times its damage as DPS and late joiners looked far stronger than they were. The total no longer shows a huge number at the first hit of a fight.
- A fight could be missing from the combat history: a theostone hit from a player whose class wasn't known yet made saving it fail.
