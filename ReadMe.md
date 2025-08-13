# The Illustrated Book

## global.json
global.json has to be changed from env to env to match the installed .Net SDK. The two versions are "8.0.101" and "8.0.410".

## appSettings
Never check in any sensitive data in appSettings. Keep it all in _appSettings.json, which is ignored ny git, and copy the stuff into the appSettings.json file.
