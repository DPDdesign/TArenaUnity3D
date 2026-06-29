# Context Economy And Spark Runbook

Use this for every TArenaUnity3D agent workflow.

## Default Mode

Work quietly and optimize for context economy and evidence quality.

Allowed communication:

- short task contract before medium or large work,
- short blocker or risk note when needed,
- concise final report with evidence.

Avoid long narration, repeated status summaries, broad repo exploration, and
large rewrites unless the user explicitly asks for them.

## Task Contract

Before medium or large work, define a short internal contract:

- goal,
- source of truth,
- known files or surfaces,
- out of scope,
- definition of done,
- QA checks,
- Spark usage plan, when useful.

## Source Order

Prefer targeted source routing:

1. active task, PRD, or user brief,
2. `_codex/Context/CONTEXT-MAP.md`,
3. relevant domain map under `_codex/Context/maps/`,
4. `_codex/agents/docs/codebase-map.md`,
5. relevant QA notes or known bugs,
6. candidate production files,
7. tests.

Read maps before code when they exist. Use targeted search instead of broad
repo-wide exploration.

## Spark Subagents

Use GPT-5.3-Codex-Spark subagents for bounded reconnaissance when it saves
GPT-5.5 context, time, or attention.

Spark may help with:

- finding candidate files,
- locating duplicated or stale logic,
- checking docs, PRDs, QA notes, and code maps for consistency,
- mapping a known subsystem,
- finding related tests or known bugs.

Do not use Spark for vague repo-wide searches, architecture judgment, product
decisions, or implementation ownership. The primary agent remains responsible
for final decisions, code changes, verification, and reporting.

Use up to 5 Spark subagents only when the work has clearly separate bounded
recon tracks. Small tasks usually skip Spark unless file location is unclear.

## Delegation Format

Every Spark task must include:

- exact question,
- known surface, folder, PRD, feature, subsystem, or likely files,
- expected search terms, when useful,
- read-only permission unless a tiny Markdown-only fix is explicitly allowed,
- expected concise report format.

Spark reports should include checked surfaces, search terms, candidate files,
key findings, possible seams or inconsistencies, files changed if any,
confidence, and recommended next step.

Verify relevant Spark findings directly before code edits or non-trivial
documentation changes.

## QA Before Code

Before implementation, define the minimal QA checks for expected behavior,
known bug reproduction when applicable, regression risk, source-of-truth
consistency, UI consistency when applicable, and AI/server-side readiness when
applicable.

After implementation, report only checks that were actually performed or clear
manual Unity validation steps for the user.

