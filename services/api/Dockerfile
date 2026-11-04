# syntax=docker/dockerfile:1
#
# TUMIIA API image. Built for linux/arm64 (Fargate on Graviton).
# Build from this folder:  docker build --platform linux/arm64 -t tumiia-api .
#
# Stages:
#   build   - compiles the API and the EF Core migrations bundle (/app/efbundle)
#   model   - downloads the pinned sentence-embedding model and checks its SHA-256
#   runtime - the small image that runs in AWS, with Tesseract OCR installed

# ---------- build ----------
# Runs on the build machine's own CPU (no emulation) and cross-compiles for the target CPU.
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /src

# Restore first, from project files only, so this slow layer is cached until a package changes.
COPY Directory.Build.props Directory.Packages.props dotnet-tools.json ./
COPY src/Tumiia.Api/Tumiia.Api.csproj src/Tumiia.Api/
COPY src/Tumiia.Application/Tumiia.Application.csproj src/Tumiia.Application/
COPY src/Tumiia.Domain/Tumiia.Domain.csproj src/Tumiia.Domain/
COPY src/Tumiia.Infrastructure/Tumiia.Infrastructure.csproj src/Tumiia.Infrastructure/
RUN dotnet tool restore && dotnet restore src/Tumiia.Api/Tumiia.Api.csproj -a "$TARGETARCH"

COPY src/ src/

# The OpenAPI file is generated and checked in CI, so it is not regenerated here.
RUN dotnet publish src/Tumiia.Api/Tumiia.Api.csproj -c Release -a "$TARGETARCH" --no-restore \
      -p:OpenApiGenerateDocuments=false -o /app

# The migrations bundle runs as a one-off ECS task before each deploy. Docker says "amd64", .NET says "x64".
RUN if [ "$TARGETARCH" = "amd64" ]; then RID=linux-x64; else RID="linux-$TARGETARCH"; fi \
 && dotnet ef migrations bundle \
      --project src/Tumiia.Infrastructure --startup-project src/Tumiia.Infrastructure \
      --configuration Release --target-runtime "$RID" --self-contained \
      --output /app/efbundle

# ---------- model ----------
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS model
# sentence-transformers/all-MiniLM-L6-v2 (Apache-2.0), pinned to one revision so it can never change underneath us.
ARG MODEL_REVISION=1110a243fdf4706b3f48f1d95db1a4f5529b4d41
ARG MODEL_SHA256=6fd5d72fe4589f189f8ebc006442dbb529bb7ce38f8082112682524616046452
ARG VOCAB_SHA256=07eced375cec144d27c900241f3e339478dec958f92fddbc551f295c992038a3
WORKDIR /models
RUN base="https://huggingface.co/sentence-transformers/all-MiniLM-L6-v2/resolve/$MODEL_REVISION" \
 && curl -fsSL --retry 3 -o model.onnx "$base/onnx/model.onnx" \
 && curl -fsSL --retry 3 -o vocab.txt "$base/vocab.txt" \
 && echo "$MODEL_SHA256  model.onnx" | sha256sum -c - \
 && echo "$VOCAB_SHA256  vocab.txt" | sha256sum -c -

# ---------- runtime ----------
# .NET 10's runtime images are Ubuntu 24.04 LTS ("noble"); there is no stable Debian variant.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS runtime

# Tesseract 5 for OCR, with English language data.
RUN apt-get update \
 && apt-get install -y --no-install-recommends tesseract-ocr tesseract-ocr-eng \
 && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app .
COPY --from=model /models /app/models/all-MiniLM-L6-v2
COPY --chmod=755 docker/healthcheck.sh /app/healthcheck.sh

# Run as the image's built-in non-root "app" user.
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 CMD ["/app/healthcheck.sh"]

# CMD (not ENTRYPOINT) so the deploy can swap it for /app/efbundle when it runs the migrations task.
CMD ["dotnet", "Tumiia.Api.dll"]
