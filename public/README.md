# `public/`

Static image assets served or embedded from the repository root.

## Purpose

Holds binary assets that are not compiled and not part of any build step. There
is exactly one file.

## Structure

```
public/
└── HalalChain-Figlet.png    # ~47 KB wordmark, rendered from the HalalChain
                             # name in a figlet-style ASCII font
```

## Usage

This is a source asset, not a build artifact.

The Node CLI depends on `figlet` and can regenerate the equivalent ASCII
banner; see [`HalalChain-Cli/src/lib/banner.ts`](../../HalalChain-Cli/README.md)
and `npm run halalchain:cli`.

To reference it from a Razor or Blazor page, either copy it into the relevant
`wwwroot/` or serve it from here through the web host's static-file
configuration. Neither UI project currently references this path — each ships its
own assets under its own `wwwroot/`.

## Notes / Limitations

- `.dockerignore` and `.gitattributes` treat root binary files specially; check
  both before adding another asset here.
- Nothing in the build reads this directory, so adding files will not be picked
  up automatically.