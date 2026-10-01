# Receipt Verification

A receipt proves one thing: the hash in it was recorded on the named chain at
the named reference, and has not changed since. It proves nothing about
validity, scope, or compliance status.

## Receipt shape

```
receipt_id
subject_type        evidence | certificate | batch | shipment
subject_id
content_hash        sha256 or keccak256 of the sealed capsule
chain               provider id from anchors/providers/
tx_reference        transaction hash
block_reference     block number or height
recorded_at
issuer              participant id that requested the anchor
tenant_id
status              confirmed | failed | orphaned
```

`status: orphaned` means the transaction is not on the canonical chain — it
lived on a fork that did not win. That is a real possibility, not a formality,
and a receipt in that state is not a receipt.

## Verification procedure

1. Resolve `tx_reference` on the named chain.
2. Confirm inclusion against the canonical chain head, not against a
   provider's own index.
3. Recompute `content_hash` from the capsule and compare.
4. Confirm `block_reference` matches the block containing the transaction.
5. Confirm `tenant_id` matches the caller's tenant.

Any mismatch is a failure. There is no tolerance, no partial credit, and no
"close enough" — a hash either matches or the receipt is not evidence of
anything.

## What verification does not do

- It does not confirm the certificate is still valid. Validity lives in
  `compliance/rules/evidence-freshness.yaml`.
- It does not confirm the scope covers the product. That is
  `compliance/rules/certificate-scope-match.yaml`.
- It does not move the certification state machine.
- It cannot be used to conclude anything at all. It is a check on a hash.

## On revocation

Revoking a certificate does not delete its receipt. The receipt remains true —
that hash was recorded and has not changed. What changes is that the thing the
receipt refers to is no longer something anyone should rely on. Consumers must
resolve the receipt against current certificate state, not treat a valid
receipt as a valid certificate.
