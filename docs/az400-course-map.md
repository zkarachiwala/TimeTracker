# AZ-400 Course Map

> **Course credit:** This map is based on the Udemy course
> **AZ-400: Designing and Implementing Microsoft DevOps Solutions** by James Lee.
> All hands-on GitHub issues in this repository reference lectures from that course.
> Course content belongs to its respective instructor and Udemy. This map is a personal study aide only.

> **Exam cross-reference:** Cross-referenced against the official
> [AZ-400 Study Guide (July 27, 2026)](https://learn.microsoft.com/en-au/credentials/certifications/resources/study-guides/az-400)
> and the [AZ-400T00 official course outline](https://learn.microsoft.com/en-us/training/courses/az-400t00).
> The current exam weights GitHub and Azure DevOps **equally** — James Lee's course is Azure DevOps-heavy.
> Entries marked ⚠️ are outdated/deprecated in the current exam.
> A **"Gaps" section at the bottom** lists exam topics NOT covered by the James Lee course.

> **Video guide:** ❌ SKIP markers reflect recommendations from a Gemini-generated study plan
> identifying which James Lee lectures are outdated given the course's age. These are video
> watch decisions only — separate from the hands-on issue strategy.

## Legend
- ✅  Covered by an existing issue
- 🔧  Needs a new issue (gap)
- 📖  Watch / study — concept knowledge, no hands-on issue needed for this project
- ⚠️   Deprecated — skip the demo; read the concept only
- ❌  SKIP — outdated video; replaced by MS Learn docs or MS-native tooling
- 🔵  Paid tier required — watch concept videos, no hands-on feasible

---

## SECTION: Design and Implement Source Control
*Exam area: Design and implement a source control strategy (10–15%)*

| Lecture | Issue | Notes |
|---|---|---|
| (OPTIONAL) Under the Hood of Git | 📖 | Git internals — useful background |
| (OPTIONAL) Demo - Take a Look Under the Hood of Git | 📖 | |
| Azure Repos Overview | ✅ | #254 |
| Demo - Manage Azure Repos | ✅ | #254 |
| Demo - Access Azure Repos with SSH | ✅ | #254 |
| Demo - Access Azure Repos with a PAT | ✅ | #254 — note: PAT is legacy; OIDC (#246) is the exam-preferred modern approach |
| GitHub Repositories Overview | 📖 | Already your primary workflow |
| Demo - Manually Mirror a Git Repo | 🔧 | New issue — mirror between Azure Repos ↔ GitHub |
| Collaborating with Git | 📖 | Standard git workflow |
| Branching Strategies | ✅ | #230 — trunk-based, feature branch, release branch (all three in the exam) |
| Merging Strategies | ✅ | #230 — squash vs merge vs rebase |
| Demo - Create and Merge a PR in Azure Repos | ✅ | #254 |
| Azure Repos Branch Policies | ✅ | #254 |
| Demo - Configure Azure Repos Branch Policies | ✅ | #254 |
| Git Tags | ✅ | #231 — SemVer tagging |
| Demo - Add a Git Tag and View in Azure DevOps | ✅ | #231 |

---

## SECTION: Design and Implement Build Pipelines
*Exam area: Design and implement build and release pipelines (50–55%)*

| Lecture | Issue | Notes |
|---|---|---|
| Azure Pipelines Overview | ✅ | #252 |
| Demo - Configure Resources for your Azure Pipelines | ✅ | #252 |
| Azure Pipelines Configuration | ✅ | #252 |
| Demo - Create a Build Pipeline with Azure Pipelines (YAML) | ✅ | #252 |
| Demo - Configure a CI Trigger in Azure Pipelines (YAML) | ✅ | #252 |
| Azure Pipelines Agents and Pools | ✅ | #252 / #238 — exam tests agent infrastructure: cost, tool selection, connectivity |
| Azure Pipelines Artifacts | ✅ | #255 |
| Demo - Publish Build Artifacts in Azure Pipelines (YAML) | ✅ | #255 |
| Azure Pipelines Self-Hosted Agent Setup | ✅ | #238 — Docker-based runner |
| Demo - Create a Build Pipeline in Azure Pipelines (Classic) | ⚠️ | Classic deprecated — exam tests *migrating* Classic → YAML (see gaps below) |
| GitHub Actions Overview | 📖 | Already your primary CI platform |
| Demo - Create a Build Workflow with GitHub Actions | 📖 | Already done in existing CI workflows |

---

## SECTION: Design and Implement Package Management
*Exam area: Design and implement a package management strategy*

| Lecture | Issue | Notes |
|---|---|---|
| Package Management Overview | ✅ | #233 / #255 |
| Demo - Create a Package | ✅ | #233 — TimeTracker.Contracts to GitHub Packages |
| Azure Artifacts | ✅ | #255 |
| Demo - Publish a Package to Azure Artifacts | ✅ | #255 |
| Demo - Use Azure Artifacts from Azure Pipelines (YAML) | ✅ | #255 |
| Demo - Configure Upstream Sources | ✅ | #255 |
| Azure Pipelines Caching | 🔧 | New issue — cache NuGet packages in Azure Pipelines YAML |
| Demo - Configure Azure Pipelines Caching | 🔧 | Part of same issue |
| Package Versioning Strategies | ✅ | #231 — exam explicitly tests SemVer and CalVer |

---

## SECTION: Design and Implement Release Pipelines
*Exam area: Design and implement build and release pipelines (50–55%)*

| Lecture | Issue | Notes |
|---|---|---|
| Release Pipelines Overview | ✅ | #252 / #256 |
| Azure Pipelines Parallel Jobs and Stages | ✅ | #256 — exam tests multi-stage, parallelism |
| Demo - Deploy to App Service in Azure Pipelines (YAML) | ✅ | #256 |
| Demo - Use Stages in Azure Pipelines (YAML) | ✅ | #256 |
| Azure Pipelines Variables | 🔧 | New issue — variable groups, Key Vault-linked variable groups (exam explicit) |
| Demo - Use Variables in Azure Pipelines (YAML) | 🔧 | Part of same issue |
| Demo - Deploy to an Environment in Azure Pipelines (YAML) | ✅ | #256 |
| Azure Pipeline Controls | ✅ | #256 — conditions, approvals, gates |
| Demo - Configure Conditions in Azure Pipelines (YAML) | ✅ | #256 |
| Demo - Configure Approvals in Azure Pipelines (YAML) | ✅ | #256 |
| Demo - Deploy to App Service in Azure Pipelines (Classic) | ⚠️ | Classic deprecated — see gaps: Classic → YAML migration is exam-tested |
| Demo - Configure Controls in Azure Pipelines (Classic) | ⚠️ | Classic deprecated |
| Demo - Explore Parallel Jobs Billing | 📖 | Awareness only |
| GitHub Actions Variables and Secrets | ✅ | #246 / #247 |
| Demo - Deploy to a Web App using GitHub Actions | 📖 | Already done in existing CD workflow |

---

## SECTION: Design and Implement Testing
*Exam area: Design and implement a testing strategy for pipelines*

| Lecture | Issue | Notes |
|---|---|---|
| Unit Tests | ✅ | #234 — exam tests unit + integration + load |
| Integration Tests | ✅ | #234 |
| Demo - Create a Unit Test Project | ✅ | #234 |
| Demo - Configure Unit Testing in Azure Pipelines (YAML) | ✅ | #234 / #252 |
| Load Tests | ✅ | #235 — k6 |
| UI Tests | 🔧 | New issue — Playwright E2E in Azure Pipelines (adapt Selenium lecture to Playwright) |
| Demo - Create a Selenium UI Test Project | 🔧 | Adapt to Playwright — same exam concept |
| Demo - Configure Selenium UI Testing in Azure Pipelines (YAML) | 🔧 | Part of same issue |
| Demo - Report on Code Coverage in Azure Pipelines (YAML) | ✅ | #234 — exam explicitly tests code coverage analysis |
| Flaky Tests | ✅ | #244 — exam tests "monitor pipeline health including flaky tests" |
| Demo - Configure Flaky Tests in Azure Pipelines | ✅ | #244 |
| Azure Test Plans | 🔵 | Watch — paid licence for hands-on but watch all three concept videos |
| Demo - Setup Azure Test Plans and a Free Trial | 🔵 | Watch |
| Demo - Create a Requirements Based Test Case | 🔵 | Watch |

---

## SECTION: Managing Application Infrastructure
*Exam area: Design and implement infrastructure as code (IaC)*

| Lecture | Issue | Notes |
|---|---|---|
| ARM Template Overview | 📖 | Watch — exam still tests ARM JSON concepts alongside Bicep |
| Demo - Deploy a Web App using an ARM Template | 📖 | Watch — Bicep (#242) is the hands-on equivalent |
| Bicep Templates | ✅ | #242 |
| Demo - Deploy a Storage Account using a Bicep Template | ✅ | #242 |
| Advanced Templates | ✅ | #242 — modules, linked templates |
| Demo - Deploy a Nested ARM Template | 📖 | Watch — Bicep modules (#242) are the hands-on equivalent |
| Demo - Deploy a Linked ARM Template | 📖 | Watch |
| Demo - Deploy an ARM Template in Azure Pipelines (YAML) | ✅ | #242 — Bicep in pipeline |
| VM Configuration Tools | 📖 | Watch — exam tests DSC / Azure Automation State Configuration |
| Demo - Automation State Configuration | 📖 | Watch |
| Azure Automanage | 📖 | Watch |

---

## SECTION: Design and Implement Deployments
*Exam area: Design and implement deployments*

| Lecture | Issue | Notes |
|---|---|---|
| Blue Green Deployments | ✅ | #241 — exam tests blue-green, canary, ring, progressive exposure, A/B |
| Rolling Deployments | 📖 | Watch — F1 has no slots but exam tests the concept |
| Ringed Deployments | 📖 | Watch — exam tests ringed/progressive exposure |
| Feature Flags | ✅ | #239 — exam explicit: "implement feature flags by using Azure App Configuration Feature Manager" |
| Deployments with Azure Load Balancer | 📖 | Watch — not applicable to F1 but exam tests the concept |
| Traffic Manager | 📖 | Watch — exam tests Traffic Manager for multi-region deployments |
| Demo - Configure Traffic Manager | 📖 | Watch |
| Deployments with Azure Traffic Manager | 📖 | Watch |
| Deployments with Azure App Service | ✅ | #241 / #256 |
| Azure App Configuration | ✅ | #239 |

---

## SECTION: Design and Implement Security and Compliance
*Exam area: Develop a security and compliance plan (10–15%)*

| Lecture | Issue | Notes |
|---|---|---|
| Key Vault | ✅ | #247 — exam tests Key Vault for secrets, keys, certificates |
| Demo - Configure and Use Key Vault from a VM | ✅ | #247 — adapt to App Service, not VM |
| Demo - Push a Container to ACR using a Key Vault Secret | 📖 | No containers in TimeTracker — study concept |
| Demo - Use Key Vault Secrets in Azure Pipelines CICD (YAML) | ✅ | #247 |
| Mend Bolt | ❌ | SKIP video — outdated third-party tool; exam tests MS-native equivalents covered by #248 and MS Learn |
| Demo - Configure Mend Bolt with Azure Pipelines (YAML) | ❌ | SKIP |
| SonarCloud | ❌ | SKIP video — exam tests quality gates via GitHub Advanced Security (#248); study concept via MS Learn |
| Demo - Configure SonarCloud with Azure Pipelines (YAML) | ❌ | SKIP |
| ZAP | ❌ | SKIP video — exam tests DAST concept; MS-native equivalent is Defender for Cloud (study-only, paid) |
| Demo - Configure OWASP ZAP with Azure Pipelines (YAML) | ❌ | SKIP |
| GitHub Code Security | ✅ | #248 — CodeQL, Dependabot, secret scanning |
| Demo - Configure GitHub Code Security | ✅ | #248 |

---

## SECTION: Optimize and Manage Source Control
*Exam area: Configure and manage repositories*

| Lecture | Issue | Notes |
|---|---|---|
| Challenges of Large Repos | 📖 | Awareness — exam tests LFS and Scalar concepts |
| Git LFS | 📖 | Watch — exam explicit: "design and implement a strategy for managing large files, including Git LFS" |
| Demo - Configure Git LFS with an Azure Repo | 📖 | Watch — no large files in TimeTracker to practice on |
| Scalar | 📖 | Watch — exam explicit: "design a strategy for scaling and optimizing a Git repository, including Scalar" |
| Demo - Working with Git Scalar | 📖 | Watch |
| Git and Deleted Data | ✅ | #232 — exam tests "recover specific data" and "remove specific data from source control" |
| Demo - Working with Git and Deleted Data | ✅ | #232 |
| Git Hooks | 🔧 | New issue — client-side hooks (commit-msg, pre-commit); exam explicit via MS Learn module "Explore Git hooks" |
| Demo - Working with Git Hooks | 🔧 | Part of same issue |
| Azure DevOps Service Hooks | 🔧 | New issue — pipeline events → Teams/webhook; exam tests "configure integration by using webhooks" |
| Demo - Configure a Service Hook with Blob Storage | 🔧 | Part of same issue |

---

## SECTION: Optimize and Manage Pipelines
*Exam area: Maintain pipelines*

| Lecture | Issue | Notes |
|---|---|---|
| Azure Pipelines Container Jobs | 🔧 | New issue — run build steps inside a container in Azure Pipelines |
| Demo - Use Container Jobs in Azure Pipelines (YAML) | 🔧 | Part of same issue |
| Azure Pipeline VMSS Agent Overview | 📖 | Watch — exam tests VMSS agent infrastructure; too expensive to run hands-on |
| Demo - Configure VMSS Agent Pool for Azure Pipelines | 📖 | Watch |
| Azure Pipelines Retention | ✅ | #245 — exam tests "design and implement a retention strategy for pipeline artifacts" |
| Demo - Exploring Azure Pipelines Retention Settings | ✅ | #245 |
| Building Modular Azure Pipelines | ✅ | #237 — exam tests "create reusable pipeline elements including YAML templates, task groups, variable groups" |
| Demo - Use Azure Pipelines Task Groups (Classic) | ⚠️ | Classic deprecated — YAML templates (#237) are the current approach |
| Demo - Use Azure Pipelines Includes Templates (YAML) | ✅ | #237 |

---

## SECTION: Monitoring and Instrumentation
*Exam area: Implement an instrumentation strategy (5–10%)*

| Lecture | Issue | Notes |
|---|---|---|
| Azure Monitor Overview | ✅ | #271 (SQL metrics, DP-800) / #250 (app telemetry, AZ-400) |
| Demo - Monitor VM Metrics | 📖 | Adapt to App Service metrics — same Azure Monitor concepts |
| Azure Monitor Logs Overview | ✅ | #251 — KQL queries |
| Demo - Azure Monitor Logs | ✅ | #251 |
| Azure Monitor Alerts | ✅ | #271 / #250 |
| Demo - Configure Azure Monitor Alerts | ✅ | #271 |
| Application Insights Overview | ✅ | #250 — exam tests "configure collection of telemetry by using Application Insights" |
| Demo - Configure App Insights (Auto) for an Azure Web App | ✅ | #250 |
| Demo - Configure App Insights (Manual) for your Azure Pipeline (YAML) | ✅ | #250 — exam tests distributed tracing |

---

## SECTION: Processes and Communications
*Exam area: Design and implement processes and communications (10–15%)*

| Lecture | Issue | Notes |
|---|---|---|
| Azure Boards | ✅ | #253 |
| Azure Boards Work Items | ✅ | #253 |
| Demo - Setup an Azure DevOps Project for Managing Work | ✅ | #253 |
| Demo - Query Work with Azure Boards | ✅ | #253 |
| Demo - Manage Work with Azure Boards | ✅ | #253 |
| Demo - Manage Work from Azure Repos | ✅ | #253 / #254 — AB# commit linking |
| Azure DevOps Dashboards | 🔧 | New issue — DORA metrics (cycle time, lead time, time to recovery) in Azure DevOps; exam explicit |
| Demo - Create an Azure DevOps Dashboard | 🔧 | Part of same issue |
| Azure DevOps Wikis | ✅ | #226 — Mermaid diagrams; exam tests "document a project by configuring wikis including Mermaid syntax" |
| Demo - Create a Provisioned Azure DevOps Wiki | 📖 | GitHub wiki used — understand the Azure DevOps equivalent |
| Demo - Create a Published (Code) Azure DevOps Wiki | 📖 | |

---

## Gaps — exam topics NOT in the James Lee course

These topics appear in the **official AZ-400 study guide (July 2026)** but have no corresponding lecture in James Lee's course. They require supplementary study or new issues.

| # | Exam objective | Issue | Notes |
|---|---|---|---|
| G1 | Repo mirroring between Azure Repos ↔ GitHub | 🔧 | "Design and implement integration between GitHub repositories and Azure Pipelines" |
| G2 | Azure Pipelines variable groups (incl. Key Vault-linked) | 🔧 | "Create reusable pipeline elements including variables and variable groups" |
| G3 | Playwright / UI tests in Azure Pipelines | 🔧 | "Implement tests in a pipeline including configuring test agents" |
| G4 | Azure Pipelines pipeline caching | 🔧 | "Optimize a pipeline for cost, time, performance" |
| G5 | Mend Bolt — SCA/dependency scanning | 📖 | Video skipped (outdated); study concept via MS Learn — MS-native equivalent is Dependabot (#248) |
| G6 | SonarCloud — code quality gate | 📖 | Video skipped (outdated); study concept via MS Learn — quality gates covered conceptually by #234 |
| G7 | OWASP ZAP — DAST in pipeline | 📖 | Video skipped (outdated); study concept via MS Learn — MS-native equivalent is Defender for Cloud |
| G8 | Git hooks (client-side) | 🔧 | MS Learn AZ-400 path has a dedicated "Explore Git hooks" module |
| G9 | Azure DevOps Service Hooks + DORA dashboard | 🔧 | "Configure integration by using webhooks" + DORA metrics in Azure DevOps |
| G10 | Classic → YAML pipeline migration | ✅ | #252 can include this — "Migrate a pipeline from classic to YAML" is exam explicit |
| G11 | Microsoft Defender for Cloud DevOps Security | 📖 | ~$15/resource/month — not feasible; study concept via MS Learn and extend #248 notes |
| G12 | GitHub Advanced Security for Azure DevOps | 📖 | $49/committer/month — not feasible; extend #248 with a study-only section on GHAzDO vs GitHub.com |
| G13 | Azure Pipelines Secure Files | ✅ | Free — #275 |
| G14 | Inner source / fork workflows | 📖 | MS Learn module: "Plan to foster inner source"; study only for personal project |
| G15 | GitHub monitoring/insights | 🔧 | Exam: "configure monitoring in GitHub, including enabling insights and creating charts" — extend #229 |
| G16 | Container image scanning in pipeline | 📖 | Exam: "automate container scanning" — no containers in TimeTracker; study concept |
| G17 | Azure Deployment Environments | ✅ | #243 — exam explicit: "design and implement Azure Deployment Environments" |
| G18 | Managed Identity vs Service Principal choice | ✅ | #249 — exam: "choose between Microsoft Entra service principals and managed identities" |

---

## Summary

| Status | Count |
|---|---|
| ✅ Covered by existing issues (#225–#256, #new) | ~90 lectures + 6 exam gaps covered |
| 🔧 Needs new issue | 8 gaps (G1–G4, G8, G9, G15, + container jobs) |
| 📖 Watch / study only | ~35 lectures + G5, G6, G7, G11, G12, G14, G16 |
| ❌ SKIP (outdated video) | 9 lectures (Classic pipelines x3, Mend/Sonar/ZAP x6) |
| 🔵 Paid tier — watch only | 3 lectures (Azure Test Plans) |

*Updated 2026-06-27. Based on James Lee's AZ-400 Udemy course cross-referenced with the official AZ-400 study guide (July 27, 2026) and AZ-400T00 course outline.*
