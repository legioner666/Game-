# Remote development from Android

## Candidate: GitHub Codespaces

Open this branch: https://github.com/codespaces/new?repo=legioner666%2FGame-&ref=gpt%2Fparasite-evolution-design

The repository includes a development container with a terminal, Python, Git and the official Unity CLI installer. Its post-create command runs the source-file checks. It does not automatically accept a license, sign in, create a Unity Cloud project, install paid resources or download the Editor. The existing Godot project is left intact.

As checked on 2026-11-15, GitHub Free personal accounts include 120 core-hours/month (about 60 running hours on two cores) and 15 GB-month of storage. These are separate allowances. A large Unity installation can consume the storage allowance quickly; this configuration does not establish that a full Android toolchain fits it. Stopped codespaces still consume storage. Delete unused instances, and stop an active instance after a session.

Before creating an instance, check the account's remaining quota and ensure that paid overage is disabled (a budget with **Stop usage when budget limit is reached**, or no payment method). No account quota or billing settings have been verified by this project. Do not select a paid larger machine to resolve a capacity error.

Sources:
- https://docs.github.com/en/billing/reference/product-usage-included
- https://docs.github.com/en/billing/concepts/product-billing/github-codespaces
- https://docs.github.com/en/codespaces/troubleshooting/troubleshooting-included-usage

## What the setup does

`bash parasite-evolution/Tools/setup_remote.sh` installs Unity CLI into the user's data directory and runs `Tools/check_source.py`. Re-running skips an existing CLI binary. It uses Unity's official HTTPS installer, which verifies the downloaded CLI checksum. It does not install agent plugins or change the game's packages.

Continue in a separately authorized development environment with its own permitted access to Unity services. The blocked `api.unity.com` connection in the ChatGPT execution environment has not been resolved; this configuration is not a proxy or tunnel for it. Do not forward credentials or authentication sessions from that environment.

The project remains pinned to Unity 6000.3.11f1. A licensed Editor plus Android Build Support/SDK/NDK/JDK are still needed. The commands and current gameplay limitations are in README.md. A browser terminal alone does not prove that the Editor, graphics or Android build will work.

## Evidence at this commit

- Development container configuration prepared; no Codespaces instance provisioned or connected.
- Shell syntax and existing static project checks verified locally.
- No confirmed remote Unity import, test run, screenshot, or APK.
- No payment method added and no billing settings changed.

The CLI connector used for this repository has no Codespaces creation or terminal-control operation. Opening the link from Android is the handoff to the owner's GitHub session, not evidence of an already running server.
