# Security Policy

HalalChain provides verifiable halal supply-chain records, certification
traceability, and an evidence-driven compliance pipeline. A defect in this
platform can mean one of two things: an operational outage, or a **false
halal claim** that reaches a consumer. We treat both as security issues and we
ask you to report both.

This document describes how to report a vulnerability, what we commit to once
you do, and what we ask of contributors in this repository.

---

## Reporting a vulnerability

**Do not open a public issue, pull request, or social media post for a security
bug.** Report it privately to:

```
security@halalchain.xyz
```

- Subject line: `[SECURITY] <component> — <one-line summary>`
- Encrypt your mail if you want to. Ask us for a one-time PGP key; we do not
  publish a long-term one.
- For non-sensitive questions about *reporting* (is this in scope? is this
  version supported?) use `support@halalchain.xyz`.
- For a suspected compromise of production data, credentials, or funds, mark
  the subject `[SECURITY] URGENT` and include the P0 on-call rotation
  (`PagerDuty` per `infrastructure/alertmanager/alertmanager.yml`).

### What to include

The more of the following you can supply, the faster we can triage:

| Item | Why it helps |
| --- | --- |
| Affected component, path, and (if relevant) Solidity contract or line numbers | Routing to the owning team via [`CODEOWNERS`](CODEOWNERS) |
| Description of the flaw and the security property it breaks | Separates a real issue from a bug report |
| Reproduction steps or a proof of concept | Confirms impact without guesswork |
| Demonstrated impact — data exposed, verdict falsified, privilege gained, funds moved | We triage on impact, not on scanner severity |
| Logs, screenshots, transaction hashes, request IDs | Lets us find the event in our own telemetry |
| Your environment (.NET / Python / Node versions, `AI_BACKEND`, deployment mode) | Many findings are mode-specific |
| Your handle and preferred contact method | Required if you want credit |

If you cannot demonstrate impact yet, say so plainly and describe what you
tried. That is still a valid report.

### What to expect from us

| Commitment | Target |
| --- | --- |
| Acknowledge receipt | 3 business days |
| Initial assessment and severity rating | 7 business days |
| Progress updates during remediation | Continuous until closed |
| Public credit | On request, at your discretion |

We will tell you the severity we assigned, what we believe is affected, and
whether we accept the report as a vulnerability. If we disagree, we will say
why.

### Safe harbor

We will not pursue legal action against researchers who act in good faith,
follow this policy, and stay within the limits below. We will not terminate
access, employment, or contributor agreements for reports made in good faith.

Permitted, without prior approval:

- Reading and running code you already have access to.
- Local experiments in a workspace you control.
- Passive measurement and observation of requests you can already make.
- Reporting results, including automation output, **if** you also state what
  you believe the impact is.

Not permitted:

- Accessing data that is not yours, or that you would need a real vulnerability
  to reach.
- Persistent changes to production: no phishing, no injected content, no
  modified records, no left-behind access.
- Denial of service, resource exhaustion, or sustained load generation.
- Social engineering of staff, vendors, or users.
- Exfiltrating, modifying, or destroying customer, certification, or evidence
  data.
- Reading or exfiltrating secrets that are not yours to test with.

If you are unsure whether an action crosses a line, stop and ask us. We will
not treat a good-faith question as a violation.

---

## Scope

### In scope

Anything HalalChain operates, deploys, or publishes under the organization:

