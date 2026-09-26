#!/usr/bin/env bash
set -e
T1="/c/Users/kurni/hukaloh/waklu/HalalChain.Platform.Contracts/contracts/test/HalalPlatform.t.sol"
T2="/c/Users/kurni/hukaloh/waklu/HalalChain.Platform.Contracts/contracts/test/RegressionAndSecurity.t.sol"
for f in "$T1" "$T2"; do
  sed -i 's/SupplierRegistry\.SupplierStatus/ISupplierRegistry.SupplierStatus/g' "$f"
  sed -i 's/HalalProductRegistry\.ProductStatus/IHalalProductRegistry.ProductStatus/g' "$f"
  sed -i 's/HalalProductRegistry\.Product /IHalalProductRegistry.Product /g' "$f"
  grep -q ISupplierRegistry "$f" || sed -i 's/import {SupplierRegistry}/import {SupplierRegistry, ISupplierRegistry}/' "$f"
  grep -q IHalalProductRegistry "$f" || sed -i 's/import {HalalProductRegistry}/import {HalalProductRegistry, IHalalProductRegistry}/' "$f"
  echo Patched: "$f"
done
echo All done
