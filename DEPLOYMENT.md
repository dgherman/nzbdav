# NzbDAV Deployment Guide

This fork includes automated Docker image builds via GitHub Actions.

## Automated Builds

Every push to any branch triggers a Docker image build that:
- Builds for **amd64** and **arm64** architectures
- Pushes to **GitHub Container Registry (GHCR)**
- Tags appropriately based on branch and version

### Image Tags

**Main branch builds** create multiple tags:
- `ghcr.io/dgherman/nzbdav:latest` - Latest main branch
- `ghcr.io/dgherman/nzbdav:0.5.x` - Latest minor version
- `ghcr.io/dgherman/nzbdav:0.x` - Latest major version
- `ghcr.io/dgherman/nzbdav:0.5.XXX` - Specific version (auto-incremented)

**Feature branch builds**:
- `ghcr.io/dgherman/nzbdav:<branch-name>` - Branch-specific tag

## Deploying on Your VPS

### 1. Update your podman/docker-compose to use GHCR

Replace your current image with:
```yaml
services:
  nzbdav:
    image: ghcr.io/dgherman/nzbdav:latest
    # ... rest of your config
```

### 2. Pull and restart

```bash
# Pull the latest image
podman pull ghcr.io/dgherman/nzbdav:latest

# Restart your container
podman restart nzbdav

# Or if using systemd:
systemctl --user restart nzbdav
```

### 3. Auto-update setup (optional)

To automatically pull new images when they're built, use Watchtower:

```bash
podman run -d \
  --name watchtower \
  -v /var/run/podman/podman.sock:/var/run/docker.sock \
  containrrr/watchtower \
  --interval 300
```

## Manual Build (Local)

If you need to build locally:

```bash
# Build for your platform
docker build -t nzbdav:local .

# Build for specific platform
docker buildx build --platform linux/amd64 -t nzbdav:local .
```

## Changes Included

This fork includes **audio file support**:
- Supports music downloads (mp3, flac, aac, m4a, ogg, opus, wav, etc.)
- Updated validators to handle both video and audio files
- See commit: `6076946` for details
