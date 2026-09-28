#!/usr/bin/env bash
set -e

T1="/c/Users/kurni/hukaloh/waklu/HalalChain.Platform.Contracts/contracts/test/HalalPlatform.t.sol"
T2="/c/Users/kurni/hukaloh/waklu/HalalChain.Platform.Contracts/contracts/test/RegressionAndSecurity.t.sol"

for f in "$T1" "$T2"; do
  # Fix mangled I*SupplierRegistry -> ISupplierRegistry (handles any number of I prefixes)
  sed -i 's/I\+SupplierRegistry\.SupplierStatus/ISupplierRegistry.SupplierStatus/g' "$f"
  sed -i 's/I\+HalalProductRegistry\.ProductStatus/IHalalProductRegistry.ProductStatus/g' "$f"
  sed -i 's/I\+HalalProductRegistry\.Product /IHalalProductRegistry.Product /g' "$f"

  # Fix imports: add named interface imports if not already present
  grep -q "ISupplierRegistry" "$f" || \
    sed -i 's|import {SupplierRegistry} from "../src/SupplierRegistry.sol";|import {SupplierRegistry, ISupplierRegistry} from "../src/SupplierRegistry.sol";|' "$f"

  grep -q "IHalalProductRegistry" "$f" || \
    sed -i 's|import {HalalProductRegistry} from "../src/HalalProductRegistry.sol";|import {HalalProductRegistry, IHalalProductRegistry} from "../src/HalalProductRegistry.sol";|' "$f"

  echo "Patched: $f"
done

echo "All done."