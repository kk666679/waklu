#!/usr/bin/env bash
set -e
F="/c/Users/kurni/hukaloh/waklu/HalalChain.Platform.Contracts/contracts/test/HalalPlatform.t.sol"

# Fix IIIHalalCertificationRegistry -> IHalalCertificationRegistry (any number of leading I's before Halal)
sed -i 's/I\+HalalCertificationRegistry\./IHalalCertificationRegistry./g' "$F"

# Add ISupplierRegistry import if missing
grep -q "ISupplierRegistry" "$F" || \
  sed -i 's|import {SupplierRegistry} from "../src/SupplierRegistry.sol";|import {SupplierRegistry, ISupplierRegistry} from "../src/SupplierRegistry.sol";|' "$F"

# Add IHalalProductRegistry import if missing
grep -q "IHalalProductRegistry" "$F" || \
  sed -i 's|import {HalalProductRegistry} from "../src/HalalProductRegistry.sol";|import {HalalProductRegistry, IHalalProductRegistry} from "../src/HalalProductRegistry.sol";|' "$F"

echo "HalalPlatform.t.sol fixed."