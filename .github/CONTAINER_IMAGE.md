# Maintained container image

`container-image.yml` turns an explicit revision from this fork into an
attested OCI image in `ghcr.io/resonant-projects/vedastro`. Builds run on the
Resonant Projects GARM pool as disposable Incus guests. The build definition is
kept separately from the selected source revision so a historical release can
be rebuilt with patched, digest-pinned build tooling.

The current upstream `master` does not compile as of
`fcb4dede360372545eb244c53e9a80ec3510e194`; the API build reports 698 missing
calculation members. For that reason the workflow defaults to the last known
runtime revision, `92561401c0728bdc925706fa896fbb4b5342b1d7`. This is an
explicit compatibility baseline, not a claim that the old code is current.
That revision also expected an ignored, workstation-generated
`API/ThisAssembly.cs`; the trusted build definition supplies an auditable shim
and records the selected source SHA in the running image.

Keep `master` synchronized with [`VedAstro/VedAstro`](https://github.com/VedAstro/VedAstro).
Develop runtime fixes on topic branches in this fork, dispatch the image
workflow with that branch or commit, and contribute generally useful fixes
upstream as separate pull requests. Change the default source revision only
after its container build and synthetic API checks pass.

Every published image has a source-SHA tag, an OCI provenance attestation, and
an immutable registry digest. Deploy only the full `name@sha256:...` reference;
the SHA tag is for discovery, not promotion.
