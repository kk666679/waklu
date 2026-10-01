# `.autoclaw/anchors/` — integrity, not authority

Formerly `blockchain/`. The rename is the point.

An anchor records that a hash existed at a time. That is a statement about
immutability, not about validity. Nothing in this directory produces,
influences, or reads a compliance status, and the directory tree is shaped to
make that hard to do by accident: the only output is a receipt.

## What leaves this directory

A receipt. `anchors/verification/receipt-verifier.md` describes its shape. A
receipt carries a hash, a chain reference, a block or transaction reference, a
timestamp, and the issuer. It does not carry a decision, and it is not an input
to any rule in `compliance/rules/`.

## What is deliberately absent

- No read path for compliance status. A status on a chain is not a HalalChain
  verdict, and treating it as one would reintroduce exactly the authority
  confusion the rename removed.
- No provider-specific logic in the workflow layer. `providers/` describes
  chains; it does not describe who decides.
- No agent or skill writes here. `anchors/` is driven by
  `workflows/evidence-anchor.yaml`.

## Layout

```
policies/       which evidence is eligible to be anchored, and on which chain
events/         anchor-requested / anchor-confirmed / anchor-failed schemas
providers/      evm-sepolia, evm-polygon, hyperledger-fabric
verification/   how a receipt is checked
```

## Eligibility

`policies/anchor-eligibility.yaml` decides what gets anchored. In short:
anything whose integrity claim matters downstream and whose hash is stable.
Not every piece of evidence qualifies; anchoring everything anchors nothing.

## The receipt is not the certificate

A receipt proves a hash has not changed. A certificate asserts something about
a product's compliance. They are different claims made by different parties,
and the platform keeps them separate all the way through. When a certificate is
revoked, its anchor receipt stays: the receipt is still true, it just no longer
describes anything anyone should rely on. That asymmetry is the design.
