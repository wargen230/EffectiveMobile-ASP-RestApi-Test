FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY *.sln ./
COPY TestAPI.API/*.csproj ./TestAPI.API/
COPY TestAPI.Tests/*.csproj ./TestAPI.Tests/

RUN dotnet restore ./TestAPI.API/TestAPI.API.csproj

COPY . .

WORKDIR /src/TestAPI.API
RUN dotnet publish TestAPI.API.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
ENV ASPNETCORE_URLS=http://localhost:5001
EXPOSE 5001

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "TestAPI.API.dll"]
