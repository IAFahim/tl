# TypedPlaybackProto

CI step (`Typed playback lane parity and throughput`): `dotnet run --project benchmarks/TypedPlaybackProto -c Release --no-build -- 100000`.

The run checks the typed lane `Timeline<T>.Seek(...)` against hand-computed lanes with exact checksum parity — staggered, uniform, and waves row distributions, forward, backward rewind, catch-up, and multi-tick passes — and then prints batch throughput for 100,000 and 1,000,000 rows in segments of 256 over 60 ticks. Any parity failure exits nonzero.
