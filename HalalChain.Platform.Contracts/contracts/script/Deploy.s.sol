// SPDX-License-Identifier: MIT
pragma solidity 0.8.24;

import {Script, console2} from "forge-std/Script.sol";
import {HalalAccessControl} from "../src/HalalAccessControl.sol";
import {SupplierRegistry} from "../src/SupplierRegistry.sol";
import {HalalProductRegistry} from "../src/HalalProductRegistry.sol";
import {HalalCertificationRegistry} from "../src/HalalCertificationRegistry.sol";
import {TraceabilityEventLog} from "../src/TraceabilityEventLog.sol";

/**
 * @title Deploy
 * @notice Deploys the HalalChain platform contracts in the correct
 *         dependency order and wires the cross-contract references.
 *
 * Usage:
 *   # LOCAL: zero timelock, single admin, no prod guards.
 *   forge script script/Deploy.s.sol:Deploy --rpc-url http://127.0.0.1:8545 \
 *     --broadcast --sig "deployLocal()"
 *
 *   # TESTNET: explicit timelock (must be >= 0).
 *   forge script script/Deploy.s.sol:Deploy --rpc-url $AMOY --broadcast \
 *     --sig "deployTestnet(uint64)" -- 86400
 *
 *   # MAINNET: minimum 24h timelock enforced.
 *   forge script script/Deploy.s.sol:Deploy --rpc-url $POLYGON --broadcast \
 *     --sig "deployMainnet(uint64)" -- 86400
 *
 * Admin rotation policy
 * ----------------------
 *   The default admin is set to ``msg.sender`` (the deployer EOA /
 *   multisig). After the contracts are live the operator MUST:
 *     1. Rotate the admin to a hardware-wallet multisig (separate from
 *        the deployer) via ``AccessControl.grantRole`` followed by
 *        ``AccessControl.renounceRole``.
 *     2. Grant the ``PAUSER_ROLE`` to a separate emergency-pause
 *        multisig and renounce it from the admin.
 *     3. Revoke any temporary ``CERTIFIER_ROLE`` granted during the
 *        smoke test before opening public registration.
 */
contract Deploy is Script {
    struct Deployment {
        address access;
        address suppliers;
        address products;
        address certs;
        address events;
    }

    Deployment public deployment;

    function run() external {
        // The default entry-point refuses to guess a deployment mode.
        // Operators MUST call deployLocal / deployTestnet / deployMainnet
        // explicitly so the timelock and admin role are intentionally
        // chosen rather than silently defaulting to a 0-timelock
        // local-mode deployment.
        revert(
            "Deploy.run() is intentionally disabled. Call deployLocal(), "
            "deployTestnet(uint64), or deployMainnet(uint64) explicitly."
        );
    }

    function deployLocal() public {
        uint64 timelock = 0;
        vm.startBroadcast();
        _deploy(timelock);
        vm.stopBroadcast();
    }

    function deployTestnet(uint64 timelockSeconds) public {
        if (timelockSeconds > 7 days) {
            // Cap testnet timelocks to one week. Production has a higher
            // minimum; this guard catches "fat-finger" deploys.
            revert("testnet timelock must be <= 7 days");
        }
        vm.startBroadcast();
        _deploy(timelockSeconds);
        vm.stopBroadcast();
    }

    function deployMainnet(uint64 timelockSeconds) public {
        if (timelockSeconds < 24 hours) {
            revert("mainnet timelock must be >= 24h");
        }
        if (timelockSeconds > 30 days) {
            revert("mainnet timelock must be <= 30 days");
        }
        vm.startBroadcast();
        _deploy(timelockSeconds);
        vm.stopBroadcast();
    }

    function _deploy(uint64 timelockSeconds) internal {
        address deployer = msg.sender;
        if (deployer == address(0)) revert("Deployer is zero address");
        console2.log("Deployer:", deployer);
        console2.log("Timelock seconds:", timelockSeconds);

        HalalAccessControl access = new HalalAccessControl(deployer, timelockSeconds);
        SupplierRegistry suppliers = new SupplierRegistry(access);
        HalalProductRegistry products = new HalalProductRegistry(access, suppliers);
        HalalCertificationRegistry certs = new HalalCertificationRegistry(access, products);
        TraceabilityEventLog events = new TraceabilityEventLog(access, products);

        // Wire the cert registry as the authorised caller on the product registry
        products.setCertificationRegistry(address(certs));

        deployment = Deployment({
            access: address(access),
            suppliers: address(suppliers),
            products: address(products),
            certs: address(certs),
            events: address(events)
        });

        console2.log("== HalalChain platform deployed ==");
        console2.log("HalalAccessControl       :", address(access));
        console2.log("SupplierRegistry         :", address(suppliers));
        console2.log("HalalProductRegistry     :", address(products));
        console2.log("HalalCertificationRegistry:", address(certs));
        console2.log("TraceabilityEventLog     :", address(events));
        console2.log("");
        console2.log("POST-DEPLOY ADMIN ROTATION STEPS:");
        console2.log("1. Grant DEFAULT_ADMIN_ROLE to a hardware-wallet multisig");
        console2.log("2. Renounce DEFAULT_ADMIN_ROLE from the deployer EOA");
        console2.log("3. Grant PAUSER_ROLE to a separate emergency-pause multisig");
        console2.log("4. Renounce PAUSER_ROLE from the admin");
        console2.log("5. Grant PLATFORM_OPERATOR_ROLE to the backend hot wallet (via the 2-step propose/execute)");
        console2.log("6. Grant CERTIFIER_ROLE to each certifier body (via the 2-step propose/execute)");
        console2.log("7. Save the addresses to infrastructure/deployments/<network>.json");
    }
}
