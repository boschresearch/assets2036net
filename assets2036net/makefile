NUGETAPIKEY:=$(shell cat nugetapikey.txt)

check_VERSION:
ifndef VERSION
	$(error Env var VERSION is undefined. Enter something like export VERSION=1.2.3)
endif

clean: 
	rm obj -rf
	rm bin -rf

pack: check_VERSION
	dotnet pack -p:PackageVersion=$(VERSION) -c Release

push: pack
	dotnet nuget push ./bin/Release/assets2036net.$(VERSION).nupkg -k $(NUGETAPIKEY) -s https://api.nuget.org/v3/index.json