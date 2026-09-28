#!/usr/bin/env bash
sed -i 's|import {SupplierRegistry} from "./SupplierRegistry.sol";|import {SupplierRegistry, ISupplierRegistry} from "./SupplierRegistry.sol";|' \
  /c/Users/kurni/hukaloh/waklu/HalalChain.Platform.Contracts/contracts/src/HalalProductRegistry.sol
echo "done"