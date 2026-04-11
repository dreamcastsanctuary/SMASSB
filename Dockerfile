FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app/out

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app

RUN apt-get update && apt-get install -y libgdiplus && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/out .

ENTRYPOINT ["dotnet", "SMASSB.dll"]
