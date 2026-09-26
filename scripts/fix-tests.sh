#!/usr/bin/env bash
set -e

T1="/c/Users/kurni/hukaloh/waklu/HalalChain.Platform.Contracts/contracts/test/HalalPlatform.t.sol"
T2="/c/Users/kurni/hukaloh/waklu/HalalChain.Platform.Contracts/contracts/test/RegressionAndSecurity.t.sol"

for f in "$T1" "$T2"; do
  # Fix enum references: ContractName.EnumType -> IInterfaceName.EnumType
  sed -i 's/SupplierRegistry\.SupplierStatus/ISupplierRegistry.SupplierStatus/g' "$f"
  sed -i 's/HalalProductRegistry\.ProductStatus/IHalalProductRegistry.ProductStatus/g' "$f"
  sed -i 's/HalalProductRegistry\.Product /IHalalProductRegistry.Product /g' "$f"
  sed -i 's/HalalCertificationRegistry\.CertificateStatus/IHalalCertificationRegistry.CertificateStatus/g' "$f"
  sed -i 's/HalalCertificationRegistry\.Certificate /IHalalCertificationRegistry.Certificate /g' "$f"
  echo "Patched enums: $f"
done

# Ensure ISupplierRegistry and IHalalProductRegistry are imported in both test files
# HalalPlatform.t.sol already imports IHalalCertificationRegistry; add the others
for f in "$T1" "$T2"; do
  # Add ISupplierRegistry import if missing
  if ! grep -q "ISupplierRegistry" "$f"; then
    sed -i 's|import {SupplierRegistry} from "../src/SupplierRegistry.sol";|import {SupplierRegistry, ISupplierRegistry} from "../src/SupplierRegistry.sol";|' "$f"
    echo "Added ISupplierRegistry import: $f"
  fi
  # Add IHalalProductRegistry import if missing
  if ! grep -q "IHalalProductRegistry" "$f"; then
    sed -i 's|import {HalalProductRegistry} from "../src/HalalProductRegistry.sol";|import {HalalProductRegistry, IHalalProductRegistry} from "../src/HalalProductRegistry.sol";|' "$f"
    echo "Added IHalalProductRegistry import: $f"
  fi
done

echo "All test fixes applied."