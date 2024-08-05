# Use the .NET SDK image to build the project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Copy everything and restore as distinct layers
COPY . ./
RUN dotnet restore GlyCounter/GlyCounter/GlyCounter.csproj

# Build the project
RUN dotnet build GlyCounter/GlyCounter/GlyCounter.csproj --configuration Release --output /app/build

# Publish the project
RUN dotnet publish GlyCounter/GlyCounter/GlyCounter.csproj --configuration Release --output /app/publish

# Use the ASP.NET runtime image for the final stage
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build-env /app/publish .

ENTRYPOINT ["dotnet", "GlyCounter.dll"]
