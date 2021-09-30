FROM swisschains/dotnet:2.2-aspnetcore-runtime
# ARG source
WORKDIR /app
EXPOSE 80
COPY . .
# COPY /runtimes/debian-x64/native/libuv.so /app
ENTRYPOINT ["dotnet", "LykkeApi2.dll"]