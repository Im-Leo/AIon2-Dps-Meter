# Changelog

## [1.11.4.0] - 2026-10-04

### Added
- **Minimize to tray**: the Hide button and the toggle-visibility hotkey send the meter and its overlays to the system tray. Double-click the tray icon or use its menu (Show / Settings / Exit).
- **Show only over the game**: the meter and all its windows are visible only while AION2 (or the meter itself) is focused. They hide when you alt-tab away, stay hidden while the game isn't running, and reappear without taking focus from the game. On by default; toggle it in Settings → Appearance. The tray icon's Show still brings everything back.
- **Windows follow the game**: the meter and overlays are placed over the game window wherever it is, and follow it live when it moves to another monitor. Positions are remembered relative to the game window, never as screen coordinates.

### Changed
- The meter refreshes 10 times per second and re-renders only when a displayed value changes; bar movement is animated in CSS. This keeps its embedded browser from falling behind in large fights.
- A boss fight is marked completed and saved as soon as the boss dies, and fights left idle are completed when the history is opened, so they no longer stay "Active" after a dungeon's last boss.
- Settings, combat history, logs, the icon cache and packet logs are stored in `%LocalAppData%\Aion2DPSMeter` instead of the program folder, so they survive updates and deleting the folder. On first start, data from earlier versions is copied there; nothing is overwritten or removed.
- The Settings window can be moved by dragging anywhere on its header bar (previously only the title text), and is wider so all tabs fit on one row.
- Player rows use class colors by default.
- The meter's default size is 473×297, placed at the game's left edge.

### Fixed
- The app slowed down over long sessions: every mob ever hit kept an entry that was scanned on each damage and buff event. Finished entries are now dropped.
- The meter and the player-details window got slower as a fight went on: each refresh copied and rescanned every hit of every player. Hit statistics are now kept as running counts, the party DPS no longer recomputes all player stats, and the details window refreshes 4 times per second (only when the player has new hits) and reads just the new combat-log entries.
- Packet intake and the current-target lookup cost less per packet and per hit, most noticeably with many mobs fought nearby.
- Hits could go missing after a network hiccup: the meter lost its place in the game's data stream and kept discarding new data until it happened to realign.
- Player DPS is now damage over the whole fight's duration, so rows add up to the total. It used each player's own first-to-last hit time, so a single hit showed ten times its damage as DPS and late joiners looked far stronger than they were. The total no longer shows a huge number at the first hit of a fight.
- A fight could be missing from the combat history: a theostone hit from a player whose class wasn't known yet made saving it fail.
- A summoner's pet damage from before the meter knew its owner stayed on a separate row instead of the owner's.
- The meter could stay locked onto another program's encrypted traffic after the game was closed, instead of waiting for the game.
- Skill and buff icons could stay blank in the details window: an icon not yet downloaded never appeared while the window was open.
- Closing the skill cooldown overlay stopped other windows from reacting to edit mode.
- Overlays could ignore the mouse in edit mode: click-through restored a stale snapshot of WebView2's child windows, leaving its input window disabled.
