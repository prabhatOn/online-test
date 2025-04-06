FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Install Java
RUN apt-get update && \
    apt-get install -y openjdk-17-jdk && \
    apt-get clean

# Install Python
RUN apt-get update && \
    apt-get install -y python3 python3-pip && \
    apt-get clean

# Copy the project files
COPY . .

# Build the application
RUN dotnet publish -c Release -o out

# Create the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Install Java runtime
RUN apt-get update && \
    apt-get install -y openjdk-17-jre && \
    apt-get clean

# Install Python runtime
RUN apt-get update && \
    apt-get install -y python3 && \
    apt-get clean

# Copy the published application
COPY --from=build /app/out .

# Create a non-root user for security
RUN useradd -m -s /bin/bash appuser && \
    chown -R appuser:appuser /app
USER appuser

# Set environment variables
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV ASPNETCORE_URLS=http://+:8080

# Expose the port
EXPOSE 8080

# Start the application
ENTRYPOINT ["dotnet", "OnlineAssessment.Compiler.dll"] 