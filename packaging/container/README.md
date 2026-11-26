# Container image

`ghcr.io/xping-dev/xping`, built by `.github/workflows/release.yml` for `linux/amd64` and `linux/arm64`.

```sh
docker run --rm -v "$PWD:/workspace" ghcr.io/xping-dev/xping:2.0.0-alpha.1 check --local
```

Local build from a published binary:

```sh
dotnet publish src/Xping.Cli -c Release -r linux-x64 -o packaging/container/linux-amd64
docker build packaging/container -f packaging/container/Dockerfile --platform linux/amd64 -t xping:dev
```
