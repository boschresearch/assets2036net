VERSION=0.4.3

.PHONY: pack
pack: 
	dotnet pack -c Release

.PHONY: nuget
nuget: pack
	dotnet nuget push ./bin/Release/assets2036net.$(VERSION).nupkg -s nuget.org -k oy2lyfcxemkqzukrowzaf3lwswbsq43ejeauaqhsclqigi 
