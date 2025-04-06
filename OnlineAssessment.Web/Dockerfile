FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["OnlineAssessment.sln", "./"]
COPY ["OnlineAssessment.Web/OnlineAssessment.Web.csproj", "OnlineAssessment.Web/"]
COPY ["OnlineAssessment.Infrastructure/OnlineAssessment.Infrastructure.csproj", "OnlineAssessment.Infrastructure/"]
COPY ["OnlineAssessment.Compiler/OnlineAssessment.Compiler.csproj", "OnlineAssessment.Compiler/"]
COPY ["OnlineAssessment.MessageQueue/OnlineAssessment.MessageQueue.csproj", "OnlineAssessment.MessageQueue/"]
RUN dotnet restore "OnlineAssessment.Web/OnlineAssessment.Web.csproj"
COPY . .
WORKDIR "/src/OnlineAssessment.Web"
RUN dotnet build "OnlineAssessment.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "OnlineAssessment.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "OnlineAssessment.Web.dll"] 