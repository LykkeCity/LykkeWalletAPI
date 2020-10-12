FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim
# ARG source
WORKDIR /app
EXPOSE 80
COPY . .
# COPY /runtimes/debian-x64/native/libuv.so /app
ENTRYPOINT ["dotnet", "LykkeApi2.dll"]
