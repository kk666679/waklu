#!/usr/bin/env bash
set -e
FILES=(
  "/c/Users/kurni/hukaloh/waklu/HalalChain.Platform.Contracts/contracts/test/HalalPlatform.t.sol"
  "/c/Users/kurni/hukaloh/waklu/HalalChain.Platform.Contracts/contracts/test/RegressionAndSecurity.t.sol"
)
for f in "${FILES[@]}"; do
  sed -i 's/address(0xOP)/address(0x0eee)/g' "$f"
  sed -i 's/address(0xIN)/address(0x1ee1)/g' "$f"
  sed -i 's/address(0xS1)/address(0x5111)/g' "$f"
  sed -i 's/address(0xS2)/address(0x5222)/g' "$f"
  sed -i 's/address(0xNEW)/address(0x9e99)/g' "$f"
  echo "Patched: $f"
done