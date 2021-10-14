FROM mcr.microsoft.com/dotnet/aspnet:5.0.3
# ARG source
WORKDIR /app
EXPOSE 80
COPY . .
# COPY /runtimes/debian-x64/native/libuv.so /app
ENTRYPOINT ["dotnet", "LykkeApi2.dll"]
