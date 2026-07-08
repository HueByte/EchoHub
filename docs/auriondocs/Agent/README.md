# Agent Context Pack — HueByte/EchoHub

Machine-oriented context for AI coding agents, generated deterministically from
the repository's symbol graph (no LLM involved). Reading this pack instead of
scanning the source tree saves tokens: one map read plus targeted doc lookups
replaces repeated file reads and greps.

| File | What it is | When to read it |
|---|---|---|
| [repo-map.md](repo-map.md) | Subsystem map + most-connected symbols | First — orientation |
| [symbol-graph.json](symbol-graph.json) | Full queryable symbol graph (kind, file, subsystem, complexity, dependencies, doc path) | To trace exact dependencies or find a symbol |
| `../Code/**` | Per-file generated documentation | For the behaviour of a specific file |
| `../Synthesis/Architecture.md` | Cross-cutting architecture narrative + diagram | For the big picture |

Suggested workflow: read `repo-map.md`; locate the symbols relevant to your task in
`symbol-graph.json` (each carries a `doc` path and `dependsOn` edges); open only
those docs/sources. The pack is regenerated on every full documentation run, so it
matches the commit recorded in the graph's `commit` field.
