# Dependency rights and owner remediation

**This is a source-only inventory, not a license grant.** All noncode game assets are excluded. The user authorized publication of the selected own code, and the two-commit baseline is now public. Downstream licensing remains unchosen; that is an informational reuse boundary, not a new publication-authorization requirement. Public visibility alone does not authorize downstream reuse.

## Conservative classification

| Class | Treatment |
|---|---|
| A — team-provided original content | Team-provided code, metadata and selected configuration are retained unchanged. Authorship is owner-supplied; no team license is invented. Potentially team-authored cube/controller assets are also conservatively excluded, not automatically assigned to B. |
| B — explicitly permissive third-party raw resources | Requires a verified full redistribution grant, provenance and applicable notices. None are included here. The shader notice described below is only an unverified B candidate and is excluded. |
| C — external raw resources without accredited public-redistribution permission | Raw resources and metadata from the four Asset Store packages below, and the six Mixamo FBX clips identified below, are excluded. Embedded licensed-product use is distinct from raw-asset publication; unresolved permission is not a categorical finding of prohibition. |
| D — generated or assembled content requiring review | Original main scene and backups, nested vendor enemy wrappers, baked NavMesh asset/metadata, templates and other settings may contain foreign or personal data and are excluded. Scene references differ from embedded foreign content; not all prefabs are inherently unlawful. Compiled build distribution has a different licensing context, but executables and companions are excluded from this source-only snapshot. |

The excluded Mixamo set comprises Zombi Idle, Walk and Run, and Rifle Idle, Walk and Run: six FBX clips and their matching metadata. The four Asset Store package groups are Sci-Fi Construction Kit (Warehouse), Character08 Renegade, NewPunch Shirtless Zombie and ZombieMale AAB. Free availability, modification, GUID-only references and credits do not by themselves establish raw-redistribution permission. The earlier Mixamo HTTP 403 leaves the raw-license inquiry unresolved; it is not evidence of a categorical prohibition.

The package manifests are version/dependency references, not vendored package code or a grant to redistribute all dependencies. `.gitattributes` retains LFS configuration, but this snapshot contains zero LFS pointers and zero media assets. No LFS download was performed.

## Official dependency references

| Dependency | Publisher / reference |
|---|---|
| Sci-Fi Construction Kit (Modular) | Sickhead Games — https://assetstore.unity.com/packages/3d/environments/sci-fi/sci-fi-construction-kit-modular-159280 |
| Sci-Fi Character 08: Renegade | Maksim Bugrimov — https://assetstore.unity.com/packages/3d/characters/humanoids/sci-fi-character-08-renegade-208640 |
| Free Shirtless Zombie | Studio New Punch — https://assetstore.unity.com/packages/3d/characters/humanoids/fantasy/free-shirtless-zombie-276762 |
| Free Zombie Male AAB | Studio New Punch — https://assetstore.unity.com/packages/3d/characters/humanoids/free-zombie-male-aab-336744 |
| Unity Asset Store terms | https://unity.com/legal/as-terms — Appendix 1, sections 2.2.1(a), 2.2.1(b) and 2.2.1.1(d) distinguish licensed embedded-product use from raw-asset redistribution |
| Mixamo FAQ | https://helpx.adobe.com/creative-cloud/faq/mixamo-faq.html — owner-supplied FAQ confirms animation use in games; tool HTTP 403 was not retried. This does not establish the team's particular acquisition or permission to publish raw FBX files. |
| Unity Asset Store EULA FAQ | https://assetstore.unity.com/browse/eula-faq — previously verified embedded-game permission is distinct from raw redistribution and does not prove this team's acquisition |

The previously examined `Standard_SNP_DoubleSided_AAB.shader` MIT notice referred to a missing `license.txt`; the full grant and provenance were not verified. It remains an excluded conditional B candidate, outside this allowlist, and is not processed or included. A shader notice does not grant rights to models or other package assets. No third-party notices were removed from retained source, and no contributor names or copyright assertions are invented.

## Existing-public-history record

The prior read-only remote/history review reported `master` at `6a9c9b7`, one advertised head, zero tags, zero advertised PR refs and four reachable commits. Those ref counts are historical, not the current remote state after source-branch publication. That review inventoried 845 review-root files, 847 historical files and 221 LFS pointer blobs: 89 FBX, 85 PNG, 10 WAV, 36 TGA and one EXR, plus a raw `Stand.obj` blob.

Warehouse, Character08 and 3Rifle content was already present in public `master`, beginning at `a934…` and later `5f67…`. NewPunch (79 files), ZombieMale (128), Zombi (9), and the reviewed local commits with prefixes `8a`, `c679`, `859`, `059` and `ffe` were absent from that published reachable graph. These are prior-review facts, not a fresh network observation. No remote was contacted while creating this copy.

Old refs, dangling objects and server caches remain unknown. This snapshot does not establish that the existing repository is cleaned. Its raw-material licensing gap remains regardless of the contents of this independent branch.

## Owner action remains separate

The compiled Windows package has separate, version-specific conditions recorded in [DELIVERY_STATUS.md](DELIVERY_STATUS.md). This source-only record does not establish runtime-package compliance. The settled Newtonsoft notice was not re-audited or changed in this documentation unit.

The selected source publication is already authorized and complete at the baseline. Any remediation of excluded material in older public history remains separate and requires its own authorization; the new source branch does not hide that history. Archiving alone does not hide public content.

The initial push and GitHub API branch validation for `chore/public-source` were confirmed at `ba06fa071dd6bf476d132cac4cb506a29d4711f8`. Public annotated tag `v1.0.0-pa4` (`847d4b3a6b386f133aace17b6840819e36f54794`) points to the unchanged parentless root `5ecc3e87796ec58e315114316a5abcb189631ed6`, containing exactly 122 files. Subsequent delivery-document commits belong to the separate source branch and do not change that tag. Compiled-package conditions remain unresolved; no downstream team-code license has been invented.

No original `master` change, force push, history rewrite, deletion, rename, merge, rebase or cherry-pick is authorized by these local commits. Do not import original history into this unrelated root. Any cleanup of the existing public repository requires separate explicit owner authorization and coordination; this local source-only history neither hides its existing assets nor reconstructs the original playable scene automatically.
