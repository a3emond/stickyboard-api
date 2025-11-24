FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY worker/StickyBoard.Worker/StickyBoard.Worker.csproj worker/StickyBoard.Worker/
COPY Core/StickyBoard.Core/StickyBoard.Core.csproj Core/StickyBoard.Core/

RUN dotnet restore worker/StickyBoard.Worker/StickyBoard.Worker.csproj

COPY . .

WORKDIR /src/worker/StickyBoard.Worker
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "StickyBoard.Worker.dll"]