| Area | Includes |
| --- | --- |
| Platform services | `HalalChain.Platform.Api`, `HalalChain.Web`, `HalalChain.Marketplace`, `HalalChain.Platform.Http` |
| Domain and application logic | `HalalChain.Domain`, `HalalChain.Application`, `HalalChain.Storage`, `HalalChain.DataFlow`, `HalalChain.Automation` |
| AI and evidence services | `.halalchain/ai-inference`, `.halalchain/tawheed`, `.halalchain/agents`, `.halalchain/local-models`, `.halalchain/_shared` |
| Agent surfaces | `HalalChain.Agents` (eval DAG), `HalalChain.Agents` runtime client, `HalalChain.Mcp` (Model Context Protocol server), `HalalChain-Cli` |
| Smart contracts | `HalalChain.Platform.Contracts/contracts/` and anything deployed from it |
| Infrastructure | `Dockerfile`, `docker-compose.yml`, `infrastructure/`, `service-manifest.yaml`, CI workflows under `.github/workflows/` |
| Build and supply chain | `global.json`, `Directory.Build.props`, `Directory.Build.targets`, `package.json` / lockfiles, `.halalchain/requirements/*.txt`, container base images |
| Public repositories | Every repository under the HalalChain organization |
| Data planes | Certification, traceability, IoT evidence ingestion, and reporting pipelines |

### Out of scope

- Findings that require access you are not entitled to (another tenant's data,
  a production host, an administrator account).
- Vulnerabilities in third-party services, platforms, or dependencies that we do
  not control or fork — report those upstream. Report them here too **only** if
  our deployment, configuration, or pinning turns them exploitable.
- Social engineering, physical security, or lost/stolen devices.
- Denial-of-service and resource-exhaustion attacks.
- Automated scanning output with no demonstrated impact.
- Issues that require unrealistic user interaction, or that only manifest with
  local privileged access you obtained yourself.
- Findings already known and tracked internally when reported to us.
- Style, documentation, and "the wording could be clearer" feedback — open a
  normal issue or pull request for those.
- Missing hardening headers or "best practice" findings on a demo-only path
  with no production data flow.

### Severity modifiers

Two findings in this codebase can have very different real-world impact:

- **Verdict integrity is critical.** The platform's core promise is that the
  deterministic Policy Engine decides halal status and an LLM never assigns or
  overrides a verdict. Any path that lets model output, prompt content, or
  ingested evidence influence a `HalalStatus` without passing the Policy Engine
  is a **critical** finding, even if the path looks narrow.
- **Record tampering is critical.** A report that would let a vendor or agent
  alter another vendor's certification record, evidence bundle, or audit trail
  outranks a same-class bug in an internal tool.
- **Demo mode is lower severity.** With `DEMO_MODE=true` and the `local`
  `AI_BACKEND` there is no production data and no external LLM. Findings that
  only exist in that configuration are real but are ranked below the same bug
  on a production path. Say which mode you tested in.

---

## Security invariants

These are the properties this platform is designed to hold. Violations are
treated as vulnerabilities, not as feature requests.

1. **Agents collect evidence; the Policy Engine decides.** No LLM output,
   model configuration, prompt, or retrieved document may set or override a
   halal status. The rule is enforced in the `Modelfile` system prompt and in
   the API design of `HalalChain.Platform.Api/Modules/Halal/`, which forwards
   to `tawheed` for evidence and never for verdicts.
2. **Evidence is tamper-evident.** Evidence bundles and their provenance must
   be attributable to a source and verifiable after the fact. Anything that
   lets evidence be forged, relabeled, or silently dropped is critical.
3. **Tenant isolation.** Vendors, marketplace tenants, and organizations must
   not read or mutate each other's data through the API, MCP, CLI, or UI.
   Cross-tenant access is a critical finding even if it is read-only.
4. **Secrets come from the environment.** No real secret in `appsettings.json`,
   `.halalchain/config.json`, `.env`, or any committed file. Non-development
   deployments must set `ASPNETCORE_ENVIRONMENT=Production`; the platform
   rejects the placeholder `Jwt:Key` outside Development, and `ai-inference`
   refuses to start without `AI_GATEWAY_API_KEY`.
5. **Least privilege by default.** Default CORS allow-lists, rate limits, and
   role checks are part of the baseline in
   [`docs/SECURITY-BASELINE.md`](docs/SECURITY-BASELINE.md), not optional
   hardening.

---

## Coordinated disclosure

