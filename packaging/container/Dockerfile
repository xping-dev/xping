# OCI image for xping. The release workflow builds it from the same self-contained linux binaries it
# signs and attests, placed in the build context as linux-amd64/xping and linux-arm64/xping.
# Nothing is compiled here.
#
# Base: .NET runtime-deps 10.0 (Ubuntu 24.04), pinned by index digest. Dependabot bumps it.
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0@sha256:e6702e45635272155c2aa35568bcbdb975595c0afa73e2656a973d0a87703282

ARG TARGETARCH

# git: xping check diffs against the merge base. ca-certificates: HTTPS to the CI provider's API.
# A CI workspace mounted into the container is owned by another user, so git must not refuse it.
RUN apt-get update \
    && apt-get install -y --no-install-recommends git ca-certificates \
    && rm -rf /var/lib/apt/lists/* \
    && git config --system --add safe.directory '*'

COPY --chmod=0755 linux-${TARGETARCH}/xping /usr/local/bin/xping

# The non-root user the .NET images ship (UID 1654).
USER app
WORKDIR /workspace

ENTRYPOINT ["xping"]
