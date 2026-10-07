FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "src/ProductMatcher.Api/ProductMatcher.Api.csproj"
WORKDIR "/src/src/ProductMatcher.Api"
RUN dotnet publish "ProductMatcher.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "ProductMatcher.Api.dll"]
