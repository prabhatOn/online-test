FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["OnlineAssessment.sln", "./"]
COPY ["OnlineAssessment.Compiler/OnlineAssessment.Compiler.csproj", "OnlineAssessment.Compiler/"]
COPY ["OnlineAssessment.Infrastructure/OnlineAssessment.Infrastructure.csproj", "OnlineAssessment.Infrastructure/"]
COPY ["OnlineAssessment.MessageQueue/OnlineAssessment.MessageQueue.csproj", "OnlineAssessment.MessageQueue/"]
RUN dotnet restore "OnlineAssessment.Compiler/OnlineAssessment.Compiler.csproj"
COPY . .
WORKDIR "/src/OnlineAssessment.Compiler"
RUN dotnet build "OnlineAssessment.Compiler.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "OnlineAssessment.Compiler.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OnlineAssessment.Compiler.dll"] 