# infinity-odin
Odin - An Open Internal Developer Platform.

# Build and deploy

## Building Odin.Api container image

### Build image locally
Run the following command:
```
dotnet publish -t:PublishContainer -p:ContainerImageTags=latest --no-restore -p:ContainerRepository=odin/api -p:VersionSuffix=beta1
```


### Build image and push to Azure Container Registry
To build and publish the application to Azure Container Registry and then update the container app, run the following commands:

Build the container image by running the following command:
```
dotnet publish .\src\Odin.Api\Odin.Api.csproj  -t:PublishContainer -p:ContainerImageTags='"latest"' -p:VersionSuffix=test1 -p:ContainerRegistry=mycontainerregistry.azurecr.io
```

This will create a docker image `odin/api` with the tag `latest` and push it to the Azure Container Registry `mycontainerregistry.azurecr.io`.
Replace `mycontainerregistry.azurecr.io` with your Azure Container Registry name.

# Running the application

## Running the application locally

### Running the application using Docker
To run the application using Docker, run the following command:
```
docker run -p 8080:80 odin/api:latest
```

This will run the application on port 8080.

### Running the application using dotnet
To run the application using dotnet, run the following command:
```
dotnet run --project .\src\Odin.Api\Odin.Api.csproj
```

This will run the application on port 5000.

### Run the application in a container using https and a self-signed certificate

To run the application in a container using https and a self-signed certificate, run the following command:
```
docker run --rm -it -p 5001:8080 -e ASPNETCORE_URLS="https://+:8080;" -e ASPNETCORE_ENVIRONMENT=Development -e ASPNETCORE_HTTPS_PORT=5001 -e ASPNETCORE_Kestrel__Certificates__Default__Password="docker" -e ASPNETCORE_Kestrel__Certificates__Default__Path=/https/aspnetapp.pfx -v $env:USERPROFILE\.aspnet\https:/https/ -v $env:APPDATA\microsoft\UserSecrets\:/root/.microsoft/usersecrets:ro odin/api:latest
```

Running locally with user-secrets requires mounting the user-secrets directory to the container. This is done by mounting the user-secrets directory to the container using the `-v` flag.
It's also required to run as root to access the user-secrets directory. This is done by running the container with the `--user` flag.
```
docker run --rm -it -p 5001:8080 -e ASPNETCORE_URLS="https://+:8080;" -e ASPNETCORE_ENVIRONMENT=Development -e ASPNETCORE_HTTPS_PORT=5001 -e ASPNETCORE_Kestrel__Certificates__Default__Password="docker" -e ASPNETCORE_Kestrel__Certificates__Default__Path=/https/aspnetapp.pfx -v $env:USERPROFILE\.aspnet\https:/https/ -v $env:APPDATA\microsoft\UserSecrets\:/root/.microsoft/usersecrets --user root odin/api:latest
```

## Dependencies
Odin.Api depends on a few services to run. These services are:
- Azure Storage Emulator (Azurite)
- .NET Aspire Dashboard
- Redis (maybe in the future)


To run Azurite, run the following command:
```
docker run -d -p 10000:10000 -p 10001:10001 -p 10002:10002 -v c:/Temp/azurite:/data mcr.microsoft.com/azure-storage/azurite:latest
```

To run .NET Aspire Dashboard, run the following command:
```
docker run --rm -it -p 18888:18888 -p 4317:18889 -d --name aspire-dashboard -e DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS='true' mcr.microsoft.com/dotnet/aspire-dashboard:latest
```