FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY api/StickyBoard.Api/StickyBoard.Api.csproj api/StickyBoard.Api/
COPY Core/StickyBoard.Core/StickyBoard.Core.csproj Core/StickyBoard.Core/

RUN dotnet restore api/StickyBoard.Api/StickyBoard.Api.csproj

COPY . .

WORKDIR /src/api/StickyBoard.Api
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "StickyBoard.Api.dll"]