We follow coordinated disclosure. The default window is **90 days from
acknowledgement**, and it ends earlier when:

- a fix ships;
- we agree a different timeline with you; or
- active exploitation, a regulator, or a legal obligation requires faster
  action.

If remediation is genuinely complex we will ask for an extension before the
window closes and keep you posted — silence is not our way of asking.

Before a fix is deployed we will, where operationally possible:

- backport to the supported release line and tell you when it lands;
- avoid silently working around your reproduction, so you can verify the fix;
- publish a release note and, for high-impact issues, an advisory
  (GHSA/CVE when applicable);
- roll back rather than leave you reporting against a regression.

---

## Supported versions

| Version | Security fixes |
| --- | --- |
| Default branch (`main`) | Yes |
| Latest tagged release | Yes |
| Older releases | No |
| Unmaintained forks | No |

The platform is pre-production, so the default branch is usually ahead of any
release. If you are unsure whether what you found is still supported, ask
`support@halalchain.xyz` before reporting.

---

## For contributors

Working on HalalChain? Treat the invariants above as acceptance criteria.

- Never commit secrets, API keys, private keys, certificates, or `.env` files.
  `.env.example`, `.env.observability.example`, and
  `infrastructure/alertmanager/secrets/` are templates and gitignored
  directories respectively — keep it that way.
- Generate development secrets locally (`openssl rand -base64 48`) and inject
  them through the environment, not through checked-in config.
- Pin dependencies and container base images to specific versions; review
  advisories for NuGet, npm, pip, and Foundry dependencies. Vendored
  `lib/openzeppelin-contracts` has its own disclosure process — read it before
  reporting anything inside `lib/`.
- Require review from the owning team in [`CODEOWNERS`](CODEOWNERS) before
  merging changes to authentication, authorization, CORS, rate limiting, secret
  loading, contract code, or the Halal module.
- Validate and encode all input; never build SQL, shell, or Solidity from
  concatenated untrusted values.
- Encrypt sensitive data in transit and at rest. Personal data, vendor data,
  and evidence bundles must not land in logs, traces, or error messages.
- Log security-relevant events (auth failures, authorization denials, evidence
  mutations, admin actions, key rotation) without logging secrets or payloads.
- Keep the architecture tests green — `HalalChain.Architecture.Tests` encodes
  layering and dependency rules that security fixes depend on.
- Report a vulnerability you find in our own code to `security@halalchain.xyz`,
  not through the pull request that fixes it.

To report a vulnerability in a **dependency**, report it upstream first. If our
pinning or deployment makes it exploitable here, tell us privately as well.

---

## Bug bounty

There is currently no paid bug bounty program. We will not offer payment for
reports unless that changes and we announce it. We will credit you publicly on
request.

---

## Related documents

- [`docs/SECURITY-BASELINE.md`](docs/SECURITY-BASELINE.md) — auth, CORS, rate
  limiting, secret loading, key rotation.
- [`CODEOWNERS`](CODEOWNERS) — routing and required reviewers.
- [`AGENTS.md`](AGENTS.md) — build, test, and architecture principles.
- [`docs/runbooks/`](docs/runbooks/) — incident and release procedures,
  including `compromised-key.md` and `determinism-violation.md`, which cover the
  two failure modes most likely to reach you as a report.
- [`LICENSE`](LICENSE) — `legal@halalchain.xyz` (inquiries),
  `dpo@halalchain.xyz` (data protection), `compliance@halalchain.xyz`
  (certification compliance).

## Contact

| Topic | Address |
| --- | --- |
| Vulnerabilities and incidents | `security@halalchain.xyz` |
| Reporting questions, non-sensitive | `support@halalchain.xyz` |
| Legal inquiries | `legal@halalchain.xyz` |
| Licensing | `licensing@halalchain.xyz` |
| Data protection | `dpo@halalchain.xyz` |
| Certification compliance | `compliance@halalchain.xyz` |
| Community conduct | HalalChain Code of Conduct (see the project README) |