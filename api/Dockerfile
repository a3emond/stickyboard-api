# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj first (only project, not everything)
COPY StickyBoard.Api/StickyBoard.Api.csproj ./StickyBoard.Api/
RUN dotnet restore StickyBoard.Api/StickyBoard.Api.csproj

# Copy all remaining files
COPY . .
WORKDIR /src/StickyBoard.Api

# Build & publish
RUN dotnet publish -c Release -o /app/publish

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Copy published output from build stage
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "StickyBoard.Api.dll"]
