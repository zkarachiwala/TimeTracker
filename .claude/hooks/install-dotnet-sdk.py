#!/usr/bin/env python3
"""Installs a .NET SDK satisfying the version pinned in global.json, for
remote Claude Code sessions.

Two tiers, tried in order:

1. apt (dotnet-sdk-10.0). Ubuntu's package tracks the .1xx SDK feature band
   closely and gets ordinary patches quickly (it's currently ahead of what
   Microsoft Container Registry publishes for that band - see tier 2). It
   can never provide a newer feature band at all though: Canonical policy
   permanently freezes it at .1xx (dotnet/core#9258).

2. Microsoft Container Registry (mcr.microsoft.com), only if apt didn't
   satisfy the pin - i.e. global.json asks for a feature band apt doesn't
   have. Microsoft's own SDK binary CDN (builds.dotnet.microsoft.com,
   dotnetcli.azureedge.net, etc.) is blocked by this sandbox's network
   policy - a known, currently-unfixed gap (anthropics/claude-code#11897) -
   but MCR (a different set of hosts, including its blob storage backend at
   *.data.mcr.microsoft.com) is reachable. Microsoft publishes the SDK as a
   Docker image, so this pulls that image's layers directly over the
   registry's plain HTTP(S) API - no Docker daemon required - and extracts
   the SDK payload from them. MCR only keeps a handful of patches per band
   before moving on to the next one, so it's a fallback for reaching a new
   band, not a replacement for apt's more current in-band patch tracking.
"""
import json
import os
import platform
import shutil
import subprocess
import sys
import tarfile
import tempfile

INSTALL_DIR = "/opt/dotnet-sdk"
REGISTRY = "https://mcr.microsoft.com/v2/dotnet/sdk"


def read_global_json_version(project_dir: str) -> str:
    path = os.path.join(project_dir, "global.json")
    with open(path) as f:
        data = json.load(f)
    return data["sdk"]["version"]


def parse_version(v: str) -> tuple[int, int, int]:
    parts = v.split(".")
    return (int(parts[0]), int(parts[1]), int(parts[2]))


def satisfies(actual: str, floor: str) -> bool:
    """rollForward: latestFeature semantics - same major.minor, patch/feature-band
    number at or above the floor. (Exact-match policies aren't in use in this
    repo; if they were, this would need to also check the caller's rollForward
    setting instead of assuming latestFeature.)"""
    a, f = parse_version(actual), parse_version(floor)
    return a[0] == f[0] and a[1] == f[1] and a[2] >= f[2]


def current_dotnet_version() -> str | None:
    dotnet_bin = shutil.which("dotnet")
    if not dotnet_bin:
        return None
    try:
        out = subprocess.run(
            [dotnet_bin, "--version"], capture_output=True, text=True, timeout=15
        )
        return out.stdout.strip() if out.returncode == 0 else None
    except Exception:
        return None


def try_apt_install() -> str | None:
    print("Trying apt for the pinned SDK version...")
    try:
        with open("/etc/os-release") as f:
            os_release = dict(
                line.strip().split("=", 1) for line in f if "=" in line and not line.startswith("#")
            )
        version_id = os_release.get("VERSION_ID", "").strip('"')

        deb_path = "/tmp/packages-microsoft-prod.deb"
        subprocess.run(
            [
                "curl", "-fsSL",
                f"https://packages.microsoft.com/config/ubuntu/{version_id}/packages-microsoft-prod.deb",
                "-o", deb_path,
            ],
            check=True, timeout=60,
        )
        subprocess.run(["dpkg", "-i", deb_path], check=True, timeout=60)
        os.remove(deb_path)
        subprocess.run(["apt-get", "update", "-qq"], timeout=120)
        subprocess.run(["apt-get", "install", "-y", "dotnet-sdk-10.0", "-qq"], check=True, timeout=180)
    except Exception as e:
        print(f"  apt install did not succeed ({e}); continuing to check what's available.")

    return current_dotnet_version()


def arch_suffix() -> str:
    machine = platform.machine()
    if machine in ("x86_64", "amd64"):
        return "amd64"
    if machine in ("aarch64", "arm64"):
        return "arm64v8"
    raise RuntimeError(f"Unsupported architecture for mcr.microsoft.com/dotnet/sdk tags: {machine}")


def curl_json(url: str, extra_headers: list[str] | None = None) -> dict:
    cmd = ["curl", "-fsSL"]
    for h in extra_headers or []:
        cmd += ["-H", h]
    cmd.append(url)
    result = subprocess.run(cmd, capture_output=True, text=True, timeout=60)
    if result.returncode != 0:
        raise RuntimeError(
            f"curl failed fetching {url} (exit {result.returncode}): {result.stderr.strip()}"
        )
    return json.loads(result.stdout)


def curl_download(url: str, dest: str) -> None:
    result = subprocess.run(
        ["curl", "-fsSL", "-o", dest, url], capture_output=True, text=True, timeout=300
    )
    if result.returncode != 0:
        raise RuntimeError(
            f"curl failed downloading {url} (exit {result.returncode}): {result.stderr.strip()}"
        )


