#!/usr/bin/env bash
#
# Build a self-contained Tomoru tarball for Linux.
#
#   ./scripts/pack-linux.sh             # x64
#   ./scripts/pack-linux.sh linux-arm64 # ARM
#
# Output: dist/Tomoru-<rid>.tar.gz containing the app folder plus a
# tomoru.desktop you can drop into ~/.local/share/applications (fix the
# Exec/Icon paths to wherever you extract).
#
# NOTE: written on macOS and not yet run on a real Linux box — the underlying
# command is just `dotnet publish` + tar.

set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PROJECT="$ROOT/src/Tomoru"
DIST="$ROOT/dist"

RID="${1:-linux-x64}"
PUBLISH="$DIST/publish-$RID"
STAGE="$DIST/Tomoru-$RID"

echo "▸ publishing for $RID ..."
rm -rf "$PUBLISH" "$STAGE"
dotnet publish "$PROJECT" \
  -c Release \
  -r "$RID" \
  --self-contained true \
  -p:DebugType=embedded \
  -o "$PUBLISH" \
  > /dev/null

echo "▸ staging ..."
mkdir -p "$STAGE"
cp -R "$PUBLISH"/. "$STAGE/"
cp "$PROJECT/Assets/icon.png" "$STAGE/tomoru.png"

cat > "$STAGE/tomoru.desktop" <<DESKTOP
[Desktop Entry]
Type=Application
Name=tomoru
Comment=A calm late-night study companion
Exec=/opt/tomoru/Tomoru
Icon=/opt/tomoru/tomoru.png
Terminal=false
Categories=Utility;
DESKTOP

echo "▸ packing ..."
tar -czf "$DIST/Tomoru-$RID.tar.gz" -C "$DIST" "Tomoru-$RID"
rm -rf "$STAGE"

echo
echo "✓ $DIST/Tomoru-$RID.tar.gz"
echo "  extract to /opt/tomoru (or anywhere) and adjust the .desktop paths"
