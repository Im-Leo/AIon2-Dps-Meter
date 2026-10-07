# Changelog

## [1.11.4.0] - 2026-10-04

### Fixed
- The app slowed down over long sessions: every mob ever hit kept an entry that was scanned on each damage and buff event. Finished entries are now dropped.