def apply_layer(layer_path: str, rootfs: str) -> None:
    """Extracts one OCI/Docker image layer onto rootfs, honoring whiteout
    entries (files docker uses to mark deletions from lower layers) so the
    merged result matches what a real container filesystem would look like."""
    with tarfile.open(layer_path, "r:gz") as tf:
        members = tf.getmembers()
        whiteouts = []
        real_members = []
        for m in members:
            base = os.path.basename(m.name)
            if base == ".wh..wh..opq":
                whiteouts.append(("opaque", os.path.dirname(m.name)))
            elif base.startswith(".wh."):
                whiteouts.append(("delete", os.path.join(os.path.dirname(m.name), base[4:])))
            else:
                real_members.append(m)

        for kind, path in whiteouts:
            full = os.path.join(rootfs, path)
            if kind == "delete":
                if os.path.isdir(full) and not os.path.islink(full):
                    shutil.rmtree(full, ignore_errors=True)
                elif os.path.exists(full) or os.path.islink(full):
                    os.remove(full)
            elif kind == "opaque":
                if os.path.isdir(full):
                    for entry in os.listdir(full):
                        p = os.path.join(full, entry)
                        if os.path.isdir(p) and not os.path.islink(p):
                            shutil.rmtree(p, ignore_errors=True)
                        else:
                            os.remove(p)

        # filter="tar": the stricter default "data" filter rejects the absolute
        # symlinks a real base-OS layer legitimately contains (e.g.
        # /etc/alternatives/*). Source is Microsoft's own signed-over-HTTPS
        # image, not untrusted input.
        tf.extractall(rootfs, members=real_members, filter="tar")


def install_from_mcr(version: str) -> None:
    tag = f"{version}-noble-{arch_suffix()}"
    print(f"Falling back to Microsoft Container Registry: mcr.microsoft.com/dotnet/sdk:{tag} ...")

    manifest = curl_json(
        f"{REGISTRY}/manifests/{tag}",
        extra_headers=[
            "Accept: application/vnd.docker.distribution.manifest.v2+json",
            "Accept: application/vnd.oci.image.manifest.v1+json",
        ],
    )
    layers = manifest["layers"]

    with tempfile.TemporaryDirectory() as tmp:
        rootfs = os.path.join(tmp, "rootfs")
        os.makedirs(rootfs, exist_ok=True)

        for i, layer in enumerate(layers):
            digest = layer["digest"]
            layer_path = os.path.join(tmp, f"layer-{i:02d}.tar.gz")
            print(f"  Layer {i + 1}/{len(layers)}: {layer['size']:,} bytes")
            curl_download(f"{REGISTRY}/blobs/{digest}", layer_path)
            apply_layer(layer_path, rootfs)
            os.remove(layer_path)

        sdk_src = os.path.join(rootfs, "usr", "share", "dotnet")
        if not os.path.isdir(sdk_src):
            raise RuntimeError(f"Expected SDK at {sdk_src} inside the image but it wasn't found")

        if os.path.isdir(INSTALL_DIR):
            shutil.rmtree(INSTALL_DIR)
        shutil.copytree(sdk_src, INSTALL_DIR, symlinks=True)

    dotnet_bin = os.path.join(INSTALL_DIR, "dotnet")
    os.chmod(dotnet_bin, 0o755)

    # Point /usr/bin/dotnet at the new install so every shell picks it up
    # with no PATH change needed - same location apt occupies.
    usr_bin_dotnet = "/usr/bin/dotnet"
    if os.path.islink(usr_bin_dotnet) or os.path.exists(usr_bin_dotnet):
        os.remove(usr_bin_dotnet)
    os.symlink(dotnet_bin, usr_bin_dotnet)


def main() -> None:
    project_dir = sys.argv[1] if len(sys.argv) > 1 else os.environ.get("CLAUDE_PROJECT_DIR", ".")
    floor = read_global_json_version(project_dir)

    existing = current_dotnet_version()
    if existing and satisfies(existing, floor):
        print(f".NET SDK {existing} already present and satisfies pinned floor {floor}.")
        return

    apt_version = try_apt_install()
    if apt_version and satisfies(apt_version, floor):
        print(f"apt provided .NET SDK {apt_version}, satisfies pinned floor {floor}.")
        return

    print(
        f"apt's SDK ({apt_version}) doesn't satisfy the pinned floor {floor} "
        "(different feature band) - falling back to Microsoft Container Registry."
    )
    install_from_mcr(floor)
    final = current_dotnet_version()
    if not final or not satisfies(final, floor):
        raise RuntimeError(
            f"Installed SDK ({final}) still doesn't satisfy pinned floor {floor} after MCR fallback."
        )
    print(f".NET SDK {final} installed successfully via Microsoft Container Registry.")


if __name__ == "__main__":
    main()
