\# Unity Project Rules for Codex



This is a Unity 6 card merge game.



\## Important Unity safety rules



Do not modify these files unless the user explicitly approves:

\- \*.unity

\- \*.prefab

\- \*.asset

\- \*.meta

\- ProjectSettings/\*\*

\- Packages/\*\*



Prefer script-only changes.



Do not rename:

\- public fields

\- \[SerializeField] fields

\- ScriptableObject fields

\- existing card IDs

\- existing GameObject names

\- existing prefab names

\- existing scene names



Renaming serialized fields can break Unity Inspector references.



\## Project architecture rules



Do not replace the existing card loading system.

Do not replace the existing CardBackpackTest scene flow.

Do not replace existing config/data classes unless explicitly approved.

Do not refactor the whole card system.



If a UI effect is needed:

\- Use temporary visual clones for animation.

\- Do not directly animate UI objects controlled by LayoutGroup.

\- Put merge/preview animation objects under a separate top-level UI effect layer.

\- Keep original cards in their normal layout until the logic decides to remove or move them.



\## Workflow rules



Before editing files:

1\. Inspect the current code.

2\. Explain which files are related.

3\. Explain which files you plan to edit.

4\. Ask for approval if you need to touch scenes, prefabs, ScriptableObjects, or assets.



After editing:

1\. List all changed files.

2\. Explain what changed.

3\. Explain what manual Unity Inspector setup is needed.

4\. Mention any risks.

