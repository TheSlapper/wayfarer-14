# `.midplaylist` File Format

Self-contained binary format for the in-game MIDI instrument's playlist Save/Load. A saved file holds the playlist and every track's MIDI bytes.

## Layout

All multi-byte integers are signed 32-bit little-endian (`Int32`). All strings are UTF-8, no byte-order mark.

### Header

- `magic` — 4 bytes, ASCII `"WPLY"`. Must match exactly.
- `version` — `Int32`. Currently `1`.
- `trackCount` — `Int32`. Range: 0 to 2048 inclusive (`MaxTrackCount`).

### Track record (repeated `trackCount` times)

- `nameByteLen` — `Int32`. Range: 0 to 1024 inclusive (`MaxNameBytes`).
- `nameBytes` — `nameByteLen` bytes of UTF-8 track name. May be empty. The UI shows a default label like "Track N".
- `midiByteLen` — `Int32`. Range: 0 to 4_194_304 inclusive, i.e. 4 MiB (`MaxMidiBytes`).
- `midiBytes` — `midiByteLen` bytes, the raw `.mid` / `.midi` file contents.

Theoretical maximum file size is ~8 GiB. Typical files are KiB to a few MiB.

`midiBytes` holds the raw contents of a Standard MIDI File (`.mid` / `.midi`). The format does not validate the MIDI. Playback parses it through the in-game synth.

## Reader behaviour

The reader rejects the file on any of these conditions. The existing playlist is left untouched.

- The first 4 bytes are not `"WPLY"`.
- `version` is anything other than `1`.
- `trackCount` is negative or above 2048.
- `nameByteLen` is negative or above 1024.
- `midiByteLen` is negative or above 4 MiB.
- `BinaryReader.ReadBytes(n)` returns fewer than `n` bytes (truncated file).
- An `IOException` is thrown during read (covers `EndOfStreamException`).

The reader stops after exactly `trackCount` track records. Any bytes after the last track are ignored.

A successful load replaces the existing playlist.

## Writer behaviour

- Save writes the playlist in one pass. There's no partial-write recovery.
- Save does nothing if the playlist is empty.

## Versioning

`version` is reserved for future format changes. A v2 reader should accept v1 files too, and reject anything it doesn't recognise.

## Hex example

A two-track playlist containing the names `"intro"` and `""` with two tiny MIDI payloads (`AA BB` and `CC DD EE`):

```
57 50 4C 59                        "WPLY"
01 00 00 00                        version = 1
02 00 00 00                        trackCount = 2

05 00 00 00                        nameByteLen = 5
69 6E 74 72 6F                     "intro"
02 00 00 00                        midiByteLen = 2
AA BB                              MIDI 1

00 00 00 00                        nameByteLen = 0  (empty name)
03 00 00 00                        midiByteLen = 3
CC DD EE                           MIDI 2
```
