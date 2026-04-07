#!/bin/sh
# EchoHub Client Installer
# Usage: curl -sSfL https://raw.githubusercontent.com/HueByte/EchoHub/master/scripts/install.sh | sh
#
# Options (pass as arguments or environment variables):
#   --version X.Y.Z    Install a specific version (default: latest)
#   --install-dir DIR  Install to a custom directory
#   --help             Show this help message

set -eu

REPO="HueByte/EchoHub"
BINARY_NAME="echohub"
INSTALL_DIR=""
VERSION=""

# ── Argument parsing ──────────────────────────────────────────────────────

while [ $# -gt 0 ]; do
    case "$1" in
        --version)
            VERSION="$2"
            shift 2
            ;;
        --install-dir)
            INSTALL_DIR="$2"
            shift 2
            ;;
        --help)
            sed -n '2,8p' "$0" 2>/dev/null || true
            echo ""
            echo "  curl -sSfL https://raw.githubusercontent.com/$REPO/master/scripts/install.sh | sh"
            echo "  curl ... | sh -s -- --version 0.2.8"
            echo "  curl ... | sh -s -- --install-dir /opt/echohub"
            exit 0
            ;;
        *)
            echo "Unknown option: $1" >&2
            exit 1
            ;;
    esac
done

# ── Platform detection ────────────────────────────────────────────────────

detect_os() {
    case "$(uname -s)" in
        Linux*)  echo "linux" ;;
        Darwin*) echo "osx" ;;
        *)
            echo "Error: Unsupported operating system: $(uname -s)" >&2
            echo "EchoHub supports Linux and macOS. For Windows, use: choco install echohub" >&2
            exit 1
            ;;
    esac
}

detect_arch() {
    case "$(uname -m)" in
        x86_64|amd64)   echo "x64" ;;
        aarch64|arm64)  echo "arm64" ;;
        *)
            echo "Error: Unsupported architecture: $(uname -m)" >&2
            echo "EchoHub supports x64 and arm64." >&2
            exit 1
            ;;
    esac
}

# ── Version resolution ────────────────────────────────────────────────────

resolve_version() {
    if [ -n "$VERSION" ]; then
        echo "$VERSION"
        return
    fi

    # Fetch latest release tag from GitHub API
    if command -v curl >/dev/null 2>&1; then
        tag=$(curl -sSf "https://api.github.com/repos/$REPO/releases/latest" \
            | grep '"tag_name"' | head -1 | sed 's/.*"tag_name"[[:space:]]*:[[:space:]]*"v\?\([^"]*\)".*/\1/')
    elif command -v wget >/dev/null 2>&1; then
        tag=$(wget -qO- "https://api.github.com/repos/$REPO/releases/latest" \
            | grep '"tag_name"' | head -1 | sed 's/.*"tag_name"[[:space:]]*:[[:space:]]*"v\?\([^"]*\)".*/\1/')
    else
        echo "Error: curl or wget is required to detect the latest version." >&2
        echo "Install curl/wget or specify a version with --version X.Y.Z" >&2
        exit 1
    fi

    if [ -z "$tag" ]; then
        echo "Error: Could not determine latest version from GitHub." >&2
        exit 1
    fi

    echo "$tag"
}

# ── Install directory resolution ──────────────────────────────────────────

resolve_install_dir() {
    if [ -n "$INSTALL_DIR" ]; then
        echo "$INSTALL_DIR"
        return
    fi

    # Prefer /usr/local/bin if writable, otherwise ~/.local/bin
    if [ -w "/usr/local/bin" ]; then
        echo "/usr/local/bin"
    else
        local_bin="$HOME/.local/bin"
        mkdir -p "$local_bin"
        echo "$local_bin"
    fi
}

# ── Download helper ───────────────────────────────────────────────────────

download() {
    url="$1"
    output="$2"

    if command -v curl >/dev/null 2>&1; then
        curl -sSfL "$url" -o "$output"
    elif command -v wget >/dev/null 2>&1; then
        wget -qO "$output" "$url"
    else
        echo "Error: curl or wget is required." >&2
        exit 1
    fi
}

# ── PATH setup ────────────────────────────────────────────────────────────

add_to_path() {
    dir="$1"
    export_line="export PATH=\"${dir}:\$PATH\" # Added by EchoHub"

    added=0
    for profile in "$HOME/.profile" "$HOME/.bashrc" "$HOME/.zshrc"; do
        if [ -f "$profile" ]; then
            if grep -q "$dir" "$profile" 2>/dev/null; then
                continue  # Already present
            fi
            printf '\n%s\n' "$export_line" >> "$profile"
            echo "  Added to PATH in $(basename "$profile")"
            added=1
        fi
    done

    # If no profile existed, create .profile
    if [ "$added" -eq 0 ]; then
        printf '\n%s\n' "$export_line" >> "$HOME/.profile"
        echo "  Added to PATH in .profile"
    fi
}

# ── Main ──────────────────────────────────────────────────────────────────

main() {
    os=$(detect_os)
    arch=$(detect_arch)
    version=$(resolve_version)
    install_dir=$(resolve_install_dir)

    artifact="EchoHub-Client-${os}-${arch}.zip"
    url="https://github.com/$REPO/releases/download/v${version}/${artifact}"

    echo "EchoHub Installer"
    echo "  Version:      v${version}"
    echo "  Platform:     ${os}-${arch}"
    echo "  Install to:   ${install_dir}"
    echo ""

    # Create temp directory with cleanup trap
    tmpdir=$(mktemp -d)
    trap 'rm -rf "$tmpdir"' EXIT

    echo "Downloading ${artifact}..."
    download "$url" "$tmpdir/echohub.zip"

    echo "Extracting..."
    if command -v unzip >/dev/null 2>&1; then
        unzip -qo "$tmpdir/echohub.zip" -d "$tmpdir/extract"
    else
        echo "Error: unzip is required to extract the archive." >&2
        echo "Install it with: apt install unzip / brew install unzip" >&2
        exit 1
    fi

    # The ZIP contains a client-{os}-{arch}/ subdirectory
    src_dir="$tmpdir/extract/client-${os}-${arch}"
    if [ ! -d "$src_dir" ]; then
        # Fallback: look for any directory containing the binary
        src_dir=$(find "$tmpdir/extract" -name "EchoHub.Client" -type f -printf '%h' -quit 2>/dev/null || true)
        if [ -z "$src_dir" ]; then
            echo "Error: Could not find EchoHub.Client binary in the archive." >&2
            exit 1
        fi
    fi

    # Install full directory to ~/.local/share/echohub and symlink the binary.
    # Even single-file publishes may have content files (appsettings, assets)
    # that the app expects next to the binary via AppContext.BaseDirectory.
    app_dir="$HOME/.local/share/echohub"
    rm -rf "$app_dir"
    mkdir -p "$app_dir"
    cp -r "$src_dir"/. "$app_dir/"
    chmod +x "$app_dir/EchoHub.Client"

    mkdir -p "$install_dir"
    ln -sf "$app_dir/EchoHub.Client" "$install_dir/$BINARY_NAME"

    echo ""

    # Ensure install directory is on PATH
    if ! command -v "$BINARY_NAME" >/dev/null 2>&1; then
        add_to_path "$install_dir"
    fi

    echo "Installed successfully! Run 'echohub' to start."
    echo ""
    if ! echo "$PATH" | tr ':' '\n' | grep -qx "$install_dir"; then
        echo "NOTE: Restart your shell or run the following to use echohub now:"
        echo ""
        echo "  export PATH=\"${install_dir}:\$PATH\""
    fi
}

main
