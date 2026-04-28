# Changelog

## Auto scan — save profile picker

- **Automatic discovery** of Windrose character saves under `%LOCALAPPDATA%\R5\Saved\SaveProfiles\`: walks each Steam ID folder → `RocksDB\<version>\Players\<profile>` and lists folders that contain a **`CURRENT`** file.
- **`SaveProfiles\*_backups`** directories are skipped.
- **Profile list**: each entry shows a friendly label using the **character name** when it can be read from profile data; otherwise Steam ID folder (shortened) plus profile id / guid hint.
- **Sync (↻)** rescans disk and refreshes the list without restarting.
- **Browse folder…** still allows picking any player folder that contains `CURRENT` when auto-scan misses a path.
