# Use the official .NET 5 runtime image
FROM mcr.microsoft.com/dotnet/runtime:5.0

# Set the working directory
WORKDIR /app

# Copy the published application files into the container
COPY ./bin/Debug/net5.0/ ./

# Expose the port your server listens on
EXPOSE 5000

# Run the server
CMD ["dotnet", "KeyLoggerServer.dll"]
