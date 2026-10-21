# README artwork and screenshots

`workspace-light.png` and `workspace-dark.png` are unmodified screenshots of HandyLime 0.4.0, supplied by the repository owner for the public README. They show the empty workspace. Screenshots containing personal file paths, activity filenames, Explorer content or other background documents were not included.

`hero.svg`, `shortcuts.svg` and the three button SVGs are original HandyLime artwork, distributed under the repository's MIT license. The hero wheel is a vector illustration of the workflow, not a screenshot. The original fruit mark matches `src/HandyLime.App/Brand.cs`; icon paths come from the existing Lucide set, covered by [third-party notices](../../THIRD-PARTY-NOTICES.md) and [Lucide's license](../../licenses/Lucide-LICENSE.txt).

The images have no scripts, external fonts, embedded screenshots or external image requests. Regenerate the SVGs with Node.js:

```text
node scripts/Build-ReadmeAssets.mjs
```

Keep screenshot replacements free of personal paths, user documents and account information. Use real screenshots for application previews and label diagrams or illustrations as such.
